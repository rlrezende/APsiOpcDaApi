using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Collections.Concurrent;
using APsiOpcDaApi.Domain.Enum;
using System.Globalization;

namespace APsiOpcDaApi.Application.Services
{
    public class OpcMonitoringService : IOpcMonitoringService, IDisposable
    {
        private readonly IOpcGroupService _groupService;
        private readonly IOpcServerService _opcServerService;
        private readonly ILeituraService _leituraService;
        private readonly INotificadorSimulacao _notificador;
        private readonly ILogger<OpcMonitoringService> _logger;
        private readonly IOpcNodeService _nodeService;
        private readonly ITagService _tagService;
        private readonly IOpcDaClientService _opcDaClientService;

        // Gerenciamento de sessões e subscriptions
        private readonly ConcurrentDictionary<Guid, Session> _activeSessions = new();
        private readonly ConcurrentDictionary<Guid, Subscription> _activeSubscriptions = new();
        private readonly ConcurrentDictionary<Guid, List<MonitoredItem>> _monitoredItems = new();
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _daGroupCancellation = new();
        private readonly ConcurrentDictionary<Guid, Task> _daPollingTasks = new();
        private readonly ConcurrentDictionary<Guid, HashSet<string>> _daGroupItems = new();

        private readonly IServiceScopeFactory _scopeFactory;

        
        private ApplicationConfiguration? _applicationConfiguration;

        private readonly HashSet<Guid> _managedSubscriptionGroupIds = new();

        private bool _disposed = false;

        private bool _initialized = false;


        public OpcMonitoringService(
            IOpcGroupService groupService,
            IOpcServerService opcServerService,
            ILeituraService leituraService,
            INotificadorSimulacao notificador,
            IOpcNodeService nodeService,
            ITagService tagService,
            IOpcDaClientService opcDaClientService,
            ILogger<OpcMonitoringService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _groupService = groupService;
            _opcServerService = opcServerService;
            _leituraService = leituraService;
            _notificador = notificador;
            _nodeService = nodeService;
            _tagService = tagService;
            _opcDaClientService = opcDaClientService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        
        private async Task CleanupStaleGroupsAndTagsAsync(CancellationToken cancellationToken)
        {
            var allGroups = await _groupService.GetActiveGroupsAsync(); // aqui pode ser GetAllGroupsAsync, dependendo da sua implementação
            var activeGroups = allGroups.Where(g => g.IsActive).ToList();
            var activeGroupIds = activeGroups.Select(g => g.Id).ToHashSet();

            // 1. Remover subscriptions de grupos desativados ou inexistentes
            foreach (var groupId in _activeSubscriptions.Keys.ToList())
            {
                // Grupo não está mais ativo (foi excluído ou desativado)
                if (!activeGroupIds.Contains(groupId))
                {
                    if (_activeSubscriptions.TryRemove(groupId, out var subscription))
                    {
                        _logger.LogInformation($"Removendo subscription para grupo inativo/desativado: {subscription.DisplayName}");
                        subscription.Delete(true);
                        subscription.Dispose();
                    }

                    _monitoredItems.TryRemove(groupId, out _); // Limpa também os monitored items
                    StopOpcDaGroup(groupId);
                }
            }

            // 2. Verificar monitored items de grupos ainda ativos
            foreach (var group in activeGroups)
            {
                var subscription = _activeSubscriptions.GetValueOrDefault(group.Id);
                if (subscription == null)
                    continue;

                if (_monitoredItems.TryGetValue(group.Id, out var items))
                {
                    var tags = await _groupService.GetGroupTagsAsync(group.Id);

                    var validNodeIds = tags
                        .Where(t => t.Monitora && !string.IsNullOrEmpty(t.NodeIdOpc))
                        .Select(t => t.NodeIdOpc)
                        .ToHashSet();

                    var itemsToRemove = items
                        .Where(i => !validNodeIds.Contains(i.StartNodeId.ToString()))
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        _logger.LogInformation($"Removendo monitored item '{item.DisplayName}' do grupo '{group.Name}' (tag desmarcada)");
                        subscription.RemoveItem(item);
                        items.Remove(item);
                    }

                    await Task.Run(() => subscription.ApplyChanges(), cancellationToken);
                }
            }
        }



        public async Task MonitorarTagsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeOpcConfigurationAsync();

                if (!_initialized)
                {
                    await StopMonitoringAsync(); // limpa sujeira deixada por execuções anteriores
                    _initialized = true;
                }


                await CleanupStaleGroupsAndTagsAsync(cancellationToken);


