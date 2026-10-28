using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.API.Services;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace APsiOpcDaApi.Tests.Security;
public sealed class QuotaBoundaryTests
{
    [Fact] public async Task TagPersistenceRefusesUnprovedTopologyBeforeOpeningDatabase()
    {
        var runtime = new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);
        runtime.Setup(r => r.OpenQuota(It.IsAny<Npgsql.NpgsqlConnection>())).Throws(new IdentityException("QUOTA_TOPOLOGY_INCOMPATIBLE"));
        using var context = new APsiOpcDaApiContext(new DbContextOptionsBuilder<APsiOpcDaApiContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NEVER_OPENED;Username=TEST-ONLY").Options, new QuotaSaveBoundary(runtime.Object));
        context.Tag.Add(new Tag { Id = Guid.NewGuid(), ModuloId = Guid.NewGuid() });
        var error = await Assert.ThrowsAsync<QuotaMutationException>(() => context.SaveChangesAsync());
        Assert.Equal("QUOTA_TOPOLOGY_INCOMPATIBLE", error.Code);
        Assert.Equal(EntityState.Added, context.ChangeTracker.Entries().Single().State);
    }
    [Theory]
    [InlineData(APsiOpcDaApi.Domain.Enum.TipoOpcServer.Ua)]
    [InlineData(APsiOpcDaApi.Domain.Enum.TipoOpcServer.Database)]
    public async Task NonDaProtocolCannotUseGenericPersistenceBoundary(APsiOpcDaApi.Domain.Enum.TipoOpcServer protocol)
    {
        var runtime=new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);
        using var context=new APsiOpcDaApiContext(new DbContextOptionsBuilder<APsiOpcDaApiContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NEVER_OPENED;Username=TEST-ONLY").Options,new QuotaSaveBoundary(runtime.Object));
        var server=(OpcServer)Activator.CreateInstance(typeof(OpcServer),true)!;
        server.Id=Guid.NewGuid();server.ModuloId=Guid.NewGuid();server.Tipo=protocol;context.OpcServers.Add(server);
        Assert.Equal("PROTOCOL_OWNER_MISMATCH",(await Assert.ThrowsAsync<QuotaMutationException>(()=>context.SaveChangesAsync())).Code);
        runtime.VerifyNoOtherCalls();
    }
}
