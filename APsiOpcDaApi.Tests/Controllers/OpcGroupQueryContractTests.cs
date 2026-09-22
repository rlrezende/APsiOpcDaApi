using System.Net;
using System.Text.Json;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Tests.Fakes;
using APsiOpcDaApi.Tests.Infrastructure;

namespace APsiOpcDaApi.Tests.Controllers;

public sealed class OpcGroupQueryContractTests
{
    private static readonly Guid ServerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UnidadeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid GroupId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetAll_SemUnidade_RetornaEnvelopeEConsultaTodosOsGrupos()
    {
        var groupService = new FakeOpcGroupService { AllGroups = [CreateGroup()] };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opc-groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, groupService.GetAllCalls);
        Assert.Empty(groupService.UnidadeRequests);
        using var json = await ReadJsonAsync(response);
        var group = Assert.Single(GetPreservedArray(json.RootElement.GetProperty("groups")));
        AssertGroup(group);
    }

    [Fact]
    public async Task GetAll_ComUnidade_RetornaEnvelopeEConsultaFiltroDaUnidade()
    {
        var groupService = new FakeOpcGroupService { GroupsByUnidade = [CreateGroup()] };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opc-groups?unidadeId={UnidadeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((UnidadeId, false), Assert.Single(groupService.UnidadeRequests));
        Assert.Equal(0, groupService.GetAllCalls);
        using var json = await ReadJsonAsync(response);
        AssertGroup(Assert.Single(GetPreservedArray(json.RootElement.GetProperty("groups"))));
    }

    [Fact]
    public async Task GetByServer_SemUnidade_RetornaListaEEncaminhaServidor()
    {
        var groupService = new FakeOpcGroupService { GroupsByServer = [CreateGroup()] };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opc-groups/server/{ServerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ServerId, Assert.Single(groupService.ServerRequests));
        using var json = await ReadJsonAsync(response);
        AssertGroup(Assert.Single(GetPreservedArray(json.RootElement)));
    }

    [Fact]
    public async Task GetByServer_UnidadeDiferente_RetornaListaVaziaSemConsultarGrupos()
    {
        var otherUnitId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var groupService = new FakeOpcGroupService { GroupsByServer = [CreateGroup()] };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/opc-groups/server/{ServerId}?unidadeId={otherUnitId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(groupService.ServerRequests);
        using var json = await ReadJsonAsync(response);
        Assert.Empty(GetPreservedArray(json.RootElement));
    }

    [Fact]
    public async Task GetActive_SemUnidade_RetornaListaEConsultaAtivosGlobais()
    {
        var groupService = new FakeOpcGroupService { ActiveGroups = [CreateGroup()] };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opc-groups/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, groupService.GetActiveCalls);
        Assert.Empty(groupService.UnidadeRequests);
        using var json = await ReadJsonAsync(response);
        AssertGroup(Assert.Single(GetPreservedArray(json.RootElement)));
    }

    [Fact]
    public async Task GetActive_ComUnidade_ConsultaFiltroAtivoDaUnidade()
    {
        var groupService = new FakeOpcGroupService { GroupsByUnidade = [CreateGroup()] };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opc-groups/active?unidadeId={UnidadeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((UnidadeId, true), Assert.Single(groupService.UnidadeRequests));
        Assert.Equal(0, groupService.GetActiveCalls);
    }

    [Fact]
    public async Task GetById_GrupoExistente_RetornaPayloadDoFake()
    {
        var groupService = new FakeOpcGroupService { GroupWithTags = CreateGroup() };
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opc-groups/{GroupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(GroupId, Assert.Single(groupService.GroupWithTagsRequests));
        using var json = await ReadJsonAsync(response);
        AssertGroup(json.RootElement);
    }

    [Fact]
    public async Task GetById_GrupoAusente_RetornaNotFoundComProblemDetails()
    {
        var groupService = new FakeOpcGroupService();
        await using var factory = CreateFactory(groupService);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/opc-groups/{GroupId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(GroupId, Assert.Single(groupService.GroupWithTagsRequests));
        using var json = await ReadJsonAsync(response);
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            json.RootElement.GetProperty("type").GetString());
    }

    private static OpcDaApiFactory CreateFactory(FakeOpcGroupService groupService) =>
        new(
            CreateDaServer(),
            new FakeOpcDaClientService(),
            existingServers: [CreateDaServer()],
            opcGroup: groupService);

    private static OpcServerDTO CreateDaServer() => new()
    {
        Id = ServerId,
        ModuloId = UnidadeId,
        Nome = "OPC DA descartável",
        Endpoint = "localhost",
        Tipo = TipoOpcServer.Da
    };

    private static OpcGroupDTO CreateGroup() => new()
    {
        Id = GroupId,
        Name = "Grupo A",
        Description = "Grupo determinístico",
        ServerId = ServerId,
        ServerName = "OPC DA descartável",
        UpdateRate = 1000,
        KeepAliveCount = 10,
        LifetimeCount = 30,
        MaxNotificationsPerPublish = 25,
        Priority = 2,
        Deadband = 0.5,
        HistorianIntervalSeconds = 15,
        AcquisitionMode = 1,
        IsActive = true,
        TagCount = 2,
        CreatedAt = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc),
        LastUpdate = new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
        TagIds =
        [
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            Guid.Parse("50000000-0000-0000-0000-000000000002")
        ]
    };

    private static void AssertGroup(JsonElement group)
    {
        Assert.Equal(GroupId, group.GetProperty("id").GetGuid());
        Assert.Equal("Grupo A", group.GetProperty("name").GetString());
        Assert.Equal(ServerId, group.GetProperty("serverId").GetGuid());
        Assert.True(group.GetProperty("isActive").GetBoolean());
        Assert.Equal(2, group.GetProperty("tagCount").GetInt32());
        Assert.Equal(2, GetPreservedArray(group.GetProperty("tagIds")).Count);
    }

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
