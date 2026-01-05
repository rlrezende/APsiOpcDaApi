using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcDaClientService
    {
        bool IsSupported { get; }

        Task<OpcBrowseResultDTO> BrowseAsync(OpcServerDTO server, string? itemId = null);

        Task<IReadOnlyList<OpcTagDTO>> ReadValuesAsync(OpcServerDTO server, IEnumerable<string> itemIds);
    }
}

