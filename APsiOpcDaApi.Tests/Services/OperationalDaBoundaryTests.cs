using APsiC.Operations;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
namespace APsiOpcDaApi.Tests.Services;
public sealed class OperationalDaBoundaryTests
{
    [Theory]
    [InlineData("connect")][InlineData("browse")][InlineData("read")][InlineData("write")]
    public void DenialPrecedesTransportAndComConstruction(string action)
    {
        var gate=new Mock<IOperationAdmission>();
        gate.Setup(g=>g.Demand(It.IsAny<ResourceOperation>())).Throws(new OperationAdmissionDeniedException("TEST-ONLY-DENIED"));
        var service=new OpcDaClientService(NullLogger<OpcDaClientService>.Instance,true,gate.Object);
        var server=new OpcServerDTO{Id=Guid.NewGuid(),Tipo=TipoOpcServer.Da,Host="TEST-ONLY.invalid",ProgId="TEST-ONLY"};
        Action call=action switch
        {
            "connect"=>()=>{_=service.TestConnectionAsync(server);},
            "browse"=>()=>{_=service.BrowseAsync(server);},
            "read"=>()=>{_=service.ReadValuesAsync(server,new[]{"TEST-ONLY-item"});},
            _=>()=>{_=service.WriteValueAsync(server,"TEST-ONLY-item",0);}
        };
        Assert.Equal("TEST-ONLY-DENIED",Assert.Throws<OperationAdmissionDeniedException>(call).Code);
        gate.Verify(g=>g.Demand(It.Is<ResourceOperation>(o=>o.ResourceId==server.Id)),Times.Once);
    }
    [Theory]
    [InlineData("connect")][InlineData("browse")][InlineData("read")][InlineData("write")]
    public void MissingAdmissionPrecedesTransportAndComConstruction(string action)
    {
        var service=new OpcDaClientService(NullLogger<OpcDaClientService>.Instance,true);
        var server=new OpcServerDTO{Id=Guid.NewGuid(),Tipo=TipoOpcServer.Da,Host="TEST-ONLY.invalid",ProgId="TEST-ONLY"};
        Action call=action switch
        {
            "connect"=>()=>{_=service.TestConnectionAsync(server);},
            "browse"=>()=>{_=service.BrowseAsync(server);},
            "read"=>()=>{_=service.ReadValuesAsync(server,new[]{"TEST-ONLY-item"});},
            _=>()=>{_=service.WriteValueAsync(server,"TEST-ONLY-item",0);}
        };
        Assert.Equal("OPERATION_ADMISSION_REQUIRED",Assert.Throws<OperationAdmissionDeniedException>(call).Code);
    }
    [Fact] public async Task WriteWithoutDurableExecutorStopsBeforeComEvenWhenAdmissionAllowed()
    {
        var gate=new Mock<IOperationAdmission>();
        var service=new OpcDaClientService(NullLogger<OpcDaClientService>.Instance,true,gate.Object);
        var server=new OpcServerDTO{Id=Guid.NewGuid(),Tipo=TipoOpcServer.Da};
        Assert.Equal("OPERATION_IDEMPOTENCY_REQUIRED",(await Assert.ThrowsAsync<OperationAdmissionDeniedException>(()=>service.WriteValueAsync(server,"TEST-ONLY-item",0))).Code);
    }
    [Fact] public async Task WriteUsesDurableExecutorBeforeConstructingTransport()
    {
        var gate=new Mock<IOperationAdmission>();var executor=new Mock<IExternalCommandExecution>(MockBehavior.Strict);
        var server=new OpcServerDTO{Id=Guid.NewGuid(),Tipo=TipoOpcServer.Da};
        executor.Setup(e=>e.ExecuteAsync(It.Is<ResourceOperation>(r=>r.ResourceId==server.Id && r.Value==0.5 && r.ItemIds!.Single()=="TEST-ONLY-item"),It.IsAny<Func<Task<bool>>>())).ReturnsAsync(true);
        Assert.True(await new OpcDaClientService(NullLogger<OpcDaClientService>.Instance,true,gate.Object,executor.Object).WriteValueAsync(server,"TEST-ONLY-item",0.5));
        gate.Verify(g=>g.Demand(It.IsAny<ResourceOperation>()),Times.Once);
        executor.VerifyAll(); // Fake executor deliberately never invokes the COM callback.
    }

}
