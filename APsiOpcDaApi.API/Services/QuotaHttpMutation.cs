using System.Security.Claims;
using System.Text;
using System.Text.Json;
using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiC.Licensing.Infrastructure;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace APsiOpcDaApi.API.Services;

public sealed class QuotaHttpMutation(APsiOpcDaApiContext database, ILocalLicensingRuntime runtime,
    IUserContextService principal)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string Serialize(object value) => JsonSerializer.Serialize(value, Json);

    public async Task<IActionResult> ExecuteAsync(ControllerBase controller, object? payload,
        Func<Task<QuotaHttpResponse?>> execute, Type? resourceType = null)
    {
        var request = controller.Request;
        if (!Guid.TryParse(request.Headers["Idempotency-Key"].ToString(), out var key) || key == Guid.Empty)
            return controller.BadRequest(new { code = "INVALID_IDEMPOTENCY_KEY" });
        var subject = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (controller.User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(subject))
            return controller.Unauthorized();
        if (principal.GetEmpresaId() != runtime.EmpresaId) return controller.NotFound();
        if (System.Transactions.Transaction.Current is not null || database.Database.CurrentTransaction is not null ||
            database.Database.GetDbConnection() is not NpgsqlConnection connection)
            throw new QuotaMutationException("QUOTA_TOPOLOGY_INCOMPATIBLE");
        var hash = HttpMutationFingerprint.Hash(key, Encoding.UTF8.GetBytes(Serialize(new
        {
            subject, company = runtime.EmpresaId, request.Method,
            controller = controller.ControllerContext.ActionDescriptor.ControllerName,
            action = controller.ControllerContext.ActionDescriptor.ActionName,
            route = controller.RouteData.Values.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(p => new { name = p.Key.ToLowerInvariant(), value = Normalize(p.Value?.ToString()) }),
            query = request.Query.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(p => new { name = p.Key.ToLowerInvariant(), values = p.Value.Select(v => Normalize(v)).ToArray() }),
            payload
        })));
        var coordinator = runtime.OpenQuota(connection);
        bool AuthorizeUnit(Guid unit)
        {
            if (controller.HttpContext.Items[CanonicalActionFilter.RequiredAction] is not string action ||
                !Guid.TryParse(subject, out var actor) ||
                !Guid.TryParse(controller.User.FindFirst("SessionId")?.Value, out var session) ||
                !long.TryParse(controller.User.FindFirst("AuthorizationVersion")?.Value, out var version)) return false;
            var current = runtime.OpenAuthorization().BindSession(connection,
                (NpgsqlTransaction)database.Database.CurrentTransaction!.GetDbTransaction(), session, actor, version).Principal;
            if (!ResourceAuthorization.Decide(current, current.AuthorizationVersion, action,
                new(runtime.EmpresaId, unit, null)).Allowed) return false;
            using var mapping = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM apsic_licensing.quota_unit_mapping WHERE product_id='apsic-modern' AND installation_id=$1 AND owner_api='opcda' AND unit_id=$2 AND empresa_id=$3)", connection,
                (NpgsqlTransaction?)database.Database.CurrentTransaction?.GetDbTransaction());
            mapping.Parameters.AddWithValue(coordinator.InstallationId);
            mapping.Parameters.AddWithValue(unit); mapping.Parameters.AddWithValue(runtime.EmpresaId);
            return mapping.ExecuteScalar() is true;
        }
        var entity = database.Model.FindEntityType(resourceType ?? typeof(APsiOpcDaApi.Domain.Entities.OpcServer))!;
        var table = $"\"{entity.GetSchema()}\".\"{entity.GetTableName()}\"";
        await using var transaction = await database.Database.BeginTransactionAsync(request.HttpContext.RequestAborted);
        PostgresQuotaTransactionCache.Remember(database,transaction.TransactionId,coordinator);
        try
        {
            var response = await coordinator.ExecuteHttpAsync(connection, (NpgsqlTransaction)transaction.GetDbTransaction(),
                key, hash, "opcda", [table],
                AuthorizeUnit,
                async () => await execute() ?? throw new MissingResource());
            // The response receipt and all SaveChanges commit together, before delivery.
            await transaction.CommitAsync(request.HttpContext.RequestAborted);
            if (response.Location is not null) controller.Response.Headers.Location = response.Location;
            return new ContentResult { StatusCode = response.Status, Content = response.Json, ContentType = response.Json is null ? null : "application/json; charset=utf-8" };
        }
        catch (MissingResource) { return controller.NotFound(); }
        catch (UnauthorizedAccessException) { return controller.NotFound(); }
        catch (IdentityException error) { throw new QuotaMutationException(error.Code); }
        catch (System.Data.Common.DbException) { throw new QuotaMutationException("STORE_UNAVAILABLE"); }
    }
    private static string Normalize(string? value) => Guid.TryParse(value, out var id) ? id.ToString("D") : value ?? "";
    private sealed class MissingResource : Exception;
}
