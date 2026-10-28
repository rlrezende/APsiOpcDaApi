using APsiC.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace APsiOpcDaApi.API.Services;
public sealed class OperationAdmissionExceptionFilter:IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if(context.Exception is not OperationAdmissionDeniedException error)return;
        var status=error.Code switch
        {
            "OPERATIONAL_STORE_UNAVAILABLE" or "STORE_UNAVAILABLE" or "LICENSE_STORE_UNAVAILABLE"=>503,
            "CANONICAL_SESSION_REQUIRED" or "AUTH_VERSION_STALE"=>401,
            "RESOURCE_OWNERSHIP_UNRESOLVED" or "SCOPE_DENIED"=>404,
            "COMMAND_OUTCOME_UNKNOWN" or "COMMAND_RECEIPT_CORRUPT" or "QUOTA_EXCEEDED" or "LICENSE_BUSY" or "GENERATION_CONFLICT" or "IDEMPOTENCY_CONFLICT"=>409,
            "IDEMPOTENCY_KEY_REQUIRED"=>400,
            "INVALID_LICENSE" or "RESOURCE_CONFIGURATION_REQUIRED"=>422,
            _=>403
        };
        var problem=APsiC.Licensing.AspNetCore.LicensingProblems.Create(error.Code,status,context.HttpContext.TraceIdentifier,context.HttpContext.Request.Path);
        context.Result=new ObjectResult(problem){StatusCode=status,ContentTypes={"application/problem+json"}};context.ExceptionHandled=true;
    }
}
