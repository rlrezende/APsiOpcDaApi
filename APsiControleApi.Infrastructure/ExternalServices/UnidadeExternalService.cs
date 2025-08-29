using APsiControleApi.Application.DTOs;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using APsiControleApi.Application.Interfaces;

namespace APsiControleApi.Infrastructure.ExternalServices
{
    public class UnidadeExternalService : IUnidadeExternalService
    {
        private readonly HttpClient _httpClient;

        public UnidadeExternalService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Guid> CriarUnidadeAsync(UnidadeDto unidadeDto)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/unidades", unidadeDto);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Guid>();
            return result;
        }
    }
}
