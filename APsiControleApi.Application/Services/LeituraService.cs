using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiControleApi.Application.Services
{
    public class TagService : GenericService<Perfil, TagDTO>, ITagService
    {
        public TagService(IGenericRepository<Tag> repository, IMapper mapper, IUserContextService userContextService)
            : base(repository,mapper, userContextService)
        {
        }
    }
}
