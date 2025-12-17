#if SOFTING_OPC
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Application.Opc;
using APsiControleApi.Domain.Enum;
using Microsoft.Extensions.Logging;
using Softing.OPCToolbox;
using Softing.OPCToolbox.Client;

namespace APsiControleApi.Application.Services
{
    public class OpcDaClientService : IOpcDaClientService
    {
        private static readonly int[] DefaultPropertyIds =
        {
            (int)EnumPropertyId.ITEM_DESCRIPTION,
            (int)EnumPropertyId.ITEM_CANONICAL_DATATYPE,
            (int)EnumPropertyId.ITEM_ACCESS_RIGHTS,
            (int)EnumPropertyId.EU_UNITS,
            (int)EnumPropertyId.HIGH_EU,
            (int)EnumPropertyId.LOW_EU
        };

        private readonly ILogger<OpcDaClientService> _logger;

        public OpcDaClientService(ILogger<OpcDaClientService> logger)
        {
            _logger = logger;
            SoftingOpcDaBootstrapper.Initialize(_logger);
        }

        public bool IsSupported => OperatingSystem.IsWindows();

        public async Task<OpcBrowseResultDTO> BrowseAsync(OpcServerDTO server, string? itemId = null)
        {
            EnsureCanUseDa(server);
            return await Task.Run(() => RunOnSta(() => BrowseInternal(server, itemId))).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<OpcTagDTO>> ReadValuesAsync(OpcServerDTO server, IEnumerable<string> itemIds)
        {
            EnsureCanUseDa(server);

            var ids = itemIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            if (ids.Count == 0)
            {
                return Array.Empty<OpcTagDTO>();
            }

            return await Task.Run(() => RunOnSta(() => ReadInternal(server, ids))).ConfigureAwait(false);
        }

        private OpcBrowseResultDTO BrowseInternal(OpcServerDTO server, string? itemId)
        {
            using var scope = CreateConnection(server);

            var nodes = new List<OpcNodeBrowseDTO>();
            var tags = new List<OpcTagDTO>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            BrowseRecursive(scope.Session, itemId, nodes, tags, visited);

            return new OpcBrowseResultDTO
            {
                Nodes = nodes,
                Tags = tags
            };
        }

        private void BrowseRecursive(
            DaSession session,
            string? itemId,
            List<OpcNodeBrowseDTO> nodes,
            List<OpcTagDTO> tags,
            HashSet<string> visited)
        {
            var options = new DaAddressSpaceElementBrowseOptions
            {
                ElementTypeFilter = EnumAddressSpaceElementType.ALL,
                RetrieveItemId = true,
                RetrieveProperties = false,
                ReturnProperties = false,
                ReturnPropertyValues = false
            };

            var exec = CreateSyncExecution();
            var browseId = itemId ?? string.Empty;

            DaAddressSpaceElement[]? elements = null;
            try
            {
                var result = session.Browse(browseId, string.Empty, options, out elements, exec);
                if (!ResultCode.SUCCEEDED(result))
                {
                    _logger.LogWarning("Falha ao navegar no OPC DA (Softing). ItemId={ItemId}, Resultado=0x{Result:X8}", browseId, result);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exceção ao navegar no OPC DA (Softing). ItemId={ItemId}", browseId);
                return;
            }

            if (elements == null)
            {
                return;
            }

            foreach (var element in elements)
            {
                var identifier = element.ItemId ?? element.Name;
                if (string.IsNullOrWhiteSpace(identifier) || !visited.Add(identifier))
                {
                    continue;
                }

                var displayName = !string.IsNullOrWhiteSpace(element.Name) ? element.Name : identifier;

                if (element.IsBranch)
                {
                    nodes.Add(new OpcNodeBrowseDTO
                    {
                        NodeId = identifier,
                        DisplayName = displayName,
                        BrowseName = identifier,
                        NodeClass = "Object",
                        HasChildren = true,
                        Description = null,
                        Icon = "folder"
                    });

                    BrowseRecursive(session, identifier, nodes, tags, visited);
                }

                if (element.IsLeaf || element.IsItem)
                {
                    var tag = CreateTagFromElement(session, element, identifier, displayName);
                    tags.Add(tag);
                }
            }
        }

        private OpcTagDTO CreateTagFromElement(
            DaSession session,
            DaAddressSpaceElement element,
            string identifier,
            string displayName)
        {
            DaProperty[]? properties = null;
            try
            {
                var options = new DaGetPropertiesOptions
                {
                    PropertyIds = DefaultPropertyIds,
                    WhatPropertyData = EnumPropertyData.ALL
                };

                var result = element.GetDaProperties(options, out properties, CreateSyncExecution());
                if (!ResultCode.SUCCEEDED(result))
                {
                    _logger.LogDebug("Sem propriedades para {NodeId}. Resultado=0x{Result:X8}", identifier, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao obter propriedades da tag {NodeId}", identifier);
            }

            var tag = new OpcTagDTO
            {
                NodeId = identifier,
                DisplayName = displayName,
                BrowseName = identifier,
                NodeClass = "Variable",
                HasChildren = false,
                Icon = "tag",
                DataType = string.Empty,
                AccessLevel = "Unknown"
            };

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    switch ((EnumPropertyId)property.Id)
                    {
                        case EnumPropertyId.ITEM_DESCRIPTION:
                            tag.Description = property.Value?.Data?.ToString();
                            break;
                        case EnumPropertyId.ITEM_CANONICAL_DATATYPE:
                            tag.DataType = ResolveDataType(property);
                            break;
                        case EnumPropertyId.ITEM_ACCESS_RIGHTS:
                            tag.AccessLevel = ResolveAccessRights(property);
                            break;
                        case EnumPropertyId.EU_UNITS:
                            tag.Unit = property.Value?.Data?.ToString();
                            break;
                        case EnumPropertyId.HIGH_EU:
                            tag.MaxValue = ConvertToDouble(property.Value?.Data);
                            break;
                        case EnumPropertyId.LOW_EU:
                            tag.MinValue = ConvertToDouble(property.Value?.Data);
                            break;
                    }
                }
            }

            return tag;
        }

        private IReadOnlyList<OpcTagDTO> ReadInternal(OpcServerDTO server, List<string> itemIds)
        {
            using var scope = CreateConnection(server);

            var exec = CreateSyncExecution();
            var itemPaths = Enumerable.Repeat(string.Empty, itemIds.Count).ToArray();

            ValueQT[] values;
            int[] results;

            var readResult = scope.Session.Read(
                maxAge: 0,
                itemIDs: itemIds.ToArray(),
                itemPaths: itemPaths,
                values: out values,
                results: out results,
                executionOptions: exec);

            if (!ResultCode.SUCCEEDED(readResult))
            {
                throw new InvalidOperationException($"Falha ao ler valores via Softing OPC Toolkit. Resultado: 0x{readResult:X8}");
            }

            var tags = new List<OpcTagDTO>(itemIds.Count);

            for (var i = 0; i < itemIds.Count; i++)
            {
                var tag = new OpcTagDTO
                {
                    NodeId = itemIds[i],
                    DisplayName = itemIds[i],
                    BrowseName = itemIds[i],
                    NodeClass = "Variable",
                    HasChildren = false,
                    Icon = "tag"
                };

                if (values != null && i < values.Length && ResultCode.SUCCEEDED(results[i]))
                {
                    tag.ValorAtual = values[i].Data?.ToString();
                    tag.DataType = values[i].Data?.GetType().FullName ?? string.Empty;
                    tag.Quality = values[i].Quality.ToString();
                    tag.Timestamp = values[i].TimeStamp;
                }
                else
                {
                    tag.ValorAtual = "Erro";
                    tag.DataType = string.Empty;
                    tag.Quality = $"Falha (0x{results[i]:X8})";
                    tag.Timestamp = null;
                }

                tags.Add(tag);
            }

            return tags;
        }

        private DaSessionScope CreateConnection(OpcServerDTO server)
        {
            var host = ResolveHost(server);
            var progId = ResolveProgId(server);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(progId))
            {
                throw new InvalidOperationException(
                    $"Host e ProgId são obrigatórios para servidores OPC DA. Host='{host}', ProgId='{progId}'.");
            }

            var url = BuildServerUrl(host, progId, server.ClsId);

            _logger.LogInformation("Conectando ao OPC DA (Softing). Host={Host}, ProgId={ProgId}, Url={Url}", host, progId, url);

            var session = new DaSession(url);
            var exec = CreateSyncExecution();

            var connectResult = session.Connect(
                connectDeep: true,
                activateObjects: true,
                executionOptions: exec);

            if (!ResultCode.SUCCEEDED(connectResult))
            {
                throw new InvalidOperationException($"Erro ao conectar ao OPC DA (Softing). Resultado: 0x{connectResult:X8}");
            }

            if (!string.IsNullOrWhiteSpace(server.Username))
            {
                var logonResult = session.Logon(server.Username, server.Password ?? string.Empty, exec);
                if (!ResultCode.SUCCEEDED(logonResult))
                {
                    throw new InvalidOperationException($"Falha ao autenticar no OPC DA (Softing). Resultado: 0x{logonResult:X8}");
                }
            }

            _logger.LogInformation("Conexão Softing OPC DA concluída. Host={Host}, ProgId={ProgId}", host, progId);
            return new DaSessionScope(session);
        }

        private static ExecutionOptions CreateSyncExecution() => new()
        {
            ExecutionType = EnumExecutionType.SYNCHRONOUS,
            ExecutionContext = 0
        };

        private static string BuildServerUrl(string host, string progId, string? clsId)
        {
            var builder = new StringBuilder();
            builder.Append("opcda://");
            builder.Append(string.IsNullOrWhiteSpace(host) ? string.Empty : host.Trim());
            builder.Append('/');
            builder.Append(progId.Trim());

            if (!string.IsNullOrWhiteSpace(clsId))
            {
                var formatted = clsId.Trim();
                if (!formatted.StartsWith("{", StringComparison.Ordinal))
                {
                    formatted = "{" + formatted;
                }
                if (!formatted.EndsWith("}", StringComparison.Ordinal))
                {
                    formatted += "}";
                }

                builder.Append('/');
                builder.Append(formatted);
            }

            return builder.ToString();
        }

        private static string ResolveAccessRights(DaProperty property)
        {
            if (property.Value?.Data == null)
            {
                return "Unknown";
            }

            if (property.Value.Data is EnumAccessRights accessRights)
            {
                return accessRights.ToString();
            }

            if (property.Value.Data is int raw)
            {
                return ((EnumAccessRights)raw).ToString();
            }

            return property.Value.Data.ToString() ?? "Unknown";
        }

        private static string ResolveDataType(DaProperty property)
        {
            if (property.DataType != null)
            {
                return property.DataType.FullName ?? property.DataType.Name;
            }

            if (property.Value?.Data is Type runtimeType)
            {
                return runtimeType.FullName ?? runtimeType.Name;
            }

            return property.Value?.Data?.GetType().FullName ?? string.Empty;
        }

        private static double? ConvertToDouble(object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static T RunOnSta<T>(Func<T> func)
        {
            if (!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return func();
            }

            T result = default!;
            ExceptionDispatchInfo? capturedException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }
            })
            {
                IsBackground = true
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            capturedException?.Throw();
            return result;
        }

