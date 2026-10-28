using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiC.Operations;
namespace APsiOpcDaApi.API.Services;
// A JWT grant alone never proves membership of the local Windows Administrators
// group. Without a separately authenticated Windows identity this stays closed.
public sealed class LocalDiagnosticsAdmission(IHttpContextAccessor accessor,ILocalLicensingRuntime runtime):ILocalDiagnosticsAdmission
{
    public void Demand()
    {
        var http=accessor.HttpContext;
        if(http?.Connection.RemoteIpAddress is not IPAddress remote || !IPAddress.IsLoopback(remote) || !OperatingSystem.IsWindows())
            throw new OperationAdmissionDeniedException("LOCAL_ADMIN_REQUIRED");
        var windows=http.User.Identities.OfType<WindowsIdentity>().FirstOrDefault(i=>i.IsAuthenticated);
        if(windows is null || !new WindowsPrincipal(windows).IsInRole(WindowsBuiltInRole.Administrator))
            throw new OperationAdmissionDeniedException("LOCAL_ADMIN_REQUIRED");
        if(!Guid.TryParse(http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,out var principal) ||
            !Guid.TryParse(http.User.FindFirst("SessionId")?.Value,out var session) || !long.TryParse(http.User.FindFirst("AuthorizationVersion")?.Value,out var version))
            throw new OperationAdmissionDeniedException("CANONICAL_SESSION_REQUIRED");
        try
        {
            var current=runtime.OpenAuthorization().Revalidate(session,principal,version).Principal;
            if(current.EmpresaId!=runtime.EmpresaId || current.ScopedGrants?.Any(g=>g.Permission=="System.Diagnostics" && g.CompanyWide)!=true)
                throw new OperationAdmissionDeniedException("PERMISSION_DENIED");
        }
        catch(IdentityException e){throw new OperationAdmissionDeniedException(e.Code);}
        catch(System.Data.Common.DbException){throw new OperationAdmissionDeniedException("STORE_UNAVAILABLE");}
    }
}
