using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Tests.Fakes;
using APsiOpcDaApi.Tests.Infrastructure;

namespace APsiOpcDaApi.Tests.Controllers;

public sealed class OpcDaWriteContractTests
{
    private static readonly Guid ServerId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task WriteValue_ItemIdAusente_RetornaBadRequestSemConsultarOpc()
    {
        var fake = new FakeOpcDaClientService();
        await using var factory = new OpcDaApiFactory(CreateDaServer(), fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opcda/{ServerId}/write", new
        {
            itemId = "  ",
            value = 12.5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertJsonStringAsync(response, "message", "ItemId é obrigatório.");
        Assert.Empty(fake.WriteCalls);
    }

    [Fact]
    public async Task WriteValue_ServidorNaoEncontrado_RetornaNotFoundSemConsultarOpc()
    {
        var fake = new FakeOpcDaClientService();
        await using var factory = new OpcDaApiFactory(null, fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opcda/{ServerId}/write", new
        {
            itemId = "Channel.Device.Tag",
            value = 12.5
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertJsonStringAsync(response, "message", "Servidor OPC DA não encontrado.");
        Assert.Empty(fake.WriteCalls);
    }

    [Fact]
    public async Task WriteValue_PlataformaNaoSuportada_RetornaBadRequestSemConsultarOpc()
    {
        var fake = new FakeOpcDaClientService { IsSupported = false };
        await using var factory = new OpcDaApiFactory(CreateDaServer(), fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opcda/{ServerId}/write", new
        {
            itemId = "Channel.Device.Tag",
            value = 12.5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertJsonStringAsync(response, "message", "Write OPC DA só é suportado em ambiente Windows.");
        Assert.Empty(fake.WriteCalls);
    }

    [Fact]
    public async Task WriteValue_FakeConfirmaEscrita_RetornaContratoDeSucesso()
    {
        var server = CreateDaServer();
        var fake = new FakeOpcDaClientService { WriteResult = true };
        await using var factory = new OpcDaApiFactory(server, fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opcda/{ServerId}/write", new
        {
            itemId = "Channel.Device.Tag",
            value = 12.5
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Channel.Device.Tag", json.RootElement.GetProperty("itemId").GetString());
        Assert.Equal(12.5, json.RootElement.GetProperty("value").GetDouble());

        var call = Assert.Single(fake.WriteCalls);
        Assert.Same(server, call.Server);
        Assert.Equal("Channel.Device.Tag", call.ItemId);
        Assert.Equal(12.5, call.Value);
    }

    [Fact]
    public async Task WriteValue_FakeRejeitaEscrita_RetornaErroFuncionalAtual()
    {
        var fake = new FakeOpcDaClientService { WriteResult = false };
        await using var factory = new OpcDaApiFactory(CreateDaServer(), fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opcda/{ServerId}/write", new
        {
            itemId = "Channel.Device.Tag",
            value = -1.25
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal("Write falhou no servidor OPC DA.", json.RootElement.GetProperty("message").GetString());
        Assert.Equal("Channel.Device.Tag", json.RootElement.GetProperty("itemId").GetString());
        Assert.Single(fake.WriteCalls);
    }

    [Fact]
    public async Task WriteValue_FakeLancaExcecao_RetornaDetalheDeErroAtual()
    {
        var fake = new FakeOpcDaClientService
        {
            WriteException = new InvalidOperationException("falha determinística")
        };
        await using var factory = new OpcDaApiFactory(CreateDaServer(), fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opcda/{ServerId}/write", new
        {
            itemId = "Channel.Device.Tag",
            value = 12.5
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal("Erro ao escrever no servidor OPC DA.", json.RootElement.GetProperty("message").GetString());
        Assert.Equal("falha determinística", json.RootElement.GetProperty("error").GetString());
        Assert.Single(fake.WriteCalls);
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

    private static async Task AssertJsonStringAsync(
        HttpResponseMessage response,
        string propertyName,
        string expected)
    {
        using var json = await ReadJsonAsync(response);
        Assert.Equal(expected, json.RootElement.GetProperty(propertyName).GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
