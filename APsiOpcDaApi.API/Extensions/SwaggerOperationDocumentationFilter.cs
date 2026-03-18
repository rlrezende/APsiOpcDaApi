using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace APsiOpcDaApi.API.Extensions
{
    public class SwaggerOperationDocumentationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor cad)
            {
                return;
            }

            var httpMethod = (context.ApiDescription.HttpMethod ?? "GET").ToUpperInvariant();
            var route = "/" + (context.ApiDescription.RelativePath ?? string.Empty).TrimStart('/');

            var businessDescription = BuildBusinessDescription(cad.ControllerName, cad.ActionName, httpMethod);

            operation.Summary ??= $"{httpMethod} {route}";

            var hasAllowAnonymous = cad.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any()
                || cad.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();

            var hasAuthorize = cad.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()
                || cad.ControllerTypeInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any();

            var authRequired = hasAuthorize && !hasAllowAnonymous;
            var authText = authRequired ? "Sim (Bearer JWT)" : "Não";

            var descriptionLines = new List<string>
            {
                businessDescription,
                $"Rota: {httpMethod} {route}",
                $"Requer autenticação: {authText}",
                "Dica de debug: endpoints OPC podem depender de ambiente x86 e servidor OPC disponível.",
                "Dica de debug: use o botão Authorize no Swagger para manter o token entre chamadas."
            };

            operation.Description = string.Join("\n", descriptionLines.Where(l => !string.IsNullOrWhiteSpace(l))).Trim();
        }

        private static string BuildBusinessDescription(string controller, string action, string httpMethod)
        {
            return (controller, action) switch
            {
                ("OpcDa", "GetAll") => "Lista servidores OPC DA cadastrados.",
                ("OpcDa", "GetById") => "Obtém servidor OPC DA pelo ID.",
                ("OpcDa", "Create") => "Cria novo servidor OPC DA.",
                ("OpcDa", "Update") => "Atualiza dados de servidor OPC DA.",
                ("OpcDa", "Delete") => "Remove servidor OPC DA.",
                ("OpcDa", "DiscoverLocal") => "Descobre servidores OPC no host informado.",
                ("OpcDa", "Browse") => "Navega itens/nós do servidor OPC DA.",

                ("OpcConnection", "ConnectToServer") => "Abre conexão com servidor OPC.",
                ("OpcConnection", "DisconnectFromServer") => "Fecha conexão com servidor OPC.",
                ("OpcConnection", "GetConnectionStatus") => "Consulta status de conexão de um servidor.",
                ("OpcConnection", "GetActiveConnections") => "Lista conexões OPC ativas.",

                ("OpcDiscovery", "ScanNetwork") => "Varre rede para encontrar servidores OPC.",
                ("OpcDiscovery", "AddManualServer") => "Adiciona servidor OPC manualmente.",
                ("OpcDiscovery", "GetDiscoveredServers") => "Lista servidores descobertos.",
                ("OpcDiscovery", "TestConnection") => "Testa conectividade de servidor OPC.",
                ("OpcDiscovery", "DiscoverAndSaveLocalhost") => "Descobre e salva servidor local.",

                ("OpcGroup", "GetAllGroups") => "Lista todos os grupos OPC.",
                ("OpcGroup", "GetGroupsByServer") => "Lista grupos OPC de um servidor específico.",
                ("OpcGroup", "GetActiveGroups") => "Lista grupos OPC ativos.",
                ("OpcGroup", "GetGroup") => "Obtém detalhes de um grupo OPC.",
                ("OpcGroup", "CreateGroup") => "Cria novo grupo OPC.",
                ("OpcGroup", "UpdateGroup") => "Atualiza grupo OPC.",
                ("OpcGroup", "DeleteGroup") => "Exclui grupo OPC.",
                ("OpcGroup", "ActivateGroup") => "Ativa grupo OPC.",
                ("OpcGroup", "DeactivateGroup") => "Desativa grupo OPC.",
                ("OpcGroup", "GetGroupTags") => "Lista tags vinculadas ao grupo OPC.",
                ("OpcGroup", "AddTagsToGroup") => "Vincula tags ao grupo OPC.",
                ("OpcGroup", "RemoveTagFromGroup") => "Remove tag de um grupo OPC.",

                ("OpcBrowser", "BrowseNodes") => "Navega nós de servidor OPC por nó pai.",
                ("OpcDatabase", "BrowseDatabase") => "Navega dados persistidos do servidor no banco.",

                ("WebBrowser", "CheckX86Support") => "Verifica suporte/estado do componente web em ambiente x86.",
                ("WebBrowser", "NavigateToPage") => "Abre navegação controlada para página/URL alvo.",
                ("WebBrowser", "ExecuteScript") => "Executa script no contexto de navegação gerenciado.",
                ("WebBrowser", "ManipulateDom") => "Manipula DOM da página carregada para automações.",

                ("OpcServer", "GetCapabilities") => "Retorna capacidades suportadas pela integração OPC.",

                _ => BuildGenericCrudDescription(controller, action, httpMethod)
            };
        }

        private static string BuildGenericCrudDescription(string controller, string action, string httpMethod)
        {
            if (action.Contains("GetById", StringComparison.OrdinalIgnoreCase) || action.Equals("Get", StringComparison.OrdinalIgnoreCase))
                return $"Consulta um registro de {controller} pelo identificador.";

            if (action.Contains("GetAll", StringComparison.OrdinalIgnoreCase) || action.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                return $"Lista registros de {controller}.";

            if (action.Contains("GetPaged", StringComparison.OrdinalIgnoreCase) || action.Contains("Pagin", StringComparison.OrdinalIgnoreCase))
                return $"Retorna lista paginada de {controller}.";

            if (action.Contains("Add", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Create", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Criar", StringComparison.OrdinalIgnoreCase))
                return $"Cria novo registro de {controller}.";

            if (action.Contains("Update", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Put", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Toggle", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Activate", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Deactivate", StringComparison.OrdinalIgnoreCase))
                return $"Atualiza estado/dados de {controller}.";

            if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Excluir", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Remove", StringComparison.OrdinalIgnoreCase))
                return $"Exclui registro de {controller}.";

            return $"Executa operação de {controller} ({httpMethod}).";
        }
    }
}
