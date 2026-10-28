using APsiC.Licensing;
using APsiC.Licensing.Infrastructure;
using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace APsiOpcDaApi.API.Services;

// Persistence boundary also covers generic CRUD, aliases and background writers
// using this context. Raw SQL/bulk writers still require explicit adapters.
public sealed class QuotaSaveBoundary(ILocalLicensingRuntime runtime) : IQuotaSaveBoundary
{
    public int Save(APsiOpcDaApiContext context, Func<int> persist)
    {
        context.ChangeTracker.DetectChanges();
        var entries = context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is Tag or OpcServer or DetectModelTag or DetectModel).ToArray();
        if (entries.Length == 0) return persist();
        try { return SaveTracked(context, persist, entries); }
        catch (IdentityException error) { throw new QuotaMutationException(error.Code); }
        catch (System.Data.Common.DbException) { throw new QuotaMutationException("STORE_UNAVAILABLE"); }
    }
    private int SaveTracked(APsiOpcDaApiContext context, Func<int> persist, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry[] entries)
    {
        if (System.Transactions.Transaction.Current is not null)
            throw new QuotaMutationException("QUOTA_TOPOLOGY_INCOMPATIBLE");
        if (entries.Any(e => e.State != EntityState.Deleted && e.Entity is OpcServer s && s.Tipo != Domain.Enum.TipoOpcServer.Da))
            throw new QuotaMutationException("PROTOCOL_OWNER_MISMATCH");
        // D04 remains unresolved. Never derive actuator identity from free text/ISA.
        if (entries.Any(e => e.Entity is DetectModelTag or DetectModel))
            throw new QuotaMutationException("RESOURCE_OWNERSHIP_UNRESOLVED");
        if (context.Database.GetDbConnection() is not NpgsqlConnection connection)
            throw new QuotaMutationException("QUOTA_TOPOLOGY_INCOMPATIBLE");
        var coordinator = PostgresQuotaTransactionCache.Get(context,context.Database.CurrentTransaction?.TransactionId,()=>runtime.OpenQuota(connection));
        var owns = context.Database.CurrentTransaction is null;
        using var owned = owns ? context.Database.BeginTransaction() : null;
        var transaction = (NpgsqlTransaction)context.Database.CurrentTransaction!.GetDbTransaction();
        PostgresQuotaTransactionCache.Remember(context,context.Database.CurrentTransaction!.TransactionId,coordinator);
        var tables = entries.Select(e => $"\"{e.Metadata.GetSchema()}\".\"{e.Metadata.GetTableName()}\"").Distinct().ToArray();
        var count = 0;
        var operation = Guid.NewGuid();
        var hash = MachineEvidence.Hash(System.Text.Encoding.UTF8.GetBytes(operation.ToString()));
        // Metadata and telemetry do not expand the source envelope. All other
        // modified configuration fields require a currently enabled capability.
        var required = entries.Where(e => e.State == EntityState.Modified && e.Properties.Any(p => p.IsModified &&
            p.Metadata.Name is not ("Id" or "Nome" or "Descricao" or "Name" or "Description" or "ModuloNome" or
                "CreatedDate" or "UpdatedDate" or "CreatedBy" or "UpdatedBy" or "ValorAtual" or
                "IsConnected" or "IsOnline" or "LastConnection" or "DiscoveryTime" or "ConnectionStatus" or "ErrorMessage" or "ResponseTime")))
            .Select(e => e.Entity switch
            {
                Tag => "acquisition.tags",
                OpcServer server => server.Tipo switch
                {
                    Domain.Enum.TipoOpcServer.Ua => "communication.opcua",
                    Domain.Enum.TipoOpcServer.Da => "communication.opcda",
                    Domain.Enum.TipoOpcServer.Database => "communication.database",
                    _ => throw new QuotaMutationException("RESOURCE_METRIC_UNRESOLVED")
                },
                _ => throw new QuotaMutationException("RESOURCE_METRIC_UNRESOLVED")
            }).Distinct().ToArray();
        var result = coordinator.Execute(connection, transaction, operation, hash, null, "opcda", tables, current =>
        {
            var next = current.ToList();
            foreach (var entry in entries)
            {
                var id = (Guid)entry.Property("Id").CurrentValue!;
                var kind = entry.Metadata.ClrType.Name;
                var resource = new ResourceRef("opcda", kind, id);
                bool Mapped(Guid unit)
                {
                    using var mapping = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM apsic_licensing.quota_unit_mapping WHERE product_id='apsic-modern' AND installation_id=$1 AND owner_api='opcda' AND unit_id=$2 AND empresa_id=$3)", connection, transaction);
                    mapping.Parameters.AddWithValue(coordinator.InstallationId);
                    mapping.Parameters.AddWithValue(unit); mapping.Parameters.AddWithValue(runtime.EmpresaId);
                    return mapping.ExecuteScalar() is true;
                }
                if (!Mapped((Guid)entry.Property("ModuloId").CurrentValue!) ||
                    entry.State != EntityState.Added && !Mapped(entry.Entity is Tag ? context.Tag.AsNoTracking().Where(x => x.Id == id).Select(x => x.ModuloId).SingleOrDefault() : context.OpcServers.AsNoTracking().Where(x => x.Id == id).Select(x => x.ModuloId).SingleOrDefault()))
                    throw new QuotaMutationException("RESOURCE_OWNERSHIP_UNRESOLVED");
                if (entry.State == EntityState.Modified && !current.Any(c => c.Configuration == resource))
                    throw new QuotaMutationException("QUOTA_RECOVERY_REQUIRED");
                next.RemoveAll(c => c.Configuration == resource);
                if (entry.State == EntityState.Deleted) continue;
                var capability = entry.Entity switch
                {
                    Tag => "acquisition.tags",
                    OpcServer server => server.Tipo switch
                    {
                        Domain.Enum.TipoOpcServer.Ua => "communication.opcua",
                        Domain.Enum.TipoOpcServer.Da => "communication.opcda",
                        Domain.Enum.TipoOpcServer.Database => "communication.database",
                        _ => throw new QuotaMutationException("RESOURCE_METRIC_UNRESOLVED")
                    },
                    _ => throw new QuotaMutationException("RESOURCE_METRIC_UNRESOLVED")
                };
                next.Add(new(capability, resource, resource));
            }
            return next;
        }, (_, _) => count = persist(), required);
        if (result.Code != "APPLIED") throw new QuotaMutationException(result.Code, result.Plan);
        owned?.Commit();
        return count;
    }
}
