using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiControleApi.Application.Services
{
    public class ControleService : GenericService<Controle, ControleDTO>, IControleService
    {
        public ControleService(IGenericRepository<Controle> repository, IMapper mapper, IUserContextService userContextService)
            : base(repository,mapper, userContextService)
        {
        }
    }
}
