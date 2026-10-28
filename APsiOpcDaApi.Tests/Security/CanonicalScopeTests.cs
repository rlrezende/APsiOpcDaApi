using System.Security.Claims;
using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.API.Services;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace APsiOpcDaApi.Tests.Security;
public sealed class CanonicalScopeTests
{
    [Theory]
    [InlineData("Modulos")]
    [InlineData("Unidades")]
    [InlineData("CompanyWide")]
    [InlineData("UnidadeId")]
    public void LegacyOrMalformedScopeCannotReachDatabaseOrMapping(string claim)
    {
        var company = Guid.NewGuid(); var unit = Guid.NewGuid();
        using var db = new APsiOpcDaApiContext(new DbContextOptionsBuilder<APsiOpcDaApiContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NEVER_OPENED;Username=TEST-ONLY").Options);
        var runtime = new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);
        runtime.SetupGet(r => r.EmpresaId).Returns(company);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("IdEmpresa", company.ToString()), new Claim(claim, claim == "UnidadeId" ? "invalid" : unit.ToString())], "test")) } };
        var scope = new CanonicalResourceUserContextService(accessor, db, runtime.Object);
        var server = (OpcServer)Activator.CreateInstance(typeof(OpcServer), true)!; server.ModuloId = unit;
        Assert.False(scope.HasAccessToResource(server));
        runtime.Verify(r => r.OpenQuota(It.IsAny<Npgsql.NpgsqlConnection>()), Times.Never);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
    [Fact] public void MissingHttpPrincipalNeverBecomesWorkerAuthority()
    {
        using var db = new APsiOpcDaApiContext(new DbContextOptionsBuilder<APsiOpcDaApiContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NEVER_OPENED;Username=TEST-ONLY").Options);
        var runtime = new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);
        var scope = new CanonicalResourceUserContextService(new HttpContextAccessor(), db, runtime.Object);
        Assert.False(scope.HasAccessToResource(new Tag { ModuloId = Guid.NewGuid() }));
    }
}
