using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;

namespace APsiOpcDaApi.Tests.Fakes;

internal sealed class FakeOpcBrowserService : IOpcBrowserService
{
    public OpcBrowseResultDTO BrowseResult { get; set; } = new();

    public Exception? BrowseException { get; set; }

    public List<(Guid ServerId, string? ParentNodeId)> BrowseCalls { get; } = [];

    public Task<OpcBrowseResultDTO> BrowseNodesAsync(Guid serverId, string? parentNodeId = null)
    {
        BrowseCalls.Add((serverId, parentNodeId));

        return BrowseException is null
            ? Task.FromResult(BrowseResult)
            : Task.FromException<OpcBrowseResultDTO>(BrowseException);
    }
}
