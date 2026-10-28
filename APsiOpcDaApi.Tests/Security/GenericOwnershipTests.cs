using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Exceptions;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;
using Moq;

namespace APsiOpcDaApi.Tests.Security;
public sealed class GenericOwnershipTests
{
    private readonly Guid unit = Guid.NewGuid();
    private readonly Mock<IGenericRepository<Tag>> repository = new();
    private readonly Mock<IUserContextService> principal = new();
    private GenericService<Tag, TagDTO> Service()
    {
        var mapper = new MapperConfiguration(c => c.CreateMap<Tag, TagDTO>().ReverseMap()).CreateMapper();
        principal.Setup(p => p.HasAccessToResource(It.IsAny<object>())).Returns((object e) => e is Tag t && t.ModuloId == unit);
        return new(repository.Object, mapper, principal.Object);
    }
    [Fact] public async Task ListsAndPaginationNeverExposeForeignRowsOrTotals()
    {
        var own = new Tag { Id = Guid.NewGuid(), ModuloId = unit };
        repository.Setup(r => r.GetAllAsync()).ReturnsAsync([new Tag { Id = Guid.NewGuid(), ModuloId = Guid.NewGuid() }, own]);
        var service = Service();
        Assert.Equal(own.Id, Assert.Single(await service.GetAllAsync()).Id);
        var page = await service.GetPagedAsync(1, 10);
        Assert.Equal(1, page.totalItems); Assert.Equal(own.Id, Assert.Single(page.items).Id);
    }
    [Fact] public async Task MoveChecksOriginalAndDestinationBeforePersistence()
    {
        var tag = new Tag { Id = Guid.NewGuid(), ModuloId = unit };
        repository.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
        await Assert.ThrowsAsync<ResourceScopeDeniedException>(() => Service().UpdateAsync(new TagDTO { Id = tag.Id, ModuloId = Guid.NewGuid() }));
        Assert.Equal(unit, tag.ModuloId);
        repository.Verify(r => r.UpdateAsync(It.IsAny<Tag>()), Times.Never);
    }
    [Fact] public async Task ForeignSourceCannotBeMovedIntoOwnScope()
    {
        var tag = new Tag { Id = Guid.NewGuid(), ModuloId = Guid.NewGuid() };
        repository.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
        await Assert.ThrowsAsync<ResourceScopeDeniedException>(() => Service().UpdateAsync(new TagDTO { Id = tag.Id, ModuloId = unit }));
        Assert.NotEqual(unit, tag.ModuloId);
    }
    [Fact] public async Task BatchValidatesEveryOwnerBeforeAddingAnyRow()
    {
        await Assert.ThrowsAsync<ResourceScopeDeniedException>(() => Service().AddRangeAsync([new TagDTO { ModuloId = unit }, new TagDTO { ModuloId = Guid.NewGuid() }]));
        repository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Tag>>()), Times.Never);
    }
    [Fact] public async Task ReadAndDeleteDenyForeignResource()
    {
        var tag = new Tag { Id = Guid.NewGuid(), ModuloId = Guid.NewGuid() };
        repository.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
        var service = Service();
        await Assert.ThrowsAsync<ResourceScopeDeniedException>(() => service.GetByIdAsync(tag.Id));
        await Assert.ThrowsAsync<ResourceScopeDeniedException>(() => service.DeleteAsync(tag.Id));
        repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
