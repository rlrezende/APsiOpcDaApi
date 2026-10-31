using System.Net;
using System.Text.Json;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Tests.Fakes;
using APsiOpcDaApi.Tests.Infrastructure;
using Moq;

namespace APsiOpcDaApi.Tests.Controllers;

public sealed class OpcDaDiscoveryContractTests
{
    private static readonly Guid UnidadeId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task DiscoverLocal_UnidadeVazia_NegaAntesDeConsultarPlataforma()
    {
        await using var factory = new OpcDaApiFactory(
            server: null,
            opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opcda/discover-local?unidadeId=00000000-0000-0000-0000-000000000000&host=nao-deve-ser-acessado");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("SCOPE_DENIED", await response.Content.ReadAsStringAsync());
        factory.OpcServerService.Verify(service => service.IsOpcDaSupported(), Times.Never);
        factory.OpcServerService.Verify(
            service => service.GetServersByTypeAsync(It.IsAny<TipoOpcServer>()),
            Times.Never);
        Assert.Empty(factory.OpcDaServerEnumerator.RequestedHosts);
    }

    [Fact]
    public async Task DiscoverLocal_UnidadeVaziaEmPlataformaSuportada_NegaAntesDeConsultarServidores()
    {
        await using var factory = new OpcDaApiFactory(
            server: null,
            opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opcda/discover-local?unidadeId=00000000-0000-0000-0000-000000000000&host=nao-deve-ser-acessado");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("SCOPE_DENIED", await response.Content.ReadAsStringAsync());
        factory.OpcServerService.Verify(service => service.IsOpcDaSupported(), Times.Never);
        factory.OpcServerService.Verify(
            service => service.GetServersByTypeAsync(It.IsAny<TipoOpcServer>()),
            Times.Never);
        Assert.Empty(factory.OpcDaServerEnumerator.RequestedHosts);
    }

    [Theory]
    [InlineData("", "localhost")]
    [InlineData("%20%20servidor-alvo%20%20", "servidor-alvo")]
    public async Task DiscoverLocal_HostAusenteOuComEspacos_NormalizaAntesDeEnumerar(
        string hostQuery,
        string expectedHost)
    {
        var enumerator = new FakeOpcDaServerEnumerator();
        await using var factory = new OpcDaApiFactory(
            server: null,
            opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: true,
            opcDaServerEnumerator: enumerator);
        using var client = factory.CreateClient();
        var query = string.IsNullOrEmpty(hostQuery) ? string.Empty : $"&host={hostQuery}";

        var response = await client.GetAsync($"/api/opcda/discover-local?unidadeId={UnidadeId}{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([expectedHost], enumerator.RequestedHosts);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        Assert.Equal(expectedHost, json.RootElement.GetProperty("host").GetString());
        Assert.Equal(0, json.RootElement.GetProperty("totalFound").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("servers").GetProperty("$values").EnumerateArray());
        factory.OpcServerService.Verify(
            service => service.AddAsync(It.IsAny<Application.DTOs.OpcServerDTO>()),
            Times.Never);
    }

    [Fact]
    public async Task DiscoverLocal_EnumeracaoFalha_RetornaErroAtualSemPersistir()
    {
        var enumerator = new FakeOpcDaServerEnumerator
        {
            Exception = new InvalidOperationException("falha deterministica")
        };
        await using var factory = new OpcDaApiFactory(
            server: null,
            opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: true,
            opcDaServerEnumerator: enumerator);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opcda/discover-local?unidadeId={UnidadeId}&host=servidor-alvo");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await SafeProblemAssertions.AssertAsync(response, "OPERATION_UNAVAILABLE");
        Assert.Equal(["servidor-alvo"], enumerator.RequestedHosts);
        factory.OpcServerService.Verify(
            service => service.AddAsync(It.IsAny<Application.DTOs.OpcServerDTO>()),
            Times.Never);
    }

    [Fact]
    public async Task DiscoverLocal_ServidorNovo_MapeiaMetadadosEPersisteAntesDeRetornar()
    {
        var beforeRequest = DateTime.UtcNow;
        var enumerator = new FakeOpcDaServerEnumerator
        {
            Servers =
            [
                new OpcDaDiscoveredServerDTO
                {
                    Nome = "Servidor descoberto",
                    Endpoint = "opcda://servidor-alvo/Fabrica.Servidor",
                    ProgId = "Fabrica.Servidor",
                    ClsId = "11111111-2222-3333-4444-555555555555",
                    Descricao = "Descrição do servidor"
                }
            ]
        };
        await using var factory = new OpcDaApiFactory(
            server: null,
            opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: true,
            opcDaServerEnumerator: enumerator);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/opcda/discover-local?unidadeId={UnidadeId}&host=%20SERVIDOR-ALVO%20");
        var afterRequest = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var added = Assert.Single(factory.AddedServers);
        Assert.Equal("Servidor descoberto", added.Nome);
        Assert.Equal("opcda://servidor-alvo/Fabrica.Servidor", added.Endpoint);
        Assert.Equal("SERVIDOR-ALVO", added.Host);
        Assert.Equal("Fabrica.Servidor", added.ProgId);
        Assert.Equal("11111111-2222-3333-4444-555555555555", added.ClsId);
        Assert.Equal("Descrição do servidor", added.Descricao);
        Assert.Equal(TipoOpcServer.Da, added.Tipo);
        Assert.Equal(UnidadeId, added.ModuloId);
        Assert.True(added.IsOnline);
        Assert.InRange(added.DiscoveryTime!.Value, beforeRequest, afterRequest);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        Assert.Equal("SERVIDOR-ALVO", json.RootElement.GetProperty("host").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("totalFound").GetInt32());
        var returned = Assert.Single(
            json.RootElement.GetProperty("servers").GetProperty("$values").EnumerateArray());
        Assert.Equal("Servidor descoberto", returned.GetProperty("nome").GetString());
        Assert.Equal("Fabrica.Servidor", returned.GetProperty("progId").GetString());
        Assert.Equal(UnidadeId, returned.GetProperty("moduloId").GetGuid());
        Assert.True(returned.GetProperty("isOnline").GetBoolean());
    }

    [Fact]
    public async Task DiscoverLocal_InventarioEDescobertasDuplicadas_ReutilizaPorHostEProgIdSemPersistirDuplicata()
    {
        var existingId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var otherUnitId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var existing = new OpcServerDTO
        {
            Id = existingId,
            Nome = "Servidor já cadastrado",
            Endpoint = "endpoint-existente",
            Host = "SERVIDOR-ALVO",
            ProgId = "Fabrica.Existente",
            Tipo = TipoOpcServer.Da,
            ModuloId = UnidadeId
        };
        var serverFromOtherUnit = new OpcServerDTO
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Nome = "Servidor de outra unidade",
            Endpoint = "endpoint-outra-unidade",
            Host = "servidor-alvo",
            ProgId = "Fabrica.Novo",
            Tipo = TipoOpcServer.Da,
            ModuloId = otherUnitId
        };
        var enumerator = new FakeOpcDaServerEnumerator
        {
            Servers =
            [
                new OpcDaDiscoveredServerDTO
                {
                    Nome = "Nome retornado pela enumeração",
                    Endpoint = "endpoint-descoberto",
                    ProgId = "fabrica.existente"
                },
                new OpcDaDiscoveredServerDTO
                {
                    Nome = "Servidor novo",
                    Endpoint = "endpoint-novo",
                    ProgId = "Fabrica.Novo"
                },
                new OpcDaDiscoveredServerDTO
                {
                    Nome = "Duplicata da descoberta",
                    Endpoint = "endpoint-duplicado",
                    ProgId = "fabrica.novo"
                }
            ]
        };
        await using var factory = new OpcDaApiFactory(
            server: null,
            opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: true,
            opcDaServerEnumerator: enumerator,
            existingServers: [existing, serverFromOtherUnit]);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/opcda/discover-local?unidadeId={UnidadeId}&host=servidor-alvo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var added = Assert.Single(factory.AddedServers);
        Assert.Equal("Servidor novo", added.Nome);
        Assert.Equal("Fabrica.Novo", added.ProgId);
        Assert.Equal(UnidadeId, added.ModuloId);
        factory.OpcServerService.Verify(
            service => service.AddAsync(It.IsAny<OpcServerDTO>()),
            Times.Once);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        Assert.Equal(3, json.RootElement.GetProperty("totalFound").GetInt32());
        var returned = json.RootElement
            .GetProperty("servers")
            .GetProperty("$values")
            .EnumerateArray()
            .ToArray();
        Assert.Equal(existingId, returned[0].GetProperty("id").GetGuid());
        Assert.Equal("Servidor já cadastrado", returned[0].GetProperty("nome").GetString());
        Assert.Equal("Servidor novo", returned[1].GetProperty("nome").GetString());
        Assert.Equal(
            returned[1].GetProperty("$id").GetString(),
            returned[2].GetProperty("$ref").GetString());
    }

    [Fact]
    public async Task DiscoverLocal_ProgIdFilter_SavesOnlyMatchingServerOnRequestedHost()
    {
        var enumerator = new FakeOpcDaServerEnumerator
        {
            Servers = [
                new OpcDaDiscoveredServerDTO { Nome = "Target", Endpoint = "Factory.Target", ProgId = "Factory.Target" },
                new OpcDaDiscoveredServerDTO { Nome = "Other", Endpoint = "Factory.Other", ProgId = "Factory.Other" }
            ]
        };
        await using var factory = new OpcDaApiFactory(
            server: null, opcDaClient: new FakeOpcDaClientService(),
            isOpcDaSupported: true, opcDaServerEnumerator: enumerator);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/opcda/discover-local?unidadeId={UnidadeId}&host=10.1.2.3&progId=%20factory.target%20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "10.1.2.3" }, enumerator.RequestedHosts);
        var saved = Assert.Single(factory.AddedServers);
        Assert.Equal("Factory.Target", saved.ProgId);
        Assert.Equal("10.1.2.3", saved.Host);
        Assert.Equal(UnidadeId, saved.ModuloId);
    }

    private static async Task AssertMessageAsync(HttpResponseMessage response, string expected)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        Assert.Equal(expected, json.RootElement.GetProperty("message").GetString());
    }
}
