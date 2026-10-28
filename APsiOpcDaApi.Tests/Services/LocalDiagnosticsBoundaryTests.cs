using System.Net;
using APsiC.Licensing.AspNetCore;
using APsiC.Operations;
using APsiOpcDaApi.API.Services;
using APsiOpcDaApi.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
namespace APsiOpcDaApi.Tests.Services;
public sealed class LocalDiagnosticsBoundaryTests
{
    [Theory]
    [InlineData("127.0.0.1")][InlineData("192.0.2.1")]
    public void JwtClaimsNeverSubstituteForLocalWindowsAdministrator(string peer)
    {
        var http=new DefaultHttpContext();http.Connection.RemoteIpAddress=IPAddress.Parse(peer);
        http.User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim("Permission","System.Diagnostics"),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"Administrators")},"TEST-ONLY"));
        var runtime=new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);
        var gate=new LocalDiagnosticsAdmission(new HttpContextAccessor{HttpContext=http},runtime.Object);
        Assert.Equal("LOCAL_ADMIN_REQUIRED",Assert.Throws<OperationAdmissionDeniedException>(()=>gate.Demand()).Code);
        Assert.Empty(runtime.Invocations);
    }
    [Theory]
    [InlineData("capability")][InlineData("navigate")][InlineData("script")][InlineData("dom")]
    public async Task DiagnosticsDenialPrecedesBrowserAndScriptHandling(string action)
    {
        var gate=new Mock<ILocalDiagnosticsAdmission>();gate.Setup(g=>g.Demand()).Throws(new OperationAdmissionDeniedException("LOCAL_ADMIN_REQUIRED"));
        var service=new WebBrowserService(NullLogger<WebBrowserService>.Instance,gate.Object);
        Func<Task> call=action switch
        {
            "capability"=>()=>{service.IsX86Supported();return Task.CompletedTask;},
            "navigate"=>async()=>{await service.NavigateToPageAsync("TEST-ONLY.invalid");},
            "script"=>async()=>{await service.ExecuteScriptAsync("TEST-ONLY");},
            _=>async()=>{await service.ManipulateDomElementAsync("TEST-ONLY","TEST-ONLY");}
        };
        Assert.Equal("LOCAL_ADMIN_REQUIRED",(await Assert.ThrowsAsync<OperationAdmissionDeniedException>(call)).Code);
        gate.Verify(g=>g.Demand(),Times.Once);
    }
}
