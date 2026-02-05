using System.Threading.Tasks;

namespace APsiOpcDaApi.Application.Interfaces
{
    /// <summary>
    /// Interface para serviços de manipulação web browser que requerem arquitetura x86
    /// </summary>
    public interface IWebBrowserService
    {
        /// <summary>
        /// Verifica se o ambiente suporta componentes x86
        /// </summary>
        bool IsX86Supported();

        /// <summary>
        /// Navega para uma URL específica
        /// </summary>
        /// <param name="url">URL para navegar</param>
        /// <returns>Conteúdo da página</returns>
        Task<string> NavigateToPageAsync(string url);

        /// <summary>
        /// Executa JavaScript na página atual
        /// </summary>
        /// <param name="script">Script JavaScript para executar</param>
        /// <returns>Resultado da execução</returns>
        Task<object?> ExecuteScriptAsync(string script);

        /// <summary>
        /// Manipula elementos DOM
        /// </summary>
        /// <param name="elementId">ID do elemento</param>
        /// <param name="action">Ação a ser executada</param>
        /// <returns>Task representando a operação</returns>
        Task ManipulateDomElementAsync(string elementId, string action);
    }
}