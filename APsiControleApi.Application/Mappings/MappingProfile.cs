using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;
using AutoMapper;
using System.Linq;
using NodaTime;
using System.Collections.Generic;

namespace APsiControleApi.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeamento de Controle para ControleDTO
            CreateMap<Controle, ControleDTO>()
                .ForMember(dto => dto.UnidadeNome, opt => opt.Ignore());

            // Mapeamento de ControleDTO para Controle
            CreateMap<ControleDTO, Controle>();

            // Mapeamento de Tag para TagDTO: evita leitura automática de Leituras
            CreateMap<Tag, TagDTO>()
                // Remove o acesso direto a Leituras para não disparar Lazy Loading
                .ForMember(dto => dto.LeituraIds, opt => opt.Ignore());

            // Mapeamento de TagDTO para Tag
            CreateMap<TagDTO, Tag>()
                .ForMember(entity => entity.Leituras, opt => opt.Ignore());

            // Convertendo de Leitura (Instant) para LeituraDTO (DateTime)
            CreateMap<Leitura, LeituraDTO>()
                .ForMember(dto => dto.DataLeitura, opt => opt.MapFrom(src => src.DataLeitura.ToDateTimeUtc()))
                .ForMember(dto => dto.Tag, opt => opt.MapFrom(src => src.Tag));

            // Convertendo de LeituraDTO (DateTime) para Leitura (Instant)
            CreateMap<LeituraDTO, Leitura>()
                .ForMember(entity => entity.DataLeitura, opt => opt.MapFrom(src => Instant.FromDateTimeUtc(src.DataLeitura.ToUniversalTime())))
                .ForMember(entity => entity.Tag, opt => opt.Ignore());

            // Mapeamento para CorrelacaoResultadoDTO
            CreateMap<(Tag, Tag, double), CorrelacaoResultadoDTO>()
                .ForMember(dto => dto.Tag1Id, opt => opt.MapFrom(src => src.Item1.Id))
                .ForMember(dto => dto.Tag2Id, opt => opt.MapFrom(src => src.Item2.Id))
                .ForMember(dto => dto.Tag1Nome, opt => opt.MapFrom(src => src.Item1.Nome))
                .ForMember(dto => dto.Tag2Nome, opt => opt.MapFrom(src => src.Item2.Nome))
                .ForMember(dto => dto.ValorCorrelacao, opt => opt.MapFrom(src => src.Item3));

            // Mapeamento OpcServer
            CreateMap<OpcServer, OpcServerDTO>()
                .ForMember(dto => dto.NodeIds, opt => opt.MapFrom(src => src.Nodes != null ? src.Nodes.Select(n => n.Id) : new List<Guid>()));

            CreateMap<OpcServerDTO, OpcServer>()
                .ForMember(entity => entity.Nodes, opt => opt.Ignore());
            
            CreateMap<OpcNode, OpcNodeDTO>();
            CreateMap<OpcNodeDTO, OpcNode>()
                .ForMember(dest => dest.Server, opt => opt.Ignore()); 

            // Mapeamento para OpcGroup
            CreateMap<OpcGroup, OpcGroupDTO>()
                .ForMember(dto => dto.ServerName, opt => opt.MapFrom(src => src.Server != null ? src.Server.Nome : string.Empty))
                .ForMember(dto => dto.TagCount, opt => opt.MapFrom(src => src.Tags != null ? src.Tags.Count : 0))
                .ForMember(dto => dto.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dto => dto.LastUpdate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ForMember(dto => dto.TagIds, opt => opt.MapFrom(src => src.Tags != null ? src.Tags.Select(t => t.Id).ToList() : new List<Guid>()));

            CreateMap<OpcGroupDTO, OpcGroup>()
                .ForMember(dest => dest.Server, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore())
                .ForMember(dest => dest.ServerId, opt =>
                {
                    opt.PreCondition(src => src.ServerId != Guid.Empty);
                    opt.MapFrom(src => src.ServerId);
                });

            // Mapeamento para OpcDiscoveredServer
            CreateMap<OpcDiscoveredServer, OpcDiscoveredServerDTO>()
                .ForMember(dto => dto.SecurityModes, opt => opt.MapFrom(src => 
                    string.IsNullOrEmpty(src.SecurityModes) ? new List<string>() : 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(src.SecurityModes) ?? new List<string>()));

            CreateMap<OpcDiscoveredServerDTO, OpcDiscoveredServer>()
                .ForMember(entity => entity.SecurityModes, opt => opt.MapFrom(src => 
                    Newtonsoft.Json.JsonConvert.SerializeObject(src.SecurityModes)));
        }
    }
}
