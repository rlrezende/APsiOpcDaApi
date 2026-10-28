using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.API.Controllers;
using APsiOpcDaApi.API.Services;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace APsiOpcDaApi.Tests.Security;

public sealed class HttpMutationBoundaryTests
{
    [Theory]
    [InlineData("server")]
    [InlineData("alias")]
    [InlineData("tag")]
    [InlineData("group-tags")]
    public async Task MissingOperationKeyRefusesBeforeServiceOrDatabase(string family)
    {
        using var db = new APsiOpcDaApiContext(new DbContextOptionsBuilder<APsiOpcDaApiContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NEVER_OPENED;Username=TEST-ONLY").Options);
        var runtime = new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);
        var principal = new Mock<IUserContextService>(MockBehavior.Strict);
        var mutation = new QuotaHttpMutation(db, runtime.Object, principal.Object);
        var servers = new Mock<IOpcServerService>(MockBehavior.Strict);
        var context = new ControllerContext { HttpContext = new DefaultHttpContext() };
        IActionResult result;
        if (family == "server")
        {
            var controller = new APsiOpcDaApi.Controllers.OpcServerController(servers.Object, mutation) { ControllerContext=context };
            result = await controller.Create(new OpcServerDTO { ModuloId=Guid.NewGuid() });
        }
        else if (family == "alias")
        {
            var controller = new OpcDaController(servers.Object, Mock.Of<IOpcBrowserService>(),
                Mock.Of<IOpcDaClientService>(), Mock.Of<IOpcDaServerEnumerator>(), mutation) { ControllerContext=context };
            result = await controller.Create(new OpcServerDTO(), Guid.NewGuid());
        }
        else if(family=="group-tags")
        {
            var controller=new OpcGroupController(new Mock<IOpcGroupService>(MockBehavior.Strict).Object,servers.Object,mutation){ControllerContext=context};
            result=await controller.AddTagsToGroup(Guid.NewGuid(),new AddTagsWithNodesToGroupRequest());
        }
        else
        {
            var controller = new TagController(Mock.Of<IGenericService<Tag,TagDTO>>(), Mock.Of<ITagService>(), mutation) { ControllerContext=context };
            result = (await controller.Create(new TagDTO())).Result!;
        }
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        servers.VerifyNoOtherCalls(); principal.VerifyNoOtherCalls(); runtime.VerifyNoOtherCalls();
    }
}
