using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiControleApi.Application.Services
{
    public class LeituraService : GenericService<Leitura, LeituraDTO>, ILeituraService
    {
        public LeituraService(IGenericRepository<Leitura> repository, IMapper mapper, IUserContextService userContextService)
            : base(repository,mapper, userContextService)
        {
        }
    }
}
