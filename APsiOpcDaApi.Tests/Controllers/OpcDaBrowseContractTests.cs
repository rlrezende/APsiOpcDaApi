using System.Net;
using System.Text.Json;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Tests.Fakes;
using APsiOpcDaApi.Tests.Infrastructure;

namespace APsiOpcDaApi.Tests.Controllers;

public sealed class OpcDaBrowseContractTests
{
    private static readonly Guid ServerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UnidadeId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Browse_SemUnidadeComItemId_EncaminhaParametrosERetornaResultadoDoFake()
    {
        var expected = new OpcBrowseResultDTO
        {
            Nodes =
            [
                new OpcNodeBrowseDTO
                {
                    NodeId = "Area.1",
                    DisplayName = "Área 1",
                    BrowseName = "Area.1",
                    NodeClass = "Object",
                    HasChildren = true
                }
            ],
            Tags =
            [
                new OpcTagDTO
                {
                    NodeId = "Area.1.TagA",
                    DisplayName = "Tag A",
                    BrowseName = "TagA",
                    NodeClass = "Variable",
                    ValorAtual = "12.5",
                    DataType = "System.Double",
                    Quality = "Good",
                    Timestamp = new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc)
                }
            ]
        };
        var browser = new FakeOpcBrowserService { BrowseResult = expected };
        await using var factory = CreateFactory(CreateDaServer(), browser);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opcda/{ServerId}/browse?itemId=Area.1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = Assert.Single(browser.BrowseCalls);
        Assert.Equal(ServerId, call.ServerId);
        Assert.Equal("Area.1", call.ParentNodeId);

        using var json = await ReadJsonAsync(response);
        var node = Assert.Single(GetPreservedArray(json.RootElement.GetProperty("nodes")));
        Assert.Equal("Area.1", node.GetProperty("nodeId").GetString());
        Assert.True(node.GetProperty("hasChildren").GetBoolean());

        var tag = Assert.Single(GetPreservedArray(json.RootElement.GetProperty("tags")));
        Assert.Equal("Area.1.TagA", tag.GetProperty("nodeId").GetString());
        Assert.Equal("12.5", tag.GetProperty("valorAtual").GetString());
        Assert.Equal("Good", tag.GetProperty("quality").GetString());
        Assert.Equal("2026-08-26T12:30:00Z", tag.GetProperty("timestamp").GetString());
    }

    [Fact]
    public async Task Browse_UnidadeDoServidor_EncaminhaBrowseAoFake()
    {
        var browser = new FakeOpcBrowserService();
        await using var factory = CreateFactory(CreateDaServer(), browser);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/opcda/{ServerId}/browse?unidadeId={UnidadeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = Assert.Single(browser.BrowseCalls);
        Assert.Equal(ServerId, call.ServerId);
        Assert.Null(call.ParentNodeId);
    }

    [Fact]
    public async Task Browse_UnidadeDiferente_RetornaNotFoundSemConsultarFake()
    {
        var outraUnidade = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var browser = new FakeOpcBrowserService();
        await using var factory = CreateFactory(CreateDaServer(), browser);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/opcda/{ServerId}/browse?itemId=Area.1&unidadeId={outraUnidade}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(browser.BrowseCalls);

        using var json = await ReadJsonAsync(response);
        Assert.Equal(
            "Servidor OPC não pertence à unidade selecionada.",
            json.RootElement.GetProperty("message").GetString());
        Assert.Equal(ServerId, json.RootElement.GetProperty("serverId").GetGuid());
        Assert.Equal(outraUnidade, json.RootElement.GetProperty("unidadeId").GetGuid());
    }

    [Fact]
    public async Task Browse_BrowserLancaExcecao_PropagaFalhaSemRespostaHttpTratada()
    {
        var expected = new InvalidOperationException("falha determinística do browser");
        var browser = new FakeOpcBrowserService { BrowseException = expected };
        await using var factory = CreateFactory(CreateDaServer(), browser);
        using var client = factory.CreateClient();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync($"/api/opcda/{ServerId}/browse?itemId=Area.1"));

        Assert.Same(expected, exception);
        var call = Assert.Single(browser.BrowseCalls);
        Assert.Equal(ServerId, call.ServerId);
        Assert.Equal("Area.1", call.ParentNodeId);
    }

    [Fact]
    public async Task Browse_PlataformaDaNaoSuportada_PropagaFalhaDoFakeDaSemRespostaHttpTratada()
    {
        var expected = new PlatformNotSupportedException(
            "OPC DA só é suportado em ambientes Windows.");
        var opcDaClient = new FakeOpcDaClientService { BrowseException = expected };
        await using var factory = new OpcDaApiFactory(CreateDaServer(), opcDaClient);
        using var client = factory.CreateClient();

        var exception = await Assert.ThrowsAsync<PlatformNotSupportedException>(
            () => client.GetAsync($"/api/opcda/{ServerId}/browse?itemId=Area.1"));

        Assert.Same(expected, exception);
        var call = Assert.Single(opcDaClient.BrowseCalls);
        Assert.Equal(ServerId, call.Server.Id);
        Assert.Equal("Area.1", call.ItemId);
    }

    private static OpcDaApiFactory CreateFactory(OpcServerDTO server, FakeOpcBrowserService browser) =>
        new(server, new FakeOpcDaClientService(), browser);

    private static OpcServerDTO CreateDaServer() => new()
    {
        Id = ServerId,
        ModuloId = UnidadeId,
        Nome = "OPC DA descartável",
        Endpoint = "localhost",
        Host = "localhost",
        ProgId = "Fake.Server",
        Tipo = TipoOpcServer.Da
    };

    private static IReadOnlyList<JsonElement> GetPreservedArray(JsonElement element) =>
        element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().ToList()
            : element.GetProperty("$values").EnumerateArray().ToList();

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
