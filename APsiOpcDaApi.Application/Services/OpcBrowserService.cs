using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Enum;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace APsiOpcDaApi.Application.Services
{
    public class OpcBrowserService : IOpcBrowserService
    {
        private readonly IOpcServerService _opcServerService;
        private readonly IOpcDaClientService _opcDaClientService;

        public OpcBrowserService(IOpcServerService opcServerService, IOpcDaClientService opcDaClientService)
        {
            _opcServerService = opcServerService;
            _opcDaClientService = opcDaClientService;
        }


        public async Task<OpcBrowseResultDTO> BrowseNodesAsync(Guid serverId, string? parentNodeId = null)
        {
            var server = await _opcServerService.GetByIdAsync(serverId);
            if (server == null)
                throw new InvalidOperationException("OPC Server não encontrado.");

            if (server.Tipo == TipoOpcServer.Da)
            {
                return await _opcDaClientService.BrowseAsync(server, parentNodeId);
            }

            if (string.IsNullOrWhiteSpace(server.Endpoint))
                throw new InvalidOperationException("OPC Server não encontrado ou endpoint não configurado.");

            var endpointUrl = server.Endpoint;

            var config = CreateApplicationConfiguration();
            var hasCredentials = !string.IsNullOrWhiteSpace(server.Username);
            var selectedEndpoint = SelectEndpointForIdentity(config, endpointUrl, hasCredentials);
            var endpointConfig = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfig);
            IUserIdentity identity = hasCredentials
                ? new UserIdentity(server.Username, server.Password ?? string.Empty)
                : new UserIdentity(new AnonymousIdentityToken());

            using var session = await Session.Create(config, endpoint, false, "OPC Browser", 60000, identity, null);

            string nodeIdStr = string.IsNullOrWhiteSpace(parentNodeId) ? "ns=0;i=85" : parentNodeId;
            var rootNodeId = NodeId.Parse(nodeIdStr);

            var nodes = new List<OpcNodeBrowseDTO>();
            var tags = new List<OpcTagDTO>();

            await BrowseRecursively(session, rootNodeId, nodes, tags, new HashSet<string>());

            return new OpcBrowseResultDTO
            {
                Nodes = nodes,
                Tags = tags
            };

            
        }

        private static EndpointDescription SelectEndpointForIdentity(
            ApplicationConfiguration config,
            string endpointUrl,
            bool useUsername)
        {
            var requiredTokenType = useUsername ? UserTokenType.UserName : UserTokenType.Anonymous;
            using var discoveryClient = DiscoveryClient.Create(new Uri(endpointUrl));
            discoveryClient.OperationTimeout = 5000;
            var endpoints = discoveryClient.GetEndpoints(null);

            var selected = endpoints
                .Where(endpoint => endpoint.UserIdentityTokens?.Any(policy => policy.TokenType == requiredTokenType) == true)
                .OrderBy(endpoint => endpoint.SecurityMode == MessageSecurityMode.None ? 0 : 1)
                .ThenBy(endpoint => endpoint.SecurityLevel)
                .FirstOrDefault();

            if (selected != null)
            {
                return selected;
            }

            var identityName = useUsername ? "usuário/senha" : "anônima";
            throw new InvalidOperationException(
                $"O servidor OPC UA não oferece autenticação {identityName}. " +
                (useUsername
                    ? "Verifique as credenciais configuradas."
                    : "Configure usuário e senha para este servidor."));
        }

        private async Task BrowseRecursively(
            Session session,
            NodeId nodeId,
            List<OpcNodeBrowseDTO> nodes,
            List<OpcTagDTO> tags,
            HashSet<string> visitedNodes)
        {
            string nodeIdStr = nodeId.ToString();

            if (!visitedNodes.Add(nodeIdStr)) return;

            var browser = new Browser(session)
            {
                BrowseDirection = BrowseDirection.Forward,
                NodeClassMask = (int)(NodeClass.Object | NodeClass.Variable),
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true
            };

            var results = browser.Browse(nodeId);

            foreach (var result in results)
            {
                try
                {
                    var realNodeId = ExpandedNodeId.ToNodeId(result.NodeId, session.NamespaceUris);
                    var realNodeIdStr = realNodeId.ToString();

                    if (visitedNodes.Contains(realNodeIdStr))
                        continue;

                    if (result.NodeClass == NodeClass.Object)
                    {
                        visitedNodes.Add(realNodeIdStr); // marca como visitado logo

                        nodes.Add(new OpcNodeBrowseDTO
                        {
                            NodeId = realNodeIdStr,
                            DisplayName = result.DisplayName.Text,
                            BrowseName = result.BrowseName.ToString(),
                            NodeClass = "Object",
                            HasChildren = CheckHasChildren(session, realNodeId),
                            Icon = GetObjectIcon(result.DisplayName.Text),
                            Description = ReadNodeDescription(session, realNodeId)
                        });

                        await BrowseRecursively(session, realNodeId, nodes, tags, visitedNodes);
                    }
                    else if (result.NodeClass == NodeClass.Variable)
                    {
                        visitedNodes.Add(realNodeIdStr); // marca como visitado ANTES da leitura

                        var tag = await ReadVariableCompleteInfo(session, realNodeId, result);
                        tag.Description ??= ReadNodeDescription(session, realNodeId);
                        tags.Add(tag);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar nó {result.NodeId}: {ex.Message}");
                    continue;
                }
            }
        }





        private async Task<OpcTagDTO> ReadVariableCompleteInfo(Session session, NodeId nodeId, ReferenceDescription result)
        {
            var tag = new OpcTagDTO
            {
                NodeId = nodeId.ToString(),
                DisplayName = result.DisplayName.Text,
                BrowseName = result.BrowseName.ToString(),
                NodeClass = "Variable"
            };

            try
            {
                var attributesToRead = new ReadValueIdCollection
                {
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.DataType },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.AccessLevel },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.Description },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.MinimumSamplingInterval },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.UserAccessLevel }
                };

                session.Read(null, 0, TimestampsToReturn.Both, attributesToRead,
                    out var results, out var diagnosticInfos);

                if (results.Count >= 1 && StatusCode.IsGood(results[0].StatusCode))
                {
                    var valueAttr = results[0];
                    tag.ValorAtual = valueAttr.Value?.ToString() ?? "null";
                    tag.Quality = GetQualityString(valueAttr.StatusCode);
                    tag.Timestamp = valueAttr.SourceTimestamp != DateTime.MinValue ? valueAttr.SourceTimestamp : valueAttr.ServerTimestamp;
                }
                else
                {
                    tag.ValorAtual = "Erro na leitura";
                    tag.Quality = "Bad";
                }

                if (results.Count >= 2 && StatusCode.IsGood(results[1].StatusCode))
                {
                    var dataTypeId = results[1].Value as NodeId;
                    tag.DataType = GetDataTypeString(session, dataTypeId);
                }

                if (results.Count >= 3 && StatusCode.IsGood(results[2].StatusCode))
                {
                    tag.AccessLevel = GetAccessLevelString(Convert.ToByte(results[2].Value));
                }

                if (results.Count >= 4 && StatusCode.IsGood(results[3].StatusCode))
                {
                    if (results[3].Value is LocalizedText desc)
                        tag.Description = desc.Text;
                }

                tag.Icon = GetVariableIcon(tag.DataType, tag.DisplayName);
                tag.HasChildren = CheckHasChildren(session, nodeId);

                TryReadNumericLimits(session, nodeId, tag);
                TryReadEngineeringUnits(session, nodeId, tag);
            }
            catch (Exception ex)
            {
                tag.ValorAtual = "Erro na leitura";
                tag.Quality = "Bad";
                Console.WriteLine($"Erro ao ler variável {nodeId}: {ex.Message}");
            }

            return tag;
        }


        private bool CheckHasChildren(Session session, NodeId nodeId)
        {
            try
            {
                var browser = new Browser(session)
                {
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true
                };

                var children = browser.Browse(nodeId);
                return children.Any();
            }
            catch
            {
                return false;
            }
        }

        private string? ReadNodeDescription(Session session, NodeId nodeId)
        {
            try
            {
                var readValueId = new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Description
                };

                var nodesToRead = new ReadValueIdCollection { readValueId };
                session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out var results, out var diagnosticInfos);

                if (results.Count > 0 && StatusCode.IsGood(results[0].StatusCode) && results[0].Value is LocalizedText desc)
                {
                    return desc.Text;
                }
            }
            catch
            {
                // Ignorar erros
            }
            return null;
        }

        private void TryReadNumericLimits(Session session, NodeId nodeId, OpcTagDTO tag)
        {
            try
            {
                // Tentar ler propriedades de limite
                var euRangeNodeId = new NodeId($"{nodeId}.EURange");
                
                var readValueId = new ReadValueId
                {
                    NodeId = euRangeNodeId,
                    AttributeId = Attributes.Value
                };

                var nodesToRead = new ReadValueIdCollection { readValueId };
                session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out var results, out var diagnosticInfos);
                
                if (results.Count > 0 && StatusCode.IsGood(results[0].StatusCode) && results[0].Value is ExtensionObject ext)
                {
                    if (ext.Body is Opc.Ua.Range range)
                    {
                        tag.MinValue = range.Low;
                        tag.MaxValue = range.High;
                    }
                }
            }
            catch
            {
                // Limites não disponíveis
            }
        }

     private void TryReadEngineeringUnits(Session session, NodeId nodeId, OpcTagDTO tag)
        {
            try
            {
                // Monta o objeto BrowseDescription
                var browseDesc = new BrowseDescription
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HasProperty,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)NodeClass.Variable,
                    ResultMask = (uint)BrowseResultMask.All
                };

                // Prepara a coleção de BrowseDescriptions
                var nodesToBrowse = new BrowseDescriptionCollection { browseDesc };

                // Faz a chamada Browse
                session.Browse(
                    null,                   // RequestHeader
                    null,                   // ViewDescription
                    0,                      // MaxResultsToReturn
                    nodesToBrowse,
                    out var results,
                    out var diagnosticInfos);

                ClientBase.ValidateResponse(results, nodesToBrowse);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                // Verifica se encontrou a propriedade EngineeringUnits
                var euProperty = results[0].References?
                    .FirstOrDefault(r => r.BrowseName.Name == "EngineeringUnits");

                if (euProperty != null)
                {
                    var euNodeId = ExpandedNodeId.ToNodeId(euProperty.NodeId, session.NamespaceUris);

                    var nodesToRead = new ReadValueIdCollection
                    {
                        new ReadValueId
                        {
                            NodeId = euNodeId,
                            AttributeId = Attributes.Value
                        }
                    };

                    session.Read(
                        null,                  // RequestHeader
                        0,                     // MaxAge
                        TimestampsToReturn.Neither,
                        nodesToRead,
                        out var readResults,
                        out var readDiagnosticInfos);

                    ClientBase.ValidateResponse(readResults, nodesToRead);
                    ClientBase.ValidateDiagnosticInfos(readDiagnosticInfos, nodesToRead);

                    if (readResults.Count > 0 &&
                        StatusCode.IsGood(readResults[0].StatusCode) &&
                        readResults[0].Value is ExtensionObject ext &&
                        ext.Body is EUInformation euInfo)
                    {
                        tag.Unit = euInfo.DisplayName?.Text ?? "";
                    }
                }

                // Fallback se não encontrar unidade
                if (string.IsNullOrEmpty(tag.Unit))
                {
                    TryExtractUnitFromDescription(tag);

                    if (string.IsNullOrEmpty(tag.Unit))
                    {
                        tag.Unit = InferUnitFromName(tag.DisplayName, tag.DataType);
                    }
                }
            }
            catch (Exception ex)
            {
                tag.Unit = InferUnitFromName(tag.DisplayName, tag.DataType);
                Console.WriteLine($"Erro ao ler EngineeringUnits de {nodeId}: {ex.Message}");
            }
        }



        private void TryExtractUnitFromDescription(OpcTagDTO tag)
        {
            if (string.IsNullOrEmpty(tag.Description)) return;
    
            var description = tag.Description.ToLower();
    
            // Procurar por padrões comuns de unidades na descrição
            var unitPatterns = new Dictionary<string, string>
            {
                { "celsius", "°C" },
                { "fahrenheit", "°F" },
                { "kelvin", "K" },
                { "meter", "m" },
                { "centimeter", "cm" },
                { "millimeter", "mm" },
                { "kilometer", "km" },
                { "bar", "bar" },
                { "pascal", "Pa" },
                { "psi", "psi" },
                { "volt", "V" },
                { "ampere", "A" },
                { "watt", "W" },
                { "hertz", "Hz" },
                { "rpm", "rpm" },
                { "percent", "%" },
                { "liter", "L" },
                { "m3/h", "m³/h" },
                { "kg/h", "kg/h" }
            };
    
            foreach (var pattern in unitPatterns)
            {
                if (description.Contains(pattern.Key))
                {
                    tag.Unit = pattern.Value;
                    break;
                }
            }
        }

        private string InferUnitFromName(string displayName, string dataType)
        {
            var name = displayName.ToLower();
    
            // Inferir unidades baseado no nome da variável
            if (name.Contains("temperature") || name.Contains("temp")) return "°C";
            if (name.Contains("pressure") || name.Contains("press")) return "bar";
            if (name.Contains("flow") || name.Contains("vazao")) return "m³/h";
            if (name.Contains("level") || name.Contains("nivel")) return "m";
            if (name.Contains("voltage") || name.Contains("tensao")) return "V";
            if (name.Contains("current") || name.Contains("corrente")) return "A";
            if (name.Contains("power") || name.Contains("potencia")) return "W";
            if (name.Contains("frequency") || name.Contains("frequencia")) return "Hz";
            if (name.Contains("speed") || name.Contains("velocidade")) return "rpm";
            if (name.Contains("percent") || name.Contains("porcentagem")) return "%";
            if (name.Contains("weight") || name.Contains("peso")) return "kg";
            if (name.Contains("volume") || name.Contains("volume")) return "L";
    
            // Para tipos booleanos, não há unidade
            if (dataType.ToLower().Contains("boolean")) return "";
    
            // Para strings, não há unidade
            if (dataType.ToLower().Contains("string")) return "";
    
            // Padrão para valores numéricos sem unidade identificada
            return "";
        }

        private string GetQualityString(StatusCode statusCode)
        {
            if (StatusCode.IsGood(statusCode)) return "Good";
            if (StatusCode.IsUncertain(statusCode)) return "Uncertain";
            return "Bad";
        }

        private string GetDataTypeString(Session session, NodeId? dataTypeId)
        {
            if (dataTypeId == null) return "Unknown";

            try
            {
                // Ler o DisplayName do tipo de dados usando Read
                var readValueId = new ReadValueId
                {
                    NodeId = dataTypeId,
                    AttributeId = Attributes.DisplayName
                };

                var nodesToRead = new ReadValueIdCollection { readValueId };
                session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out var results, out var diagnosticInfos);

                if (results.Count > 0 && StatusCode.IsGood(results[0].StatusCode) && results[0].Value is LocalizedText displayName)
                {
                    return displayName.Text ?? "Unknown";
                }
            }
            catch
            {
                // Mapear tipos conhecidos
                if (dataTypeId.Equals(DataTypeIds.Double)) return "Double";
                if (dataTypeId.Equals(DataTypeIds.Float)) return "Float";
                if (dataTypeId.Equals(DataTypeIds.Int32)) return "Int32";
                if (dataTypeId.Equals(DataTypeIds.Boolean)) return "Boolean";
                if (dataTypeId.Equals(DataTypeIds.String)) return "String";
                if (dataTypeId.Equals(DataTypeIds.DateTime)) return "DateTime";
            }
            
            return "Unknown";
        }

        private string GetAccessLevelString(byte accessLevel)
        {
            var access = new List<string>();
            
            if ((accessLevel & AccessLevels.CurrentRead) != 0) access.Add("Read");
            if ((accessLevel & AccessLevels.CurrentWrite) != 0) access.Add("Write");
            if ((accessLevel & AccessLevels.HistoryRead) != 0) access.Add("HistoryRead");
            if ((accessLevel & AccessLevels.HistoryWrite) != 0) access.Add("HistoryWrite");

            return access.Any() ? string.Join(", ", access) : "None";
        }

        private string GetObjectIcon(string displayName)
        {
            var name = displayName.ToLower();
            if (name.Contains("device") || name.Contains("equipment")) return "device";
            if (name.Contains("folder") || name.Contains("group")) return "folder";
            if (name.Contains("server") || name.Contains("system")) return "server";
            return "folder";
        }

        private string GetVariableIcon(string dataType, string displayName)
        {
            var name = displayName.ToLower();
            var type = dataType.ToLower();

            if (name.Contains("temperature") || name.Contains("temp")) return "thermometer";
            if (name.Contains("pressure") || name.Contains("press")) return "gauge";
            if (name.Contains("flow") || name.Contains("rate")) return "activity";
            if (name.Contains("level") || name.Contains("height")) return "bar-chart";
            if (name.Contains("motor") || name.Contains("pump")) return "zap";
            if (name.Contains("valve") || name.Contains("actuator")) return "settings";
            if (type.Contains("boolean") || name.Contains("status")) return "toggle";
            if (type.Contains("string") || type.Contains("text")) return "type";
            
            return "tag";
        }

        private ApplicationConfiguration CreateApplicationConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = "OPCMonitor",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "pki/own",
                        SubjectName = "CN=OPCMonitor"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "pki/trusted"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "pki/issuers"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "pki/rejected"
                    },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                DisableHiResClock = true
            };

            // Criar diretórios necessários
            Directory.CreateDirectory("pki/own");
            Directory.CreateDirectory("pki/trusted");
            Directory.CreateDirectory("pki/issuers");
            Directory.CreateDirectory("pki/rejected");

            return config;
        }
    }
}

