# APsiOpcDaApi

Microserviço responsável exclusivamente pelas rotinas de OPC DA/UA usadas pelo desktop (cadastro de servidores/grupos/tags, discovery, browser e jobs de monitoração). O código foi extraído da API original e roda isolado em `net9.0`, alvo `win-x86`, reutilizando as mesmas tabelas do banco existente – nenhuma migration nova é executada aqui.

## ⚠️ IMPORTANTE - Arquitetura x86

Esta API **OBRIGATORIAMENTE** requer arquitetura x86 (32-bit) devido às seguintes dependências:

- **OPC Classic Libraries** (`OpcNetApi.dll`, `OpcNetApi.Com.dll`) - Requerem x86 para interoperabilidade COM
- **Componentes de Manipulação Web** - WebBrowser Control e ActiveX precisam de x86 para funcionar corretamente
- **Bibliotecas nativas OPC** - A maioria dos servidores OPC DA são compilados em x86

### Por que x86?

1. **OPC Classic (OPC DA)** foi desenvolvido antes da era x64 e muitos servidores OPC ainda são x86
2. **Componentes COM/ActiveX** frequentemente dependem de registros x86 específicos
3. **WebBrowser Control** em .NET pode ter limitações em x64 para certas funcionalidades
4. **Interoperabilidade** - A maioria dos sistemas industriais ainda usa componentes x86

## Como rodar

```bash
cd APsiOpcDaApi
dotnet restore
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=apsic;Username=postgres;Password=postgres" \
dotnet run --project APsiOpcDaApi.API
```

A URL padrão é `http://0.0.0.0:5100` (configurável via `appsettings.json → OpcDaApi:BaseUrl`). O frontend deve usar `http://localhost:5100/api` para todos os endpoints OPC.

## Publicação x86

```bash
dotnet publish APsiOpcDaApi.API/APsiOpcDaApi.API.csproj \
  -c Release -r win-x86 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o ../../artifacts/APsiOpcDaApi
```

Esse passo também é disparado por `npm run build:artifacts`, que copia o resultado para `apps/APsiOpcDaApi` e embala junto ao Electron.

## Serviços Disponíveis

### OPC DA/UA Services
- **OpcServerService**: Gerenciamento de servidores OPC
- **OpcBrowserService**: Navegação em árvores OPC
- **OpcDaClientService**: Cliente OPC Classic (requer x86)
- **OpcDiscoveryService**: Descoberta automática de servidores

### Web Browser Services (x86)
- **WebBrowserService**: Manipulação de componentes web que requerem x86
  - `NavigateToPageAsync()`: Navegação web usando WebBrowser Control
  - `ExecuteScriptAsync()`: Execução de JavaScript em páginas
  - `ManipulateDomElementAsync()`: Manipulação de elementos DOM
  - `IsX86Supported()`: Verificação de suporte x86

### Endpoints API
- `/api/opcda/*`: Endpoints OPC Classic
- `/api/webbrowser/*`: Endpoints de manipulação web (requer x86)
- `/api/opc-connection/*`: Gerenciamento de conexões
- `/api/opcbrowser/*`: Navegação OPC

## Pontos importantes

- **Banco:** usa o mesmo schema e connection string da API principal (`APsiOpcDaApi`). Execute migrations somente no serviço original; aqui apenas consumimos as tabelas já criadas (`OpcServer`, `OpcGroup`, `OpcNode`, `Tag`, `Leitura`, etc.).
- **DLLs OPC DA:** continuam em `APsiOpcDaApi/Libs/Opc/*.dll` e são copiadas para o publish automaticamente.
- **Hosted services:** `OpcMonitorBackgroundService` e `DatabaseMonitorBackgroundService` estão habilitados para manter os cron jobs de leitura.
- **Configuração do frontend:** defina `NEXT_PUBLIC_API_OPC=http://localhost:5100/api` (ou ajuste `public/config.json`) para que os serviços `opc*` e `TagService` usem essa base via `opcApiService`.
- **Empacotamento:** `package.json` e `scripts/setup/config.json` agora referenciam `artifacts/APsiOpcDaApi → apps/APsiOpcDaApi`. Ajuste caminhos parecidos nos instaladores se mudar o nome da pasta.

