using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcBrowserService
    {
        Task<OpcBrowseResultDTO> BrowseNodesAsync(Guid serverId, string? parentNodeId = null);
    }
}

