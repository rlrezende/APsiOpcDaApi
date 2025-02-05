using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APsiControleApi.Application.Services
{
    public class TagService : GenericService<Tag, TagDTO>, ITagService
    {

        ITagRepository _tagRepository;
        IMapper _mapper;

        public TagService(IGenericRepository<Tag> repository, IMapper mapper, IUserContextService userContextService , ITagRepository tagRepository)
            : base(repository, mapper, userContextService)
        {
            _tagRepository = tagRepository;
            _mapper= mapper;
        }

        /// <summary>
        /// Processa a planilha de tags, insere os dados no banco e retorna um mapeamento de nome para ID.
        /// </summary>
        /// <param name="planilha">Planilha Excel contendo os dados de tags</param>
        /// <param name="unidadeId">ID da unidade associada às tags</param>
        /// <returns>Dicionário com o nome da tag e seu respectivo ID</returns>
        public async Task<Dictionary<int, Guid>> ProcessarTagsAsync(ExcelWorksheet planilha, Guid unidadeId)
        {
            var linhas = planilha.Dimension.Rows;
            var tagMap = new Dictionary<int, Guid>();

            for (int i = 2; i <= linhas; i++)
            {
                // Obtém o índice e os valores associados à tag
                if (!int.TryParse(planilha.Cells[i, 1].Text, out var tagIndex))
                {
                    Console.WriteLine($"[Aviso] Linha {i}: 'TagIndex' inválido. Registro ignorado.");
                    continue;
                }

                var nome = planilha.Cells[i, 2].Text;
                var descricao = planilha.Cells[i, 3].Text;

                // Cria um DTO para a tag
                var tagDto = new TagDTO
                {
                    Nome = nome,
                    Descricao = descricao,
                    UnidadeId = unidadeId
                };

                // Salva a tag no banco e recupera o ID criado
                var createdTagDto = await AddAsync(tagDto);

                // Adiciona o mapeamento tag_index -> ID da tag
                tagMap[tagIndex] = createdTagDto.Id;
            }

            return tagMap;
        }

         /// <summary>
        /// Retorna tags paginadas que possuem leituras associadas.
        /// </summary>
        /// <param name="pageIndex">Índice da página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista de tags e o total de itens</returns>
        public async Task<(IEnumerable<TagDTO> items, int totalItems)> GetPagedTagsWithReadingsAsync(int pageIndex, int pageSize)
        {
            var (tags, totalItems) = await _tagRepository.GetPagedTagsWithReadingsAsync(pageIndex, pageSize);
            var tagsDto = _mapper.Map<IEnumerable<TagDTO>>(tags);

            return (tagsDto, totalItems);
        }

    }
}
