# APsiOpcDaApi — Manual de Debug e Swagger

## 1) Objetivo
Orientar depuração local da API OPC DA, uso do Swagger e autenticação com token para testes.

## 2) Arquivos de configuração importantes
- `.vscode/launch.json`: perfil de debug local.
- `.vscode/tasks.json`: build e limpeza de portas/processos.
- `APsiOpcDaApi.API/appsettings.json`: config padrão.
- `APsiOpcDaApi.API/appsettings.Development.json`: config local de Development.
- `APsiOpcDaApi.API/Program.cs`: pipeline, Swagger, CORS e SignalR.
- `APsiOpcDaApi.API/Extensions/ServiceExtensions.cs`: autenticação JWT + Swagger config.
- `APsiOpcDaApi.API/wwwroot/swagger-default-auth.js`: helper de token.

## 3) Pré-requisitos
- .NET SDK compatível (`net9.0`).
- Dependências OPC disponíveis.
- Ambiente x86 quando necessário (integrações legadas OPC Classic).

## 4) Como debugar (VS Code)
1. Abrir repositório no VS Code.
2. Selecionar perfil da API OPC DA.
3. Rodar F5.
4. Verificar URL/porta no terminal.

## 5) Swagger
- URL padrão: `http://localhost:5003/swagger`
- Deve exibir botão **Authorize**.

## 6) Token no Swagger
1. Obter token da API autorizadora (ou endpoint interno usado no ambiente).
2. Clicar em **Authorize**.
3. Informar `Bearer {token}`.
4. Executar endpoints protegidos.

## 7) Rotas principais (visão funcional)
### OPC DA
- `GET /api/opcda`
- `GET /api/opcda/{id}`
- `POST /api/opcda`
- `PUT /api/opcda/{id}`
- `DELETE /api/opcda/{id}`
- `GET /api/opcda/discover-local`
- `GET /api/opcda/{id}/browse`

### Conexão OPC
- `POST /api/opc-connection/connect/{serverId}`
- `POST /api/opc-connection/disconnect/{serverId}`
- `GET /api/opc-connection/status/{serverId}`
- `GET /api/opc-connection/active`

### Descoberta
- `GET /api/opc-discovery/scan`
- `POST /api/opc-discovery/add-manual`
- `GET /api/opc-discovery/servers`
- `POST /api/opc-discovery/test-connection`
- `POST /api/opc-discovery/discover-localhost`

### Estruturas OPC
- Servidores: `/api/opcserver`
- Nós: `/api/opcnode`
- Grupos: `/api/opc-groups`
- Tags: `/api/tag`
- Browser/Database auxiliares: `/api/opcbrowser`, `/api/opcdatabase`

### Funcionalidades Web auxiliares
- `POST /api/webbrowser/navigate`
- `POST /api/webbrowser/execute-script`
- `POST /api/webbrowser/manipulate-dom`

## 8) Problemas comuns
- **401 Unauthorized**: token inválido/expirado ou chave diferente.
- **Sem botão Authorize**: revisar `AddSecurityDefinition`/`AddSecurityRequirement` no Swagger.
- **Falha de conexão OPC**: endpoint e servidor OPC indisponíveis.
- **Porta em uso**: executar task de kill-port antes de subir.

## 9) Checklist rápido
- [ ] Build ok.
- [ ] API subiu em Development.
- [ ] Swagger abriu com Authorize.
- [ ] Token aplicado com `Bearer`.
- [ ] Endpoints OPC respondendo.
