using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Interfaces
{
    public interface IOpcDaClientService
    {
        bool IsSupported { get; }

        Task<OpcBrowseResultDTO> BrowseAsync(OpcServerDTO server, string? itemId = null);

        Task<IReadOnlyList<OpcTagDTO>> ReadValuesAsync(OpcServerDTO server, IEnumerable<string> itemIds);
    }
}
