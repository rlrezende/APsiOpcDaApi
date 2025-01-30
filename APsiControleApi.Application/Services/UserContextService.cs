using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace APsiControleApi.Application.Services
{

    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetEmpresaId()
        {
            var empresaIdClaim = _httpContextAccessor.HttpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == "IdEmpresa")?.Value;
            return Guid.TryParse(empresaIdClaim, out var empresaId) ? empresaId : null;
        }
    }
}