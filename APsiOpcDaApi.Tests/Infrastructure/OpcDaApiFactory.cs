using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace APsiOpcDaApi.Tests.Infrastructure;

internal sealed class OpcDaApiFactory : WebApplicationFactory<Program>
{
    private readonly OpcServerDTO? _server;
    private readonly FakeOpcBrowserService? _opcBrowser;
    private readonly FakeOpcGroupService? _opcGroup;
    private readonly IReadOnlyList<OpcServerDTO> _existingServers;

    public OpcDaApiFactory(
        OpcServerDTO? server,
        FakeOpcDaClientService opcDaClient,
        FakeOpcBrowserService? opcBrowser = null,
        bool isOpcDaSupported = false,
        FakeOpcDaServerEnumerator? opcDaServerEnumerator = null,
        IReadOnlyList<OpcServerDTO>? existingServers = null,
        FakeOpcGroupService? opcGroup = null)
    {
        _server = server;
        _opcBrowser = opcBrowser;
        _opcGroup = opcGroup;
        _existingServers = existingServers ?? (server is null ? [] : [server]);
        OpcDaClient = opcDaClient;
        OpcDaServerEnumerator = opcDaServerEnumerator ?? new FakeOpcDaServerEnumerator();
        IsOpcDaSupported = isOpcDaSupported;
    }

    public FakeOpcDaClientService OpcDaClient { get; }
    public FakeOpcDaServerEnumerator OpcDaServerEnumerator { get; }
    public bool IsOpcDaSupported { get; }
    public List<OpcServerDTO> AddedServers { get; } = [];
    public Mock<IOpcServerService> OpcServerService { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=discardable;Username=discardable;Password=discardable",
                ["Jwt:Secret"] = "testing-only-key-with-more-than-32-bytes",
                ["Logging:FileLoggingEnabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var serverService = new Mock<IOpcServerService>(MockBehavior.Strict);
            serverService
                .Setup(service => service.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(_server!);
            serverService
                .Setup(service => service.GetServersByTypeAsync(It.IsAny<Domain.Enum.TipoOpcServer>()))
                .ReturnsAsync(_existingServers);
            serverService
                .Setup(service => service.GetAllAsync())
                .ReturnsAsync(_existingServers);
            serverService
                .Setup(service => service.AddAsync(It.IsAny<OpcServerDTO>()))
                .ReturnsAsync((OpcServerDTO dto) =>
                {
                    AddedServers.Add(dto);
                    return dto;
                });
            serverService
                .Setup(service => service.IsOpcDaSupported())
                .Returns(IsOpcDaSupported);
            OpcServerService = serverService;

            services.RemoveAll<IOpcServerService>();
            services.RemoveAll<IOpcDaClientService>();
            services.AddSingleton(serverService.Object);
            services.AddSingleton<IOpcDaClientService>(OpcDaClient);

            if (_opcBrowser is not null)
            {
                services.RemoveAll<IOpcBrowserService>();
                services.AddSingleton<IOpcBrowserService>(_opcBrowser);
            }

            if (_opcGroup is not null)
            {
                services.RemoveAll<IOpcGroupService>();
                services.AddSingleton<IOpcGroupService>(_opcGroup);
            }

            services.RemoveAll<IOpcDaServerEnumerator>();
            services.AddSingleton<IOpcDaServerEnumerator>(OpcDaServerEnumerator);
        });
    }
}
