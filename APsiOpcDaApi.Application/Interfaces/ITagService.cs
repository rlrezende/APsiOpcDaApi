using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Entities;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface ITagService : IGenericService<Tag, TagDTO>
    {
        /// <summary>
        /// Processa a aba "Tags" de um arquivo Excel e insere os dados no banco.
        /// </summary>
        /// <param name="planilha">A planilha contendo os dados de tags</param>
        /// <param name="unidadeId">ID da unidade associada às tags</param>
        /// <returns>Retorna um dicionário com o nome da tag e seu respectivo ID</returns>
        Task<Dictionary<int, Guid>> ProcessarTagsAsync(ExcelWorksheet planilha, Guid unidadeId);

        Task<(IEnumerable<TagDTO> items, int totalItems)> GetPagedTagsWithReadingsAsync(int pageIndex, int pageSize);

        Task<Guid?> GetOpcServerIdByTagIdAsync(Guid tagId);
         
         Task<IEnumerable<TagDTO>> GetTagsByServerAsync(Guid serverId, string origem);

        Task<TagDTO?> GetByNodeIdOpcAsync(string nodeIdOpc);

    }
}

