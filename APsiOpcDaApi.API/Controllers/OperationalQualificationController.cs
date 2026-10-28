using System.Security.Claims;
using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiC.Operations;
using APsiOpcDaApi.API.Services;
using Microsoft.AspNetCore.Mvc;
namespace APsiOpcDaApi.API.Controllers;
[ApiController,Route("api/operational")]
public sealed class OperationalQualificationController(OperationAdmission admission,ILocalLicensingRuntime runtime):ControllerBase
{
    [HttpGet("servers/{id:guid}/qualification-request"),CanonicalAction("Installation.Manage")]
    public IActionResult QualificationRequest(Guid id)
    {
        var resource=admission.Describe(new("OpcServer",id,OperationKind.MetadataEdit,"Installation.Manage","qualification.request"));
        if(HttpContext.Items[typeof(PrincipalAccess)] is not PrincipalAccess principal ||
            !ResourceAuthorization.Decide(principal,principal.AuthorizationVersion,"Installation.Manage",resource.Ownership).Allowed)return NotFound();
        return Ok(new{schemaVersion=1,resource,qualification="REQUIRES_EXTERNAL_FUNCTIONAL_EVIDENCE",windowsHomologation="NOT_EXECUTED"});
    }
    [HttpGet("servers/{id:guid}/status"),CanonicalAction("Installation.Manage")]
    public IActionResult Status(Guid id)
    {
        var resource=admission.Describe(new("OpcServer",id,OperationKind.MetadataEdit,"Installation.Manage","qualification.request"));
        if(HttpContext.Items[typeof(PrincipalAccess)] is not PrincipalAccess principal ||
            !ResourceAuthorization.Decide(principal,principal.AuthorizationVersion,"Installation.Manage",resource.Ownership).Allowed)return NotFound();
        try
        {
            var status=runtime.OpenOperational().Inspect(resource);
            return Ok(new{resource,status,isSupervisor=status.SupervisorPrincipalId==principal.PrincipalId});
        }
        catch(IdentityException e){return StatusCode(403,new{code=e.Code});}
        catch(System.Data.Common.DbException){return StatusCode(503,new{code="OPERATIONAL_STORE_UNAVAILABLE"});}
    }
    public sealed record HoldRequest(string Reason);
    [HttpPost("servers/{id:guid}/safety-hold"),CanonicalAction("Installation.Manage")]
    public IActionResult EnterHold(Guid id,[FromBody]HoldRequest request,[FromHeader(Name="Idempotency-Key"),System.ComponentModel.DataAnnotations.Required]Guid? operationKey)
        =>Hold(id,request,operationKey,false);
    [HttpPost("servers/{id:guid}/safety-hold/close"),CanonicalAction("Installation.Manage")]
    public IActionResult CloseHold(Guid id,[FromBody]HoldRequest request,[FromHeader(Name="Idempotency-Key"),System.ComponentModel.DataAnnotations.Required]Guid? operationKey)
        =>Hold(id,request,operationKey,true);
    private IActionResult Hold(Guid id,HoldRequest request,Guid? operationKey,bool close)
    {
        if(operationKey is null || operationKey==Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length>1024)return BadRequest(new{code="INVALID_SAFETY_HOLD_REQUEST"});
        var resource=admission.Describe(new("OpcServer",id,OperationKind.MetadataEdit,"Installation.Manage","qualification.request"));
        if(HttpContext.Items[typeof(PrincipalAccess)] is not PrincipalAccess principal ||
            !ResourceAuthorization.Decide(principal,principal.AuthorizationVersion,"Installation.Manage",resource.Ownership).Allowed)return NotFound();
        if(!Guid.TryParse(User.FindFirst("SessionId")?.Value,out var session))return Unauthorized();
        try
        {
            var result=runtime.OpenOperational().ChangeSafetyHold(resource,new(session,principal.PrincipalId,principal.AuthorizationVersion),close,request.Reason,operationKey);
            return StatusCode(result.Allowed?200:403,result);
        }
        catch(IdentityException e){return StatusCode(e.Code=="IDEMPOTENCY_CONFLICT"?409:403,new{code=e.Code});}
        catch(System.Data.Common.DbException){return StatusCode(503,new{code="OPERATIONAL_STORE_UNAVAILABLE"});}
    }
}
