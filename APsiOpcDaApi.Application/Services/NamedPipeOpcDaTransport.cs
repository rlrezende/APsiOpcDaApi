using System.IO.Pipes;
using System.Security.Principal;
using System.Text.Json;
using APsiC.OpcDa.PrivateIpc;
using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Services;

// This adapter only transports an API-resolved configured server/item set.
// It cannot issue licenses, infer ownership or accept a desktop host/ProgId grant.
public sealed class NamedPipeOpcDaTransport
{
    public async Task<IReadOnlyList<OpcTagDTO>> ReadAsync(OpcServerDTO server, IReadOnlyList<string> itemIds)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WINDOWS_PRIVATE_IPC_REQUIRED");
        if (Environment.GetEnvironmentVariable("OPC_DA_BRIDGE_PIPE") != PrivatePipeProtocol.PipeName)
            throw new InvalidOperationException("BRIDGE_PRIVATE_IPC_MIGRATION_REQUIRED");
        PrivateServicePipe.DemandCurrentService(PrivatePipeProtocol.ApiServiceName);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var pipe = await PrivateServicePipe.ConnectClientAsync(timeout.Token);
        return await ExchangeAsync(pipe, server, itemIds, () => PrivateServicePipe.DemandServer(pipe), Guid.NewGuid(), timeout.Token);
    }
    internal static async Task<IReadOnlyList<OpcTagDTO>> ExchangeAsync(Stream pipe, OpcServerDTO server, IReadOnlyList<string> itemIds,
        Action authenticateServer, Guid requestId, CancellationToken cancellationToken)
    {
        if (requestId == Guid.Empty || server.Id == Guid.Empty || itemIds.Count is 0 or > 10000 ||
            itemIds.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 2048 || item.Any(char.IsControl)) ||
            itemIds.Distinct(StringComparer.Ordinal).Count() != itemIds.Count)
            throw new InvalidDataException("IPC_INVALID_REQUEST");
        // Never send endpoint/item data to a pipe whose owning service was not verified.
        authenticateServer();
        using var challenge = JsonDocument.Parse(await PrivatePipeProtocol.ReadAsync(pipe, cancellationToken), new JsonDocumentOptions { MaxDepth = 16 });
        ExactFields(challenge.RootElement, "version", "nonce");
        if (challenge.RootElement.GetProperty("version").GetInt32() != PrivatePipeProtocol.Version)
            throw new InvalidDataException("IPC_VERSION_UNSUPPORTED");
        var nonce = challenge.RootElement.GetProperty("nonce").GetString();
        if (nonce is null || nonce.Length != 64 || nonce.Any(c => !Uri.IsHexDigit(c))) throw new InvalidDataException("IPC_INVALID_CHALLENGE");
        var payload = JsonSerializer.Serialize(new
        {
            version = 1, requestId, nonce, serverId = server.Id,
            host = server.Host ?? "localhost", progId = server.ProgId ?? server.Endpoint ?? "", clsId = server.ClsId ?? "", itemIds
        });
        await PrivatePipeProtocol.WriteAsync(pipe, payload, cancellationToken);
        using var response = JsonDocument.Parse(await PrivatePipeProtocol.ReadAsync(pipe, cancellationToken), new JsonDocumentOptions { MaxDepth = 16 });
        ExactFields(response.RootElement, "version", "requestId", "nonce", "result");
        if (response.RootElement.GetProperty("version").GetInt32() != 1 || response.RootElement.GetProperty("requestId").GetGuid() != requestId ||
            response.RootElement.GetProperty("nonce").GetString() != nonce)
            throw new InvalidDataException("IPC_RESPONSE_MISMATCH");
        var result = response.RootElement.GetProperty("result").Deserialize<ReadResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("IPC_INVALID_RESPONSE");
        if (result.Items is null || result.Items.Count > itemIds.Count) throw new InvalidDataException("IPC_INVALID_RESPONSE");
        var allowed = itemIds.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var tags = new List<OpcTagDTO>();
        foreach (var item in result.Items)
        {
            if (item.ItemId is null || !allowed.Contains(item.ItemId) || !seen.Add(item.ItemId) ||
                !DateTimeOffset.TryParse(item.Timestamp, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var timestamp))
                throw new InvalidDataException("IPC_UNEXPECTED_ITEM");
            tags.Add(new OpcTagDTO { NodeId = item.ItemId, DisplayName = item.ItemId, BrowseName = item.ItemId,
                NodeClass = "Variable", DataType = "", ValorAtual = item.Value, Quality = item.Quality ?? "", Timestamp = timestamp.UtcDateTime });
        }
        return tags;
    }
    private static void ExactFields(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new InvalidDataException("IPC_INVALID_RESPONSE");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            if (!names.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name)) throw new InvalidDataException("IPC_UNKNOWN_OR_DUPLICATE_FIELD");
        if (seen.Count != names.Length) throw new InvalidDataException("IPC_INVALID_RESPONSE");
    }
    private sealed record Item(string? ItemId, string? Value, string? Quality, string? Timestamp);
    private sealed record ReadResult(List<Item>? Items, List<string>? Errors);
}