                var activeGroups = await _groupService.GetActiveGroupsAsync();
                _logger.LogInformation($"Iniciando monitoramento de {activeGroups.Count} grupos ativos");

                // Processar cada grupo ativo
                foreach (var group in activeGroups)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    await ProcessGroupAsync(group, cancellationToken);
                }

                // Manter sessões ativas
                // await KeepSessionsAliveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante monitoramento de tags");
            }
        }

        private async Task InitializeOpcConfigurationAsync()
        {
            if (_applicationConfiguration != null) return;

            _applicationConfiguration = new ApplicationConfiguration
            {
                ApplicationName = "APsiControle OPC Monitor",
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:APsiControle:OpcMonitor",
                
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "pki/own",
                        SubjectName = "CN=APsiControle OPC Monitor"
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
                TransportQuotas = new TransportQuotas 
                { 
                    OperationTimeout = 30000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                },
                
                ClientConfiguration = new ClientConfiguration 
                { 
                    DefaultSessionTimeout = 60000,
                    WellKnownDiscoveryUrls = new StringCollection(),
                    MinSubscriptionLifetime = 10000
                },
                
                DisableHiResClock = true
            };

            // Criar diretórios de certificados
            Directory.CreateDirectory("pki/own");
            Directory.CreateDirectory("pki/trusted");
            Directory.CreateDirectory("pki/issuers");
            Directory.CreateDirectory("pki/rejected");

            await _applicationConfiguration.Validate(ApplicationType.Client);
            
            _applicationConfiguration.CertificateValidator = new CertificateValidator();
            await _applicationConfiguration.CertificateValidator.Update(_applicationConfiguration);
            _applicationConfiguration.CertificateValidator.CertificateValidation += (s, e) => e.Accept = true;
        }

        private async Task ProcessGroupAsync(OpcGroupDTO group, CancellationToken cancellationToken)
        {
            try
            {
                var server = await _opcServerService.GetByIdAsync(group.ServerId);
                if (server == null)
                {
                    _logger.LogWarning($"Servidor OPC para grupo {group.Name} não encontrado");
                    return;
                }

                if (server.Tipo == TipoOpcServer.Da)
                {
                    await EnsureOpcDaMonitoringAsync(server, group, cancellationToken);
                    return;
                }

                if (string.IsNullOrWhiteSpace(server.Endpoint))
                {
                    _logger.LogWarning($"Servidor OPC para grupo {group.Name} sem endpoint configurado");
                    return;
                }

                // Obter ou criar sessão para o servidor
                var session = await GetOrCreateSessionAsync(server, cancellationToken);
                if (session == null || !session.Connected)
                {
                    _logger.LogWarning($"Sessão OPC não conectada para servidor {server.Nome}, tentando recriar...");
                    
                    // Remove sessão antiga e tenta criar nova
                    _activeSessions.TryRemove(server.Id, out _);

                    session = await GetOrCreateSessionAsync(server, cancellationToken);
                    if (session == null || !session.Connected)
                    {
                        _logger.LogError($"Não foi possível estabelecer sessão OPC com o servidor {server.Nome}");
                        return;
                    }
                }

                if (!session.Connected)
                    throw new ServiceResultException(StatusCodes.BadNotConnected);


                // Obter ou criar subscription para o grupo
                var subscription = await GetOrCreateSubscriptionAsync(session, group);
                if (subscription == null) return;

                // Configurar monitored items para as tags do grupo
                await ConfigureMonitoredItemsAsync(subscription, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar grupo {group.Name}");
            }
        }

        private async Task<Session?> GetOrCreateSessionAsync(OpcServerDTO server, CancellationToken cancellationToken)
        {
            if (_activeSessions.TryGetValue(server.Id, out var existingSession) && 
                existingSession.Connected)
            {
                return existingSession;
            }

            try
            {
                var endpointDescription = CoreClientUtils.SelectEndpoint(_applicationConfiguration!, server.Endpoint, false);
                var endpointConfiguration = EndpointConfiguration.Create(_applicationConfiguration);
                var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                var session = await Session.Create(
                    _applicationConfiguration!,
                    endpoint,
                    false,
                    $"APsiControle-{server.Nome}",
                    60000,
                    null,
                    null);

                session.KeepAlive += OnSessionKeepAlive;
                session.Notification += OnSessionNotification;

                _activeSessions.AddOrUpdate(server.Id, session, (key, oldSession) =>
                {
                    oldSession?.Dispose();
                    return session;
                });

                _logger.LogInformation($"Sessão OPC criada para servidor {server.Nome}");
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao criar sessão OPC para servidor {server.Nome}");
                return null;
            }
        }

        private async Task<Subscription?> GetOrCreateSubscriptionAsync(Session session, OpcGroupDTO group)
        {
            if (_activeSubscriptions.TryGetValue(group.Id, out var existingSubscription))
            {
                // Atualiza propriedades da subscription existente
                existingSubscription.PublishingEnabled = group.IsActive;
                existingSubscription.PublishingInterval = group.UpdateRate;
                existingSubscription.KeepAliveCount = (uint)group.KeepAliveCount;
                existingSubscription.LifetimeCount = (uint)group.LifetimeCount;
                existingSubscription.MaxNotificationsPerPublish = (uint)group.MaxNotificationsPerPublish;
                existingSubscription.Priority = group.Priority;

                await Task.Run(() => existingSubscription.Modify()); // Aplica as alterações

                _logger.LogInformation($"Subscription atualizada para grupo {group.Name}");
                return existingSubscription;
            }

            try
            {
                var subscription = new Subscription(session.DefaultSubscription)
                {
                    DisplayName = $"Group_{group.Name}",
                    PublishingEnabled = group.IsActive,
                    PublishingInterval = group.UpdateRate,
                    KeepAliveCount = (uint)group.KeepAliveCount,
                    LifetimeCount = (uint)group.LifetimeCount,
                    MaxNotificationsPerPublish = (uint)group.MaxNotificationsPerPublish,
                    Priority = group.Priority
                };

                subscription.StateChanged += OnSubscriptionStateChanged;
                subscription.FastDataChangeCallback = OnFastDataChange;

                session.AddSubscription(subscription);
                await Task.Run(() => subscription.Create(), CancellationToken.None);

                _activeSubscriptions.TryAdd(group.Id, subscription);
                _managedSubscriptionGroupIds.Add(group.Id);
                _logger.LogInformation($"Subscription criada para grupo {group.Name}");

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao criar subscription para grupo {group.Name}");
                return null;
            }
        }


        private async Task ConfigureMonitoredItemsAsync(Subscription subscription, OpcGroupDTO group)
        {
            try
            {
                // Buscar todas as tags do grupo
                var groupTags = await _groupService.GetGroupTagsAsync(group.Id);
                var currentItems = _monitoredItems.GetOrAdd(group.Id, new List<MonitoredItem>());

                // Remover monitored items antigos que não estão mais no grupo ou cuja tag não está mais como Monitora
                var itemsToRemove = currentItems
                    .Where(item => !groupTags.Any(tag =>
                        tag.Monitora &&
                        !string.IsNullOrEmpty(tag.NodeIdOpc) &&
                        item.StartNodeId.ToString() == tag.NodeIdOpc))
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    subscription.RemoveItem(item);
                    currentItems.Remove(item);
                }

                var newItems = new List<MonitoredItem>();

                foreach (var tag in groupTags.Where(t => t.Monitora && !string.IsNullOrEmpty(t.NodeIdOpc)))
                {
                    var existingItem = currentItems.FirstOrDefault(item => item.StartNodeId.ToString() == tag.NodeIdOpc);

                    if (existingItem != null)
                    {
                        // Atualiza propriedades do item já existente
                        existingItem.SamplingInterval = group.UpdateRate;

                        if (existingItem.Filter is DataChangeFilter filter)
                        {
                            filter.DeadbandValue = group.Deadband;
                            existingItem.Filter = filter;
                        }

                        continue; // já está monitorando, apenas atualizado
                    }

                    // Criar novo monitored item
                    var monitoredItem = new MonitoredItem(subscription.DefaultItem)
                    {
                        DisplayName = tag.Nome,
                        StartNodeId = new NodeId(tag.NodeIdOpc),
                        AttributeId = Attributes.Value,
                        MonitoringMode = MonitoringMode.Reporting,
                        SamplingInterval = group.UpdateRate,
                        Filter = new DataChangeFilter
                        {
                            Trigger = DataChangeTrigger.StatusValue,
                            DeadbandType = (uint)DeadbandType.Absolute,
                            DeadbandValue = group.Deadband
                        },
                        DiscardOldest = true,
                        QueueSize = 10,
                        Handle = tag.Id
                    };

                    monitoredItem.Notification += OnMonitoredItemNotification;

                    newItems.Add(monitoredItem);
                    currentItems.Add(monitoredItem);
                }

                // Adiciona os novos itens e aplica todas as alterações
                if (newItems.Any() || itemsToRemove.Any())
                {
                    if (newItems.Any())
                    {
                        subscription.AddItems(newItems);
                        _logger.LogInformation($"Adicionados {newItems.Count} monitored items ao grupo {group.Name}");
                    }

                    await Task.Run(() => subscription.ApplyChanges(), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao configurar monitored items para grupo {group.Name}");
            }
        }


        private void OnMonitoredItemNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                foreach (var value in item.DequeueValues())
                {
                    if (item.Handle is Guid tagId)
                    {
                        ProcessDataChange(tagId, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar notification para tag {item.DisplayName}");
            }
        }


        private void OnFastDataChange(Subscription subscription, DataChangeNotification notification, IList<string> stringTable)
        {
            try
            {
                foreach (var item in notification.MonitoredItems)
                {
                    if (subscription.MonitoredItems.FirstOrDefault(mi => mi.ClientHandle == item.ClientHandle)?.Handle is Guid tagId)
                    {
                        ProcessDataChange(tagId, item.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mudança de dados");
            }
        }

        private void ProcessDataChange(Guid tagId, DataValue dataValue)
        {
            // Use Task.Run para capturar exceções e não bloquear thread de evento
            Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
                var leituraService = scope.ServiceProvider.GetRequiredService<ILeituraService>();
                var notificador = scope.ServiceProvider.GetRequiredService<INotificadorSimulacao>();

                try
                {
                    if (dataValue?.Value == null || !StatusCode.IsGood(dataValue.StatusCode))
                    {
                        _logger.LogWarning($"Valor inválido recebido para tag {tagId}");
                        return;
                    }

                    if (double.TryParse(dataValue.Value.ToString(), out var valorNumerico))
                    {
                        var tag = await tagService.GetByIdAsync(tagId);
                        if (tag != null)
                        {
                            tag.ValorAtual = valorNumerico;
                            await tagService.UpdateAsync(tag);

                            await leituraService.AddAsync(new LeituraDTO
                            {
                                TagId = tagId,
                                Valor = valorNumerico,
                                DataLeitura = dataValue.SourceTimestamp != DateTime.MinValue 
                                    ? dataValue.SourceTimestamp 
                                    : DateTime.UtcNow
                            });

                            await notificador.NotificarAtualizacaoTagAsync(
                                tagId,
                                valorNumerico,
                                dataValue.ServerTimestamp != DateTime.MinValue 
                                    ? dataValue.ServerTimestamp 
                                    : DateTime.UtcNow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao processar mudança de dados para tag {tagId}");
                }
            });
        }

        private async Task EnsureOpcDaMonitoringAsync(OpcServerDTO server, OpcGroupDTO group, CancellationToken cancellationToken)
        {
            if (!_opcDaClientService.IsSupported)
            {
                _logger.LogWarning("Servidor OPC DA '{Server}' ignorado: ambiente não Windows.", server.Nome);
                StopOpcDaGroup(group.Id);
                return;
            }

            if (!group.IsActive)
            {
                StopOpcDaGroup(group.Id);
                return;
            }

            var tags = await _groupService.GetGroupTagsAsync(group.Id);
            var itemIds = tags
                .Where(t => t.Monitora && !string.IsNullOrWhiteSpace(t.NodeIdOpc))
                .Select(t => t.NodeIdOpc!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (itemIds.Count == 0)
            {
                StopOpcDaGroup(group.Id);
                return;
            }

            var newSet = new HashSet<string>(itemIds, StringComparer.OrdinalIgnoreCase);
            if (_daGroupItems.TryGetValue(group.Id, out var currentSet))
            {
                if (!currentSet.SetEquals(newSet))
                {
                    StopOpcDaGroup(group.Id);
                    _daGroupItems[group.Id] = newSet;
                }
                else
                {
                    var existingTask = _daPollingTasks.GetValueOrDefault(group.Id);
                    if (existingTask != null && !existingTask.IsCompleted && !existingTask.IsCanceled)
                    {
                        return;
                    }
                }
            }
            else
            {
                _daGroupItems[group.Id] = newSet;
            }

            var cts = _daGroupCancellation.GetOrAdd(group.Id, _ => CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _daGroupCancellation[group.Id] = cts;
            }

            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var pollingTask = Task.Run(() => PollOpcDaGroupAsync(server, group, itemIds, linkedSource.Token), linkedSource.Token);
            _daPollingTasks[group.Id] = pollingTask;
        }

        private async Task PollOpcDaGroupAsync(OpcServerDTO server, OpcGroupDTO group, IReadOnlyList<string> itemIds, CancellationToken token)
        {
            _logger.LogInformation("Iniciando polling OPC DA para grupo '{Group}'", group.Name);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var values = await _opcDaClientService.ReadValuesAsync(server, itemIds);
                    foreach (var value in values)
                    {
                        await ProcessOpcDaValueAsync(value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao ler valores OPC DA para grupo '{Group}'", group.Name);
                }

                try
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Max(200, group.UpdateRate));
                    await Task.Delay(delay, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Encerrando polling OPC DA para grupo '{Group}'", group.Name);
        }

        private async Task ProcessOpcDaValueAsync(OpcTagDTO opcValue)
        {
            if (string.IsNullOrWhiteSpace(opcValue.NodeId) || string.IsNullOrWhiteSpace(opcValue.ValorAtual))
            {
                return;
            }

            if (!double.TryParse(opcValue.ValorAtual, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
            var leituraService = scope.ServiceProvider.GetRequiredService<ILeituraService>();
            var notificador = scope.ServiceProvider.GetRequiredService<INotificadorSimulacao>();

            try
            {
                var tag = await tagService.GetByNodeIdOpcAsync(opcValue.NodeId);
                if (tag == null || !tag.Monitora)
                {
                    return;
                }

                tag.ValorAtual = numericValue;
                await tagService.UpdateAsync(tag);

                var timestamp = opcValue.Timestamp ?? DateTime.UtcNow;

                await leituraService.AddAsync(new LeituraDTO
                {
                    TagId = tag.Id,
                    Valor = numericValue,
                    DataLeitura = timestamp
                });

                await notificador.NotificarAtualizacaoTagAsync(tag.Id, numericValue, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar leitura OPC DA para item '{ItemId}'", opcValue.NodeId);
            }
        }

        private void StopOpcDaGroup(Guid groupId)
        {
            if (_daGroupCancellation.TryRemove(groupId, out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch
                {
                    // ignorar
                }
                finally
                {
                    cts.Dispose();
                }
            }

            if (_daPollingTasks.TryRemove(groupId, out var task))
            {
                try
                {
                    task.Wait(TimeSpan.FromSeconds(1));
                }
                catch
                {
                    // ignorar
                }
            }

            _daGroupItems.TryRemove(groupId, out _);
        }


        private void OnSessionKeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
            {
                _logger.LogWarning($"KeepAlive falhou para sessão {session.SessionName}: {e.Status}");
            }
        }

        private void OnSessionNotification(ISession session, NotificationEventArgs e)
        {
            // Log de notificações se necessário
        }

        private void OnSubscriptionStateChanged(Subscription subscription, SubscriptionStateChangedEventArgs e)
        {
            _logger.LogInformation($"Estado da subscription {subscription.DisplayName} mudou para {e.Status}");
        }

        private async Task KeepSessionsAliveAsync(CancellationToken cancellationToken)
        {
            var tasks = _activeSessions.Values.Select(async session =>
            {
                try
                {
                    // Não precisa chamar KeepAlive manualmente, o evento já cuida disso
                    // Apenas verificar se a sessão está conectada
                    if (!session.Connected)
                    {
                        _logger.LogWarning($"Sessão {session.SessionName} desconectada");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao manter sessão {session.SessionName} viva");
                }
            });

            await Task.WhenAll(tasks);
        }

        public async Task StopMonitoringAsync()
        {
            _logger.LogInformation("Parando monitoramento OPC...");

            // Parar todas as subscriptions
            foreach (var subscription in _activeSubscriptions.Values)
            {
                try
                {
                    subscription.Delete(true);
                    subscription.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao parar subscription");
                }
            }
            _activeSubscriptions.Clear();
            _monitoredItems.Clear();

            foreach (var cts in _daGroupCancellation.Values)
            {
                try
                {
                    cts.Cancel();
                }
                catch
                {
                    // ignorar
                }
                finally
                {
                    cts.Dispose();
                }
            }
            _daGroupCancellation.Clear();

            foreach (var task in _daPollingTasks.Values)
            {
                try
                {
                    await task;
                }
                catch
                {
                    // ignorar
                }
            }
            _daPollingTasks.Clear();
            _daGroupItems.Clear();

            // Fechar todas as sessões
            foreach (var session in _activeSessions.Values)
            {
                try
                {
                    await session.CloseAsync();
                    session.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao fechar sessão");
                }
            }
            _activeSessions.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoringAsync().Wait(5000);
                _disposed = true;
            }
        }
    }
}

