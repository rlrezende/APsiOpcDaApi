using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcDaServerEnumerator
    {
        IEnumerable<OpcDaDiscoveredServerDTO> Enumerate(string host);
    }
}
