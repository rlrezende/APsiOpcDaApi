using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Interfaces
{
    public interface IOpcBrowserService
    {
        Task<OpcBrowseResultDTO> BrowseNodesAsync(Guid serverId, string? parentNodeId = null);
    }
}
