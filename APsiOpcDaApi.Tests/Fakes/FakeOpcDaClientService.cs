using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;

namespace APsiOpcDaApi.Tests.Fakes;

internal sealed class FakeOpcDaClientService : IOpcDaClientService
{
    public bool IsSupported { get; set; } = true;

    public bool WriteResult { get; set; } = true;

    public bool TestConnectionResult { get; set; } = true;

    public Exception? TestConnectionException { get; set; }

    public Exception? WriteException { get; set; }

    public IReadOnlyList<OpcTagDTO> ReadResult { get; set; } = [];

    public Exception? ReadException { get; set; }

    public OpcBrowseResultDTO BrowseResult { get; set; } = new();

    public Exception? BrowseException { get; set; }

    public List<(OpcServerDTO Server, string ItemId, double Value)> WriteCalls { get; } = [];

    public List<OpcServerDTO> TestConnectionCalls { get; } = [];

    public List<(OpcServerDTO Server, IReadOnlyList<string> ItemIds)> ReadCalls { get; } = [];

    public List<(OpcServerDTO Server, string? ItemId)> BrowseCalls { get; } = [];

    public Task<bool> TestConnectionAsync(OpcServerDTO server)
    {
        TestConnectionCalls.Add(server);

        return TestConnectionException is null
            ? Task.FromResult(TestConnectionResult)
            : Task.FromException<bool>(TestConnectionException);
    }

    public Task<OpcBrowseResultDTO> BrowseAsync(OpcServerDTO server, string? itemId = null)
    {
        BrowseCalls.Add((server, itemId));

        return BrowseException is null
            ? Task.FromResult(BrowseResult)
            : Task.FromException<OpcBrowseResultDTO>(BrowseException);
    }

    public Task<IReadOnlyList<OpcTagDTO>> ReadValuesAsync(
        OpcServerDTO server,
        IEnumerable<string> itemIds)
    {
        ReadCalls.Add((server, itemIds.ToList()));

        if (ReadException is not null)
        {
            return Task.FromException<IReadOnlyList<OpcTagDTO>>(ReadException);
        }

        return Task.FromResult(ReadResult);
    }

    public Task<bool> WriteValueAsync(OpcServerDTO server, string itemId, double value)
    {
        WriteCalls.Add((server, itemId, value));

        if (WriteException is not null)
        {
            return Task.FromException<bool>(WriteException);
        }

        return Task.FromResult(WriteResult);
    }
}
