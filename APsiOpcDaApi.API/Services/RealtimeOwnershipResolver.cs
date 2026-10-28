using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace APsiOpcDaApi.API.Services;
public sealed class RealtimeOwnershipResolver(APsiOpcDaApiContext db,ILocalLicensingRuntime runtime) : IRealtimeOwnershipResolver
{
    public RealtimeOwnership? Resolve(string name,object payload)
    {
        if(name is not ("ReceberSimulacao" or "AtualizarTag")) return null;
        var ids=RealtimePayload.Ids(payload,"tagId",name=="ReceberSimulacao");
        if(ids.Length==0) return null;
        var units=db.Tag.AsNoTracking().Where(t=>ids.Contains(t.Id)).Select(t=>t.ModuloId).ToArray();
        if(units.Length!=ids.Length || units.Any(u=>u==Guid.Empty) || db.Database.GetDbConnection() is not NpgsqlConnection connection) return null;
        var company=runtime.EmpresaId;
        try
        {
            var coordinator=runtime.OpenQuota(connection);
            var close=connection.State==System.Data.ConnectionState.Closed;
            if(close) connection.Open();
            try
            {
                foreach(var unit in units.Distinct())
                {
                    using var command=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM apsic_licensing.quota_unit_mapping WHERE product_id='apsic-modern' AND installation_id=$1 AND owner_api='opcda' AND unit_id=$2 AND empresa_id=$3)",connection);
                    command.Parameters.AddWithValue(coordinator.InstallationId);command.Parameters.AddWithValue(unit);command.Parameters.AddWithValue(company);
                    if(command.ExecuteScalar() is not true) return null;
                }
            }
            finally { if(close) connection.Close(); }
        }
        catch(Exception e) when(e is IdentityException or System.Data.Common.DbException) { return null; }
        return new("Communication.Monitor",units.Distinct().Select(u=>new ResourceOwnership(company,u,null)).ToArray());
    }
}
