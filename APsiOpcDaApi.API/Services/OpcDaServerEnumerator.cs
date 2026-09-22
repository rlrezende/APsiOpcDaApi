using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using Opc;
using OpcCom;

namespace APsiOpcDaApi.API.Services
{
    public sealed class OpcDaServerEnumerator : IOpcDaServerEnumerator
    {
        internal readonly record struct UrlMetadata(
            string? Scheme,
            string? HostName,
            string? Path);

        public IEnumerable<OpcDaDiscoveredServerDTO> Enumerate(string host)
        {
            using var enumerator = new ServerEnumerator();
            var servers = enumerator.GetAvailableServers(Specification.COM_DA_20, host, null)
                ?? Array.Empty<Opc.Server>();

            foreach (var opcServer in servers.OfType<Opc.Da.Server>())
            {
                using var serverInstance = opcServer;
                var url = serverInstance.Url;
                var metadata = url == null
                    ? (UrlMetadata?)null
                    : new UrlMetadata(url.Scheme, url.HostName, url.Path);

                yield return MapServerMetadata(serverInstance.Name, metadata);
            }
        }

        internal static OpcDaDiscoveredServerDTO MapServerMetadata(
            string? name,
            UrlMetadata? url)
        {
            var endpoint = url.HasValue
                ? $"{url.Value.Scheme}://{url.Value.HostName}/{url.Value.Path}".TrimEnd('/')
                : string.Empty;
            var progId = ExtractProgId(url?.Path);

            return new OpcDaDiscoveredServerDTO
            {
                Nome = name ?? progId ?? "Servidor OPC DA",
                Endpoint = endpoint,
                ProgId = progId ?? endpoint,
                ClsId = ExtractClsId(url?.Path),
                Descricao = name
            };
        }

        private static string? ExtractProgId(string? path)
        {
            if (path == null)
            {
                return null;
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return null;
            }

            var first = segments[0];
            if (first.StartsWith("{") && first.EndsWith("}", StringComparison.Ordinal))
            {
                return null;
            }

            return first;
        }

        private static string? ExtractClsId(string? path)
        {
            if (path == null)
            {
                return null;
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.LastOrDefault();
            if (string.IsNullOrWhiteSpace(last))
            {
                return null;
            }

            if (last.StartsWith("{") && last.EndsWith("}", StringComparison.Ordinal))
            {
                return last.Trim('{', '}');
            }

            return null;
        }
    }
}
