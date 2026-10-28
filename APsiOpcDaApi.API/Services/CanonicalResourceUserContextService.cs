using APsiC.Operations;
using System.Security.Claims;
using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace APsiOpcDaApi.API.Services;

// A recorded installation mapping is required; legacy Modulo claims are never units.
public sealed class CanonicalResourceUserContextService(IHttpContextAccessor accessor,
    APsiOpcDaApiContext database, ILocalLicensingRuntime runtime) : IUserContextService
{
    public Guid? GetEmpresaId()
    {
        if(accessor.HttpContext is null && WorkerResourceScope.Current is { } worker)
            return worker.CompanyId==runtime.EmpresaId ? worker.CompanyId : null;
        var user = accessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(user.FindFirst("IdEmpresa")?.Value, out var company) && company == runtime.EmpresaId
            ? company : null;
    }
    public bool HasAccessToResource(object resource)
    {
        var company = GetEmpresaId();
        if (company is null) return false;
        Guid ServerUnit(Guid id) => database.OpcServers.AsNoTracking().Where(x => x.Id == id).Select(x => x.ModuloId).SingleOrDefault();
        Guid TagUnit(Tag tag)
        {
            // Both the explicit owner and every referenced parent must agree.
            if (tag.ModuloId == Guid.Empty) return Guid.Empty;
            Guid? groupServer = null, nodeServer = null;
            if (tag.GroupId is Guid group)
            {
                groupServer = database.OpcGroups.AsNoTracking().Where(x => x.Id == group).Select(x => (Guid?)x.ServerId).SingleOrDefault();
                if (groupServer is null || ServerUnit(groupServer.Value) != tag.ModuloId) return Guid.Empty;
            }
            if (tag.NodeId is Guid node)
            {
                nodeServer = database.OpcNodes.AsNoTracking().Where(x => x.Id == node).Select(x => (Guid?)x.ServerId).SingleOrDefault();
                if (nodeServer is null || ServerUnit(nodeServer.Value) != tag.ModuloId) return Guid.Empty;
            }
            return groupServer.HasValue && nodeServer.HasValue && groupServer != nodeServer ? Guid.Empty : tag.ModuloId;
        }
        var unit = resource switch
        {
            OpcServer server => server.ModuloId,
            OpcGroup group => ServerUnit(group.ServerId),
            OpcNode node => ServerUnit(node.ServerId),
            Tag tag => TagUnit(tag),
            Leitura reading => database.Tag.AsNoTracking().Where(x => x.Id == reading.TagId).AsEnumerable().Select(TagUnit).SingleOrDefault(),
            _ => Guid.Empty
        };
        if(accessor.HttpContext is null && WorkerResourceScope.Current is { } worker)
        {
            Guid? TagServer(Guid tagId)=>database.Tag.AsNoTracking().Where(t=>t.Id==tagId).Select(t=>t.Group!=null ? (Guid?)t.Group.ServerId : t.Node!=null ? (Guid?)t.Node.ServerId : null).SingleOrDefault();
            Guid? serverId=resource switch
            {
                OpcServer server=>server.Id,OpcGroup group=>group.ServerId,OpcNode node=>node.ServerId,
                Tag tag=>tag.GroupId is Guid group ? database.OpcGroups.AsNoTracking().Where(g=>g.Id==group).Select(g=>(Guid?)g.ServerId).SingleOrDefault()
                    : tag.NodeId is Guid node ? database.OpcNodes.AsNoTracking().Where(n=>n.Id==node).Select(n=>(Guid?)n.ServerId).SingleOrDefault() : null,
                Leitura reading=>TagServer(reading.TagId),_=>null
            };
            if(serverId!=worker.ServerId || unit!=worker.UnitId)return false;
        }
        var user = accessor.HttpContext?.User;
        if (unit == Guid.Empty) return false;
        if (accessor.HttpContext?.Items[typeof(PrincipalAccess)] is PrincipalAccess canonical)
        {
            if (!CanonicalActionFilter.AllowsUnit(accessor.HttpContext, canonical, unit)) return false;
        }
        else if (accessor.HttpContext is not null && !user!.FindAll("UnidadeId").Any(c => Guid.TryParse(c.Value, out var id) && id == unit)) return false;
        if (database.Database.GetDbConnection() is not NpgsqlConnection connection) return false;
        try
        {
            var coordinator = runtime.OpenQuota(connection);
            var close = connection.State == System.Data.ConnectionState.Closed;
            if (close) connection.Open();
            try
            {
                using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM apsic_licensing.quota_unit_mapping WHERE product_id='apsic-modern' AND installation_id=$1 AND owner_api='opcda' AND unit_id=$2 AND empresa_id=$3)", connection);
                command.Parameters.AddWithValue(coordinator.InstallationId); command.Parameters.AddWithValue(unit); command.Parameters.AddWithValue(company.Value);
                return command.ExecuteScalar() is true;
            }
            finally { if (close) connection.Close(); }
        }
        catch (IdentityException) { return false; }
        catch (System.Data.Common.DbException) { return false; }
    }
}
