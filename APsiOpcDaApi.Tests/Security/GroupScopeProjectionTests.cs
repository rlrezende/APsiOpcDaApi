using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;
using Moq;
namespace APsiOpcDaApi.Tests.Security;
public sealed class GroupScopeProjectionTests
{
    [Fact]
    public async Task FilteringReadProjectionCannotDetachForeignTagsFromTrackedGroup()
    {
        var group=(OpcGroup)Activator.CreateInstance(typeof(OpcGroup),true)!;group.Id=Guid.NewGuid();var unit=Guid.NewGuid();
        var owned=new Tag{Id=Guid.NewGuid(),ModuloId=unit};var foreign=new Tag{Id=Guid.NewGuid(),ModuloId=Guid.NewGuid()};group.Tags.Add(owned);group.Tags.Add(foreign);
        var repository=new Mock<IOpcGroupRepository>();repository.Setup(r=>r.GetGroupWithTagsAsync(group.Id)).ReturnsAsync(group);
        var principal=new Mock<IUserContextService>();principal.Setup(p=>p.HasAccessToResource(It.IsAny<object>())).Returns((object value)=>value is OpcGroup || value is Tag tag && tag.ModuloId==unit);
        var mapper=new MapperConfiguration(c=>c.CreateMap<OpcGroup,OpcGroupDTO>()).CreateMapper();
        var service=new OpcGroupService(repository.Object,mapper,principal.Object,Mock.Of<IOpcNodeService>(),Mock.Of<ITagService>(),Mock.Of<IUnitOfWork>());
        var projected=await service.GetGroupWithTagsAsync(group.Id);
        Assert.Equal(new[]{owned.Id},projected.TagIds);Assert.Equal(1,projected.TagCount);Assert.Equal(2,group.Tags.Count);Assert.Contains(foreign,group.Tags);
    }
}
