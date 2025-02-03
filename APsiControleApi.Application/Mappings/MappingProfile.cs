using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;
using AutoMapper;
using System.Linq;

namespace APsiControleApi.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeamento de Tag para TagDTO
            CreateMap<Tag, TagDTO>()
                .ForMember(dto => dto.LeituraIds, opt => opt.MapFrom(src => src.Leituras.Select(l => l.Id)));

            // Mapeamento de TagDTO para Tag
            CreateMap<TagDTO, Tag>()
                .ForMember(entity => entity.Leituras, opt => opt.Ignore());


            // Mapeamento de Leitura para LeituraDTO
            CreateMap<Leitura, LeituraDTO>()
                .ForMember(dto => dto.TagNome, opt => opt.MapFrom(src => src.Tag.Nome));

            // Mapeamento de LeituraDTO para Leitura
            CreateMap<LeituraDTO, Leitura>()
                .ForMember(entity => entity.Tag, opt => opt.Ignore());
        }
    }
}