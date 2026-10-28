using APsiC.Operations;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;
using Moq;
namespace APsiOpcDaApi.Tests.Services;
public sealed class LegacyUaDiscoveryBoundaryTests
{
    [Theory]
    [InlineData("scan")][InlineData("manual")][InlineData("test")][InlineData("local")]
    public void UaDiscoveryAliasesNeverStartNetworkOrComInDa(string operation)
    {
        var servers=new Mock<IOpcServerService>(MockBehavior.Strict);
        var subject=new OpcDiscoveryService(Mock.Of<IOpcDiscoveredServerRepository>(),servers.Object,Mock.Of<IMapper>());
        Action action=operation switch
        {
            "scan"=>()=>{_=subject.ScanNetworkAsync(Guid.NewGuid());},
            "manual"=>()=>{_=subject.AddManualServerAsync(Guid.NewGuid(),"TEST-ONLY","opc.tcp://TEST-ONLY.invalid");},
            "test"=>()=>{_=subject.TestServerConnectionAsync("opc.tcp://TEST-ONLY.invalid");},
            _=>()=>{_=subject.DiscoverLocalhostAsync(Guid.NewGuid());}
        };
        Assert.Equal("PROTOCOL_OWNER_MISMATCH",Assert.Throws<OperationAdmissionDeniedException>(action).Code);
        Assert.Empty(servers.Invocations);
    }
    [Fact]
    public async Task ListingAndStatusUseOwnedPersistedEvidenceWithoutFabricatedProbeMetrics()
    {
        var server=new OpcServerDTO{Id=Guid.NewGuid(),Tipo=TipoOpcServer.Da,Nome="TEST-ONLY",IsConnected=true,IsOnline=true,ResponseTime=42,LastConnection=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc)};
        var servers=new Mock<IOpcServerService>(MockBehavior.Strict);
        servers.Setup(s=>s.GetAllAsync()).ReturnsAsync(new List<OpcServerDTO>{server});
        servers.Setup(s=>s.GetByIdAsync(server.Id)).ReturnsAsync(server);
        var subject=new OpcDiscoveryService(Mock.Of<IOpcDiscoveredServerRepository>(),servers.Object,Mock.Of<IMapper>());
        Assert.Equal(server.Id,Assert.Single(await subject.GetDiscoveredServersAsync()).Id);
        var status=Assert.Single(await subject.GetActiveConnectionsAsync());
        Assert.Equal(server.LastConnection,status.LastConnection);Assert.Equal(42,status.ResponseTime);
    }
}
