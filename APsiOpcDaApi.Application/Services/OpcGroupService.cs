using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiOpcDaApi.Application.Services
{
    public class OpcGroupService : GenericService<OpcGroup, OpcGroupDTO>, IOpcGroupService
    {
        private readonly IOpcGroupRepository _groupRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITagService _tagService;

        private readonly IOpcNodeService _opcNodeService;
        private readonly IMapper _mapper;

        public OpcGroupService(
            IOpcGroupRepository repository,
            IMapper mapper,
            IUserContextService userContextService,
            IOpcNodeService opcNodeService,
            ITagService tagService,IUnitOfWork unitOfWork)
            : base(repository, mapper, userContextService)
        {
            _groupRepository = repository;
            _unitOfWork=unitOfWork;
            _tagService = tagService;
            _mapper = mapper;
            _opcNodeService = opcNodeService;
        }

        public async Task<List<OpcGroupDTO>> GetGroupsByServerIdAsync(Guid serverId)
        {
            var groups = await _groupRepository.GetGroupsByServerIdAsync(serverId);
            return _mapper.Map<List<OpcGroupDTO>>(groups.Where(g => InScope(g)))
                .OrderBy(group => group.Name)
                .ThenBy(group => group.Id)
                .ToList();
        }

        public async Task<List<OpcGroupDTO>> GetGroupsByUnidadeIdAsync(Guid unidadeId, bool activeOnly = false)
        {
            var groups = await _groupRepository.GetGroupsByUnidadeIdAsync(unidadeId, activeOnly);
            return _mapper.Map<List<OpcGroupDTO>>(groups.Where(g => InScope(g)));
        }

        public async Task<List<OpcGroupDTO>> GetActiveGroupsAsync()
        {
            var groups = await _groupRepository.GetActiveGroupsAsync();
            return _mapper.Map<List<OpcGroupDTO>>(groups.Where(g => InScope(g)))
                .OrderBy(group => group.Name)
                .ThenBy(group => group.Id)
                .ToList();
        }

        public override async Task<IEnumerable<OpcGroupDTO>> GetAllAsync()
        {
            var groups = await base.GetAllAsync();
            return groups
                .OrderBy(group => group.Name)
                .ThenBy(group => group.Id)
                .ToList();
        }

        public async Task<OpcGroupDTO> GetGroupWithTagsAsync(Guid groupId)
        {
            var group = await _groupRepository.GetGroupWithTagsAsync(groupId);
            if (group != null) DemandScope(group);
            var dto=_mapper.Map<OpcGroupDTO>(group);
            if(group is not null && dto is not null)
            {
                dto.TagIds=group.Tags.Where(t=>InScope(t)).Select(t=>t.Id).ToList();
                dto.TagCount=dto.TagIds.Count;
            }
            return dto;
        }

        public async Task<bool> ActivateGroupAsync(Guid groupId)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null) return false;
            DemandScope(group);

            group.IsActive = true;
            await _groupRepository.UpdateAsync(group);
            return true;
        }

        public async Task<bool> DeactivateGroupAsync(Guid groupId)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null) return false;
            DemandScope(group);

            group.IsActive = false;
            await _groupRepository.UpdateAsync(group);
            return true;
        }

        public async Task<int> DeactivateGroupsByServerAsync(Guid serverId)
        {
            var groups = await _groupRepository.GetGroupsByServerIdAsync(serverId);
            var activeGroups = groups.Where(group => group.IsActive && InScope(group)).ToList();

            foreach (var groupDto in _mapper.Map<List<OpcGroupDTO>>(activeGroups))
            {
                groupDto.IsActive = false;
                await UpdateAsync(groupDto);
            }

            return activeGroups.Count;
        }

        public async Task<List<TagDTO>> GetGroupTagsAsync(Guid groupId)
        {
            var group = await _groupRepository.GetGroupWithTagsAsync(groupId);
            if (group == null) return new List<TagDTO>();
            DemandScope(group);

            var orderedTags = group.Tags.Where(t => InScope(t))
                .OrderBy(tag => tag.Nome)
                .ThenBy(tag => tag.NodeIdOpc)
                .ToList();

            return _mapper.Map<List<TagDTO>>(orderedTags);
        }

        public async Task<bool> AddTagsToGroupAsync(Guid groupId, List<TagDTO> tagDtos)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null) return false;
            DemandScope(group);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
            foreach (var tagDto in tagDtos)
            {
                 var newNode = new OpcNodeDTO
                {
                    Id = Guid.NewGuid(),
                    Nome = tagDto.Nome,
                    NodeId = tagDto.NodeIdOpc ?? "", // obrigatório
                    ServerId = group.ServerId       // se for ServerId, ajuste aqui
                };

               var node =  await _opcNodeService.AddAsync(newNode);

                tagDto.GroupId = groupId;
                tagDto.NodeId = node.Id;
                await _tagService.AddAsync(tagDto);
            } 

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }


        public async Task<bool> RemoveTagFromGroupAsync(Guid groupId, Guid tagId)
        {
            var tag = await _tagService.GetByIdAsync(tagId);
            if (tag == null || tag.GroupId != groupId) return false;

            tag.GroupId = null;
            await _tagService.UpdateAsync(tag);
            return true;
        }
    }
}

