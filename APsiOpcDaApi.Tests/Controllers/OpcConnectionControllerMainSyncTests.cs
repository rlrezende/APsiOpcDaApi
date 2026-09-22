using APsiOpcDaApi.API.Controllers;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace APsiOpcDaApi.Tests.Controllers;

public sealed class OpcConnectionControllerMainSyncTests
{
    [Fact]
    public async Task ConnectToServer_ConexaoAprovada_PersisteEstadoAtivoEConectado()
    {
        var server = CreateServer();
        var discovery = new Mock<IOpcDiscoveryService>(MockBehavior.Strict);
        var servers = new Mock<IOpcServerService>(MockBehavior.Strict);
        var groups = new Mock<IOpcGroupService>(MockBehavior.Strict);
        var client = new FakeOpcDaClientService { TestConnectionResult = true };
        servers.Setup(service => service.GetByIdAsync(server.Id)).ReturnsAsync(server);
        servers.Setup(service => service.UpdateAsync(server)).Returns(Task.CompletedTask);
        var controller = new OpcConnectionController(discovery.Object, servers.Object, groups.Object, client);

        var result = await controller.ConnectToServer(server.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(client.TestConnectionCalls);
        Assert.True(server.IsActive);
        Assert.True(server.IsConnected);
        Assert.True(server.IsOnline);
        Assert.Equal("Connected", server.ConnectionStatus);
        Assert.NotNull(server.LastConnection);
        Assert.Null(server.ErrorMessage);
        servers.Verify(service => service.UpdateAsync(server), Times.Once);
    }

    [Fact]
    public async Task GetConnectionStatus_FalhaDeConexao_PersisteEstadoDesconectado()
    {
        var server = CreateServer();
        server.IsActive = true;
        server.IsConnected = true;
        server.IsOnline = true;
        var discovery = new Mock<IOpcDiscoveryService>(MockBehavior.Strict);
        var servers = new Mock<IOpcServerService>(MockBehavior.Strict);
        var groups = new Mock<IOpcGroupService>(MockBehavior.Strict);
        var client = new FakeOpcDaClientService { TestConnectionResult = false };
        servers.Setup(service => service.GetByIdAsync(server.Id)).ReturnsAsync(server);
        servers.Setup(service => service.UpdateAsync(server)).Returns(Task.CompletedTask);
        var controller = new OpcConnectionController(discovery.Object, servers.Object, groups.Object, client);

        var result = await controller.GetConnectionStatus(server.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(server.IsConnected);
        Assert.False(server.IsOnline);
        Assert.Equal("Disconnected", server.ConnectionStatus);
        Assert.Equal("Falha na conexão com o servidor OPC DA.", server.ErrorMessage);
        Assert.True(server.IsActive);
        servers.Verify(service => service.UpdateAsync(server), Times.Once);
    }

    [Fact]
    public async Task ConnectToServer_UnidadeDiferente_NaoTestaNemAlteraServidor()
    {
        var server = CreateServer();
        var discovery = new Mock<IOpcDiscoveryService>(MockBehavior.Strict);
        var servers = new Mock<IOpcServerService>(MockBehavior.Strict);
        var groups = new Mock<IOpcGroupService>(MockBehavior.Strict);
        var client = new FakeOpcDaClientService();
        servers.Setup(service => service.GetByIdAsync(server.Id)).ReturnsAsync(server);
        var controller = new OpcConnectionController(discovery.Object, servers.Object, groups.Object, client);

        var result = await controller.ConnectToServer(server.Id, Guid.NewGuid());

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Empty(client.TestConnectionCalls);
        Assert.False(server.IsActive);
        servers.Verify(service => service.UpdateAsync(It.IsAny<OpcServerDTO>()), Times.Never);
    }

    [Fact]
    public async Task GetConnectionStatus_UnidadeDiferente_NaoTestaNemAlteraServidor()
    {
        var server = CreateServer();
        var discovery = new Mock<IOpcDiscoveryService>(MockBehavior.Strict);
        var servers = new Mock<IOpcServerService>(MockBehavior.Strict);
        var groups = new Mock<IOpcGroupService>(MockBehavior.Strict);
        var client = new FakeOpcDaClientService();
        servers.Setup(service => service.GetByIdAsync(server.Id)).ReturnsAsync(server);
        var controller = new OpcConnectionController(discovery.Object, servers.Object, groups.Object, client);

        var result = await controller.GetConnectionStatus(server.Id, Guid.NewGuid());

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Empty(client.TestConnectionCalls);
        servers.Verify(service => service.UpdateAsync(It.IsAny<OpcServerDTO>()), Times.Never);
    }

    [Fact]
    public async Task ConnectToServer_ConexaoFalha_MantemIntencaoAtivaParaRetentativa()
    {
        var server = CreateServer();
        var discovery = new Mock<IOpcDiscoveryService>(MockBehavior.Strict);
        var servers = new Mock<IOpcServerService>(MockBehavior.Strict);
        var groups = new Mock<IOpcGroupService>(MockBehavior.Strict);
        var client = new FakeOpcDaClientService { TestConnectionResult = false };
        servers.Setup(service => service.GetByIdAsync(server.Id)).ReturnsAsync(server);
        servers.Setup(service => service.UpdateAsync(server)).Returns(Task.CompletedTask);
        var controller = new OpcConnectionController(discovery.Object, servers.Object, groups.Object, client);

        var result = await controller.ConnectToServer(server.Id, server.ModuloId);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(server.IsActive);
        Assert.False(server.IsConnected);
        Assert.False(server.IsOnline);
        Assert.Equal("Disconnected", server.ConnectionStatus);
    }

    private static OpcServerDTO CreateServer() => new()
    {
        Id = Guid.NewGuid(),
        ModuloId = Guid.NewGuid(),
        Nome = "OPC DA de teste",
        Endpoint = "Matrikon.OPC.Simulation.1",
        ProgId = "Matrikon.OPC.Simulation.1"
    };
}
