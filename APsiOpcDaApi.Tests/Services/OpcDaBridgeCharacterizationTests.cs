using System.Net;
using System.Text;
using System.Text.Json;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.Extensions.Logging;

namespace APsiOpcDaApi.Tests.Services;

public sealed class OpcDaBridgeCharacterizationTests
{
    [Fact]
    public async Task ReadViaBridgeAsync_UrlAusente_RetornaVazioSemEnviarRequisicao()
    {
        var transport = new RecordingBridgeTransport(() =>
            throw new InvalidOperationException("A bridge não deveria ser chamada."));
        var service = CreateService();

        var tags = await service.ReadViaBridgeAsync(
            CreateServer(), ["Channel.Device.Tag"], "   ", transport.PostAsync);

        Assert.Empty(tags);
        Assert.Null(transport.Url);
    }

    [Fact]
    public async Task ReadViaBridgeAsync_RespostaParcial_MapeiaItensPreservaPayloadERegistraErros()
    {
        const string responseBody = """
            {
              "Items": [
                {
                  "ItemId": "Channel.Device.Good",
                  "Value": "12.5",
                  "Quality": "Good",
                  "Timestamp": "2026-08-26T18:19:20Z"
                },
                {
                  "ItemId": null,
                  "Value": null,
                  "Quality": null,
                  "Timestamp": "timestamp-invalido"
                }
              ],
              "Errors": ["Channel.Device.Missing: sem retorno"]
            }
            """;
        var transport = new RecordingBridgeTransport(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
        var logger = new RecordingLogger<OpcDaClientService>();
        var service = new OpcDaClientService(logger);
        var beforeInvalidTimestampFallback = DateTime.UtcNow;

        var tags = await service.ReadViaBridgeAsync(
            CreateServer(),
            ["Channel.Device.Good", "Channel.Device.Missing"],
            "http://bridge.test/base/",
            transport.PostAsync);

        var afterInvalidTimestampFallback = DateTime.UtcNow;
        Assert.Equal("http://bridge.test/base/read", transport.Url);
        Assert.Equal("application/json", transport.MediaType);
        using var payload = JsonDocument.Parse(transport.RequestBody!);
        Assert.Equal("opc-host", payload.RootElement.GetProperty("host").GetString());
        Assert.Equal("Matrikon.OPC.Simulation.1", payload.RootElement.GetProperty("progId").GetString());
        Assert.Equal("{00000000-0000-0000-0000-000000000001}", payload.RootElement.GetProperty("clsId").GetString());
        Assert.Collection(
            payload.RootElement.GetProperty("itemIds").EnumerateArray(),
            item => Assert.Equal("Channel.Device.Good", item.GetString()),
            item => Assert.Equal("Channel.Device.Missing", item.GetString()));

        Assert.Collection(
            tags,
            tag =>
            {
                Assert.Equal("Channel.Device.Good", tag.NodeId);
                Assert.Equal("Channel.Device.Good", tag.DisplayName);
                Assert.Equal("Channel.Device.Good", tag.BrowseName);
                Assert.Equal("Variable", tag.NodeClass);
                Assert.Equal(string.Empty, tag.DataType);
                Assert.Equal("12.5", tag.ValorAtual);
                Assert.Equal("Good", tag.Quality);
                Assert.Equal(
                    new DateTimeOffset(2026, 8, 26, 18, 19, 20, TimeSpan.Zero).LocalDateTime,
                    tag.Timestamp);
            },
            tag =>
            {
                Assert.Equal(string.Empty, tag.NodeId);
                Assert.Equal(string.Empty, tag.DisplayName);
                Assert.Equal(string.Empty, tag.BrowseName);
                Assert.Equal("Variable", tag.NodeClass);
                Assert.Equal(string.Empty, tag.DataType);
                Assert.Null(tag.ValorAtual);
                Assert.Equal(string.Empty, tag.Quality);
                Assert.InRange(tag.Timestamp!.Value, beforeInvalidTimestampFallback, afterInvalidTimestampFallback);
            });
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message.Contains("Channel.Device.Missing: sem retorno", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadViaBridgeAsync_StatusDeErro_RetornaVazioERegistraFalha()
    {
        var transport = new RecordingBridgeTransport(() =>
            new HttpResponseMessage(HttpStatusCode.BadGateway));
        var logger = new RecordingLogger<OpcDaClientService>();
        var service = new OpcDaClientService(logger);

        var tags = await service.ReadViaBridgeAsync(
            CreateServer(), ["Channel.Device.Tag"], "http://bridge.test", transport.PostAsync);

        Assert.Empty(tags);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadViaBridgeAsync_IndisponibilidadeOuTimeout_RetornaVazioERegistraFalha(bool timeout)
    {
        var transport = new RecordingBridgeTransport(() => timeout
            ? throw new TaskCanceledException("timeout caracterizado")
            : throw new HttpRequestException("bridge indisponível"));
        var logger = new RecordingLogger<OpcDaClientService>();
        var service = new OpcDaClientService(logger);

        var tags = await service.ReadViaBridgeAsync(
            CreateServer(), ["Channel.Device.Tag"], "http://bridge.test", transport.PostAsync);

        Assert.Empty(tags);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    private static OpcDaClientService CreateService() =>
        new(new RecordingLogger<OpcDaClientService>());

    private static OpcServerDTO CreateServer() => new()
    {
        Tipo = TipoOpcServer.Da,
        Host = "opc-host",
        ProgId = "Matrikon.OPC.Simulation.1",
        ClsId = "{00000000-0000-0000-0000-000000000001}"
    };

    private sealed class RecordingBridgeTransport(Func<HttpResponseMessage> responseFactory)
    {
        public string? Url { get; private set; }
        public string? RequestBody { get; private set; }
        public string? MediaType { get; private set; }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            Url = url;
            MediaType = content.Headers.ContentType?.MediaType;
            RequestBody = await content.ReadAsStringAsync();
            return responseFactory();
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
