using APsiC.Licensing.Infrastructure;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiC.Operations;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace APsiOpcDaApi.API.Services;

public sealed class OperationAdmission(DbContextOptions<APsiOpcDaApiContext> options,ILocalLicensingRuntime runtime,IHttpContextAccessor accessor,ILogger<OperationAdmission>? logger=null):IOperationAdmission,IWorkerOperationAccess,IEndpointOperationAdmission,IExternalCommandExecution
{
    public void DemandEndpoint(string protocol,string endpoint,string permission,string command)
    {
        if(protocol!="opcda")throw new OperationAdmissionDeniedException("PROTOCOL_OWNER_MISMATCH");
        if(string.IsNullOrWhiteSpace(endpoint) || endpoint.Length>253 || endpoint.Any(char.IsControl))
            throw new OperationAdmissionDeniedException("OPERATIONAL_QUALIFICATION_REQUIRED");
        try
        {
            using var db=new APsiOpcDaApiContext(options);
            var host=endpoint.Trim().ToLowerInvariant();
            var ids=db.OpcServers.AsNoTracking().Where(s=>(int)s.Tipo==2 && (s.Host??"localhost").ToLower()==host).Select(s=>s.Id).ToArray();
            OperationAdmissionDeniedException? denial=null;
            foreach(var id in ids)
            {
                try { Demand(new("OpcServer",id,OperationKind.Connect,permission,command)); return; }
                catch(OperationAdmissionDeniedException e) { denial=e; }
            }
            throw denial??new OperationAdmissionDeniedException("OPERATIONAL_QUALIFICATION_REQUIRED");
        }
        catch(System.Data.Common.DbException) { throw new OperationAdmissionDeniedException("OPERATIONAL_STORE_UNAVAILABLE"); }
    }
    public IReadOnlyList<Guid> ConfiguredServers(string capability)
    {
        if(accessor.HttpContext is not null)throw new OperationAdmissionDeniedException("EXPLICIT_WORKER_REQUIRED");
        try{return runtime.OpenOperational().ReadWorkerResources("opcda",capability,"Communication.Monitor").Select(r=>r.Resource.Id).ToArray();}
        catch(IdentityException e){throw new OperationAdmissionDeniedException(e.Code);}
        catch(System.Data.Common.DbException){throw new OperationAdmissionDeniedException("OPERATIONAL_STORE_UNAVAILABLE");}
    }
    public IDisposable Enter(Guid serverId)
    {
        if(accessor.HttpContext is not null)throw new OperationAdmissionDeniedException("EXPLICIT_WORKER_REQUIRED");
        var resource=Describe(new("OpcServer",serverId,OperationKind.AcquireCycle,"Communication.Monitor","opc.read"));
        var command=resource.Capability=="communication.database" ? "database.read" : "opc.read";
        Demand(new("OpcServer",serverId,OperationKind.AcquireCycle,"Communication.Monitor",command));
        return new WorkerResourceScope(resource.Ownership.EmpresaId,resource.Ownership.UnidadeId,serverId);
    }
    public IDisposable EnterForTag(Guid tagId)
    {
        using var db=new APsiOpcDaApiContext(options);
        var server=db.Tag.AsNoTracking().Where(t=>t.Id==tagId).Select(t=>t.Group!=null ? (Guid?)t.Group.ServerId : t.Node!=null ? (Guid?)t.Node.ServerId : null).SingleOrDefault();
        if(server is not Guid id)throw new OperationAdmissionDeniedException("RESOURCE_OWNERSHIP_UNRESOLVED");
        return Enter(id);
    }
    public void Demand(ResourceOperation operation)
    {
        using var decision=new OperationalDecisionTelemetry(operation.Permission,operation.Operation.ToString(),
            accessor.HttpContext?.TraceIdentifier??APsiC.Operations.AnalyticWorkerScope.Current?.BatchId.ToString());
        try{DemandCore(operation,decision);decision.Allowed();}
        catch(QuotaMutationException e){throw Denied(e.Code,decision);}
        catch(IdentityException e){throw Denied(e.Code,decision);}
        catch(System.Data.Common.DbException){throw Denied("OPERATIONAL_STORE_UNAVAILABLE",decision);}
        catch(OperationAdmissionDeniedException e){decision.Denied(e.Code);throw;}
    }
    private OperationAdmissionDeniedException Denied(string code,OperationalDecisionTelemetry decision)
    {
        decision.Denied(code);
        logger?.LogWarning("Operational decision {DecisionId} denied: {Reason}; capability {Capability}.",decision.DecisionId,code,decision.Capability);
        return new(code);
    }
    public async Task<bool> ExecuteAsync(ResourceOperation operation,Func<Task<bool>> send)
    {
        try
        {
            var actor=AuthorizeHttp(operation)??throw new IdentityException("CANONICAL_SESSION_REQUIRED");
            var values=accessor.HttpContext!.Request.Headers["Idempotency-Key"];
            if(values.Count!=1 || !Guid.TryParse(values[0],out var key) || key==Guid.Empty)throw new IdentityException("IDEMPOTENCY_KEY_REQUIRED");
            var resource=Describe(operation);
            var command=new OperationalCommand(Enum.Parse<LicensedOperation>(operation.Operation.ToString()),operation.CommandKey,operation.Value);
            return await runtime.OpenOperational().ExecuteExternalCommandAsync(resource,command,actor,operation.Permission,key,send,()=>Demand(operation));
        }
        catch(IdentityException e){throw new OperationAdmissionDeniedException(e.Code);}
        catch(System.Data.Common.DbException){throw new OperationAdmissionDeniedException("OPERATIONAL_STORE_UNAVAILABLE");}
    }
    private void DemandCore(ResourceOperation operation,OperationalDecisionTelemetry decision)
    {
        // Direct service calls must validate canonical scope before licensing diagnostics.
        var actor=AuthorizeHttp(operation);
        var resource=Describe(operation);
        decision.Capability=resource.Capability;
        if(operation.Operation is OperationKind.HistoryRead or OperationKind.HistoryExport)
        {
            if(actor is null)throw new IdentityException("CANONICAL_SESSION_REQUIRED");
            return;
        }
        try
        {
            var store=runtime.OpenOperational();
            var command=new OperationalCommand(Enum.Parse<LicensedOperation>(operation.Operation.ToString()),operation.CommandKey,operation.Value);
            var result=store.Evaluate(resource,command,actor,operation.Permission);
            if(!result.Allowed && result.Code=="RESOURCE_ADMISSION_REQUIRED" && operation.Operation is OperationKind.Connect or OperationKind.AcquireCycle or OperationKind.IndustrialCycle)
            {
                var admitted=store.Admit(resource,actor,operation.Permission,"Backend resolved persisted resource and reviewed qualification");
                if(admitted.Allowed)result=store.Evaluate(resource,command,actor,operation.Permission);
                else result=admitted;
            }
            if(!result.Allowed)throw new QuotaMutationException(result.Code);
        }
        catch(IdentityException e){throw new QuotaMutationException(e.Code);}
        catch(System.Data.Common.DbException){throw new QuotaMutationException("OPERATIONAL_STORE_UNAVAILABLE");}
    }
    private OperationalSupervisor? AuthorizeHttp(ResourceOperation operation)
    {
        var http=accessor.HttpContext;
        if(http is null)return null;
        if(http.User.Identity?.IsAuthenticated!=true || !Guid.TryParse(http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,out var principal) ||
            !Guid.TryParse(http.User.FindFirst("SessionId")?.Value,out var session) || !long.TryParse(http.User.FindFirst("AuthorizationVersion")?.Value,out var version))
            throw new IdentityException("CANONICAL_SESSION_REQUIRED");
        var current=runtime.OpenAuthorization().Revalidate(session,principal,version).Principal;
        if(operation.ResourceKind!="OpcServer")throw new IdentityException("RESOURCE_OWNERSHIP_UNRESOLVED");
        using var db=new APsiOpcDaApiContext(options);
        var unit=db.OpcServers.AsNoTracking().Where(r=>r.Id==operation.ResourceId).Select(r=>(Guid?)r.ModuloId).SingleOrDefault();
        if(unit is not Guid owner)throw new IdentityException("RESOURCE_OWNERSHIP_UNRESOLVED");
        var decision=ResourceAuthorization.Decide(current,current.AuthorizationVersion,operation.Permission,new(runtime.EmpresaId,owner,null));
        if(!decision.Allowed)throw new IdentityException(decision.Code=="SCOPE_DENIED" ? "RESOURCE_OWNERSHIP_UNRESOLVED" : decision.Code);
        return new(session,principal,version);
    }
    public OperationalResource Describe(ResourceOperation operation)
    {
        if(operation.ResourceKind!="OpcServer" || operation.ResourceId==Guid.Empty)throw new QuotaMutationException("RESOURCE_OWNERSHIP_UNRESOLVED");
        using var db=new APsiOpcDaApiContext(options);
        var server=db.OpcServers.AsNoTracking().SingleOrDefault(s=>s.Id==operation.ResourceId)??throw new QuotaMutationException("RESOURCE_OWNERSHIP_UNRESOLVED");
        var company=runtime.EmpresaId;var unit=server.ModuloId;
        if(db.Database.GetDbConnection() is not NpgsqlConnection connection)throw new QuotaMutationException("QUOTA_TOPOLOGY_INCOMPATIBLE");
        var coordinator=runtime.OpenQuota(connection);connection.Open();
        using(var mapping=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM apsic_licensing.quota_unit_mapping WHERE installation_id=$1 AND product_id='apsic-modern' AND owner_api='opcda' AND unit_id=$2 AND empresa_id=$3)",connection))
        {mapping.Parameters.AddWithValue(coordinator.InstallationId);mapping.Parameters.AddWithValue(unit);mapping.Parameters.AddWithValue(company);if(mapping.ExecuteScalar() is not true)throw new QuotaMutationException("RESOURCE_OWNERSHIP_UNRESOLVED");}
        if((int)server.Tipo!=2)throw new QuotaMutationException("PROTOCOL_OWNER_MISMATCH");
        var tags=db.Tag.AsNoTracking().Where(t=>(t.Node!=null && t.Node.ServerId==server.Id) || (t.Group!=null && t.Group.ServerId==server.Id))
            .Select(t=>new{t.Id,unit=t.ModuloId,t.NodeIdOpc,t.Monitora,t.GroupId,t.NodeId}).ToArray();
        if(tags.Any(t=>t.unit!=unit))throw new QuotaMutationException("RESOURCE_OWNERSHIP_UNRESOLVED");
        if(operation.ItemIds is not null && operation.ItemIds.Any(item=>!tags.Any(t=>t.NodeIdOpc==item)))throw new QuotaMutationException("OPERATION_OUTSIDE_ENVELOPE");
        var critical=new {server.Id,server.ModuloId,server.Tipo,server.Endpoint,server.Host,server.ProgId,server.ClsId,
            server.SecurityPolicy,server.SecurityMode,server.Username,server.Password,server.Provider,server.ConnectionString};
        if(operation.Configuration is OpcServerDTO supplied)
        {
            var requested=new {supplied.Id,supplied.ModuloId,supplied.Tipo,supplied.Endpoint,supplied.Host,supplied.ProgId,supplied.ClsId,
                supplied.SecurityPolicy,supplied.SecurityMode,supplied.Username,supplied.Password,supplied.Provider,supplied.ConnectionString};
            if(JsonSerializer.Serialize(critical)!=JsonSerializer.Serialize(requested))throw new QuotaMutationException("OPERATION_OUTSIDE_ENVELOPE");
        }
        var groups=db.OpcGroups.AsNoTracking().Where(g=>g.ServerId==server.Id).OrderBy(g=>g.Id)
            .Select(g=>new{g.Id,g.UpdateRate,g.HistorianIntervalSeconds,g.AcquisitionMode,g.Deadband,g.IsActive,g.KeepAliveCount,g.LifetimeCount,g.MaxNotificationsPerPublish,g.Priority}).ToArray();
        var hash=MachineEvidence.Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{critical,tags=tags.OrderBy(t=>t.Id).ToArray(),groups})));
        var capability=(int)server.Tipo switch {1=>"communication.opcua",2=>"communication.opcda",3=>"communication.database",_=>throw new QuotaMutationException("RESOURCE_METRIC_UNRESOLVED")};
        return new(new("opcda","OpcServer",server.Id),new(company,unit,null),capability,hash,"configured-acquisition-v1");
    }
}
