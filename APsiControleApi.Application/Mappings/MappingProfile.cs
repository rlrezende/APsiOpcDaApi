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
            // Mapeamento de Controle para ControleDTO
            CreateMap<Controle, ControleDTO>()
                .ForMember(dto => dto.UnidadeNome, opt => opt.Ignore());  // Ajuste se necessário para buscar o nome da unidade

            // Mapeamento de ControleDTO para Controle
            CreateMap<ControleDTO, Controle>();

            // Mapeamento de Tag para TagDTO
            CreateMap<Tag, TagDTO>()
                .ForMember(dto => dto.LeituraIds, opt => opt.MapFrom(src => src.Leituras.Select(l => l.Id)));

            // Mapeamento de TagDTO para Tag
            CreateMap<TagDTO, Tag>()
                .ForMember(entity => entity.Leituras, opt => opt.Ignore());

  // Mapeamento de Leitura para LeituraDTO, incluindo a Tag como objeto completo
            CreateMap<Leitura, LeituraDTO>()
                .ForMember(dto => dto.Tag, opt => opt.MapFrom(src => src.Tag));

            // Mapeamento de LeituraDTO para Leitura
            CreateMap<LeituraDTO, Leitura>()
                .ForMember(entity => entity.Tag, opt => opt.Ignore());  // Ignora a atribuição direta da Tag, evitando problemas de referência
            


             // Mapeamento para CorrelacaoResultadoDTO
            CreateMap<(Tag, Tag, double), CorrelacaoResultadoDTO>()
                .ForMember(dto => dto.Tag1Id, opt => opt.MapFrom(src => src.Item1.Id))
                .ForMember(dto => dto.Tag2Id, opt => opt.MapFrom(src => src.Item2.Id))
                .ForMember(dto => dto.Tag1Nome, opt => opt.MapFrom(src => src.Item1.Nome))
                .ForMember(dto => dto.Tag2Nome, opt => opt.MapFrom(src => src.Item2.Nome))
                .ForMember(dto => dto.ValorCorrelacao, opt => opt.MapFrom(src => src.Item3));
        }
    }
}
