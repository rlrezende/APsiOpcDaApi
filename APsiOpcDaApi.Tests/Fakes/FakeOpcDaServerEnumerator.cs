using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;

namespace APsiOpcDaApi.Tests.Fakes;

internal sealed class FakeOpcDaServerEnumerator : IOpcDaServerEnumerator
{
    public IReadOnlyList<OpcDaDiscoveredServerDTO> Servers { get; set; } = [];
    public Exception? Exception { get; set; }
    public List<string> RequestedHosts { get; } = [];

    public IEnumerable<OpcDaDiscoveredServerDTO> Enumerate(string host)
    {
        RequestedHosts.Add(host);

        if (Exception is not null)
        {
            throw Exception;
        }

        return Servers;
    }
}
