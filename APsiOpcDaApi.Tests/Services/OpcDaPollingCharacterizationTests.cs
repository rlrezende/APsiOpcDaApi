using System.Reflection;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace APsiOpcDaApi.Tests.Services;

public sealed class OpcDaPollingCharacterizationTests
{
    private static readonly Guid ServerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid GroupId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid FirstTagId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondTagId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid WhitespaceTagId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid DisabledTagId = Guid.Parse("30000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task PollingOpcDa_ItemIdsEquivalentes_NormalizaIdsEProcessaQualityBadComTimestampOriginal()
    {
        var timestamp = new DateTime(2026, 8, 26, 14, 15, 16, DateTimeKind.Utc);
        var server = CreateDaServer();
        var group = CreatePollingGroup();
        var firstTag = CreateTag(FirstTagId, "Channel.Device.Tag");
        var secondTag = CreateTag(SecondTagId, "channel.device.tag");
        var fake = new FakeOpcDaClientService
        {
            ReadResult =
            [
                new OpcTagDTO
                {
                    NodeId = "CHANNEL.DEVICE.TAG",
                    ValorAtual = "12.5",
                    Quality = "Bad",
                    Timestamp = timestamp,
                    DataType = "System.Double"
                }
            ]
        };

        IReadOnlyDictionary<Guid, double>? currentValues = null;
        IReadOnlyList<LeituraDTO>? readings = null;
        var notifications = new List<(Guid TagId, double Value, DateTime Timestamp)>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var groupService = new Mock<IOpcGroupService>(MockBehavior.Strict);
        groupService.Setup(service => service.GetGroupTagsAsync(GroupId)).ReturnsAsync(
        [
            firstTag,
            secondTag,
            CreateTag(WhitespaceTagId, "   "),
            CreateTag(DisabledTagId, "Ignored.Tag", monitored: false)
        ]);
        groupService.Setup(service => service.GetByIdAsync(GroupId)).ReturnsAsync(group);

        var tagService = new Mock<ITagService>(MockBehavior.Strict);
        tagService
            .Setup(service => service.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([firstTag, secondTag]);
        tagService
            .Setup(service => service.AtualizarValoresAtuaisAsync(It.IsAny<IReadOnlyDictionary<Guid, double>>()))
            .Callback<IReadOnlyDictionary<Guid, double>>(values => currentValues = values)
            .Returns(Task.CompletedTask);

        var leituraService = new Mock<ILeituraService>(MockBehavior.Strict);
        leituraService
            .Setup(service => service.AddRangeAsync(It.IsAny<IEnumerable<LeituraDTO>>()))
            .Callback<IEnumerable<LeituraDTO>>(values => readings = values.ToList())
            .Returns(Task.CompletedTask);

        var notifier = new Mock<INotificadorSimulacao>(MockBehavior.Strict);
        notifier
            .Setup(service => service.NotificarAtualizacaoTagAsync(It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<DateTime>()))
            .Callback<Guid, double, DateTime>((tagId, value, observedTimestamp) =>
            {
                notifications.Add((tagId, value, observedTimestamp));
                if (notifications.Count == 2)
                {
                    completed.TrySetResult();
                }
            })
            .Returns(Task.CompletedTask);

        using var scopedServices = new ServiceCollection()
            .AddSingleton(groupService.Object)
            .AddSingleton(tagService.Object)
            .AddSingleton(leituraService.Object)
            .AddSingleton(notifier.Object)
            .BuildServiceProvider();

        using var service = new OpcMonitoringService(
            groupService.Object,
            Mock.Of<IOpcServerService>(),
            leituraService.Object,
            notifier.Object,
            Mock.Of<IOpcNodeService>(),
            tagService.Object,
            fake,
            NullLogger<OpcMonitoringService>.Instance,
            scopedServices.GetRequiredService<IServiceScopeFactory>());
        using var cancellation = new CancellationTokenSource();

        try
        {
            await InvokeEnsureOpcDaMonitoringAsync(service, server, group, cancellation.Token);
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            cancellation.Cancel();
            await service.StopMonitoringAsync();
        }

        var readCall = Assert.Single(fake.ReadCalls);
        Assert.Same(server, readCall.Server);
        Assert.Equal(["Channel.Device.Tag"], readCall.ItemIds);

        Assert.NotNull(currentValues);
        Assert.Equal(12.5, currentValues[FirstTagId]);
        Assert.Equal(12.5, currentValues[SecondTagId]);

        Assert.NotNull(readings);
        Assert.Collection(
            readings.OrderBy(reading => reading.TagId),
            reading => AssertReading(reading, FirstTagId, timestamp),
            reading => AssertReading(reading, SecondTagId, timestamp));

        Assert.Collection(
            notifications.OrderBy(notification => notification.TagId),
            notification => AssertNotification(notification, FirstTagId, timestamp),
            notification => AssertNotification(notification, SecondTagId, timestamp));
    }

    private static async Task InvokeEnsureOpcDaMonitoringAsync(
        OpcMonitoringService service,
        OpcServerDTO server,
        OpcGroupDTO group,
        CancellationToken cancellationToken)
    {
        var method = typeof(OpcMonitoringService).GetMethod(
            "EnsureOpcDaMonitoringAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = method.Invoke(service, [server, group, cancellationToken]) as Task;
        Assert.NotNull(task);
        await task;
    }

    private static OpcServerDTO CreateDaServer() => new()
    {
        Id = ServerId,
        Nome = "OPC DA descartável",
        Endpoint = "localhost",
        Host = "localhost",
        ProgId = "Fake.Server",
        Tipo = TipoOpcServer.Da
    };

    private static OpcGroupDTO CreatePollingGroup() => new()
    {
        Id = GroupId,
        ServerId = ServerId,
        Name = "Grupo determinístico",
        IsActive = true,
        AcquisitionMode = 2,
        UpdateRate = 1_000,
        HistorianIntervalSeconds = 30
    };

    private static TagDTO CreateTag(Guid id, string nodeId, bool monitored = true) => new()
    {
        Id = id,
        GroupId = GroupId,
        Monitora = monitored,
        NodeIdOpc = nodeId
    };

    private static void AssertReading(LeituraDTO reading, Guid expectedTagId, DateTime expectedTimestamp)
    {
        Assert.Equal(expectedTagId, reading.TagId);
        Assert.Equal(12.5, reading.Valor);
        Assert.Equal(expectedTimestamp, reading.DataLeitura);
    }

    private static void AssertNotification(
        (Guid TagId, double Value, DateTime Timestamp) notification,
        Guid expectedTagId,
        DateTime expectedTimestamp)
    {
        Assert.Equal(expectedTagId, notification.TagId);
        Assert.Equal(12.5, notification.Value);
        Assert.Equal(expectedTimestamp, notification.Timestamp);
    }
}