        private void EnsureCanUseDa(OpcServerDTO server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (server.Tipo != TipoOpcServer.Da)
            {
                throw new InvalidOperationException("Este serviço só deve ser usado para servidores OPC DA.");
            }

            if (!IsSupported)
            {
                throw new PlatformNotSupportedException("OPC DA só é suportado em ambientes Windows.");
            }
        }

        private static string? ResolveHost(OpcServerDTO server)
        {
            if (!string.IsNullOrWhiteSpace(server.Host))
            {
                return server.Host;
            }

            if (!string.IsNullOrWhiteSpace(server.Endpoint) &&
                Uri.TryCreate(server.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                return endpointUri.Host;
            }

            return server.Endpoint;
        }

        private static string? ResolveProgId(OpcServerDTO server)
        {
            if (!string.IsNullOrWhiteSpace(server.ProgId))
            {
                return server.ProgId;
            }

            if (!string.IsNullOrWhiteSpace(server.Endpoint) &&
                Uri.TryCreate(server.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                var segments = endpointUri.AbsolutePath
                    .Trim('/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length > 0)
                {
                    return string.Join("/", segments);
                }
            }

            return string.IsNullOrWhiteSpace(server.Endpoint) ? null : server.Endpoint;
        }

        private sealed class DaSessionScope : IDisposable
        {
            private readonly DaSession _session;
            private bool _disposed;

            public DaSessionScope(DaSession session)
            {
                _session = session;
            }

            public DaSession Session => _session;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    _session.Disconnect(CreateSyncExecution());
                }
                catch
                {
                    // best-effort
                }
                finally
                {
                    try
                    {
                        Application.Instance.RemoveDaSession(_session);
                    }
                    catch
                    {
                        // ignore
                    }

                    _session.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
#endif
