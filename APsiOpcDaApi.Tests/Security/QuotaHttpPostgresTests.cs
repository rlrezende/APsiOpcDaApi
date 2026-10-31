using System.Security.Claims;
using APsiC.Licensing;
using APsiC.Licensing.AspNetCore;
using APsiC.Licensing.Infrastructure;
using APsiC.Licensing.Tests;
using APsiOpcDaApi.API.Services;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;

namespace APsiOpcDaApi.Tests.Security;
public sealed class QuotaHttpPostgresTests
{
    private sealed class Identity(InstallationIdentity value):IInstallationIdentity { public InstallationIdentity Read()=>value; }
    private sealed class Clock:TimeProvider { public override DateTimeOffset GetUtcNow()=>TestAuthority.Now; }
    private sealed class Runtime(string connection,string id,Guid company,PostgresLicenseStore store,LocalLicenseService licensing):ILocalLicensingRuntime
    {
        public Guid EmpresaId=>company;
        public LocalLicenseService Open()=>licensing;
        public PostgresOperationalStore OpenOperational()=>new(connection,id,company,licensing);
        public PostgresAuthorizationStore OpenAuthorization()=>new(connection,id,company);
        public PostgresQuotaCoordinator OpenQuota(NpgsqlConnection resource)
        { PostgresQuotaTopology.DemandSameEndpoint(connection,resource.ConnectionString);return new(store,licensing,"apsic-modern",id); }
    }
    private sealed class Endpoint:ControllerBase;
    [Fact]
    public async Task ResourceQuotaAndHttpReceiptCommitReplayAndRollbackTogether()
    {
        // Never infer a local/default server. The runner must name a disposable server.
        await using var owned=APsiC.Licensing.Tests.OwnedWindowsPostgres.FromEnvironment();
        if(owned is not null) await owned.Start();
        var configured=owned?.Connection ?? Environment.GetEnvironmentVariable("APSIC_TEST_POSTGRES_CONNECTION")
            ??throw new InvalidOperationException("DISPOSABLE_POSTGRES_REQUIRED");
        var builder=new NpgsqlConnectionStringBuilder(configured){Pooling=false};
        var database="apsic_da_http_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(builder.ConnectionString);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE DATABASE {database}",admin))await create.ExecuteNonQueryAsync();
        builder.Database=database;
        try { await Verify(builder.ConnectionString); }
        finally { await using var drop=new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync(); }
    }
    private static async Task Verify(string connection)
    {
        var options=new DbContextOptionsBuilder<APsiOpcDaApiContext>().UseNpgsql(connection).Options;
        using(var schema=new APsiOpcDaApiContext(options))await schema.Database.EnsureCreatedAsync();
        using var authority=new TestAuthority();var id=InstallationIds.Create();var company=Guid.NewGuid();var unit=Guid.NewGuid();
        var store=new PostgresLicenseStore(connection,"apsic-modern",id);store.ProvisionForSetup(true);
        var licensing=new LocalLicenseService(store,new LicenseValidator(authority.Trust),new Identity(TestAuthority.Context.Installation with{InstallationId=id}),"apsic-modern",company,new Clock());
        var payload=TestAuthority.Payload();payload["installationId"]=id;payload["customer"]!["empresaId"]=company;
        payload["entitlements"]!["communication.opcda"]=System.Text.Json.Nodes.JsonNode.Parse("""{"enabled":true,"scope":"installation","limit":{"kind":"finite","value":1}}""");
        payload["entitlements"]!["acquisition.tags"]=System.Text.Json.Nodes.JsonNode.Parse("""{"enabled":true,"scope":"installation","limit":{"kind":"finite","value":1}}""");
        Assert.True(licensing.Import(authority.Sign(payload),0).Succeeded);
        string state;
        using(var db=new NpgsqlConnection(connection))
        {
            db.Open();using var tx=db.BeginTransaction();
            using var mapping=new NpgsqlCommand("INSERT INTO apsic_licensing.quota_unit_mapping VALUES ('apsic-modern',$1,'opcda',$2,$3)",db,tx);
            mapping.Parameters.AddWithValue(id);mapping.Parameters.AddWithValue(unit);mapping.Parameters.AddWithValue(company);mapping.ExecuteNonQuery();
            state=Stage2Upgrade.Fingerprint(db,tx,id);tx.Commit();
        }
        PostgresQuotaBaseline.Initialize(connection,"apsic-modern",id,new HashSet<string>{"opcda"},[PostgresQuotaSources.OpcDaV1(company,id)],
            new(state,"TEST-ONLY","Reviewed disposable DA baseline","One PostgreSQL resource connection and transaction"));
        var authorization=new PostgresAuthorizationStore(connection,id,company);authorization.ProvisionForSetup(true);
        var actor=Guid.NewGuid();authorization.InitializeForCutover(actor,[new("Communication.Create",unit,null,false),new("Communication.Delete",unit,null,false)],"TEST-ONLY","Explicit unit scope");
        var session=authorization.CreateSession(actor);var runtime=new Runtime(connection,id,company,store,licensing);
        var principal=new Mock<IUserContextService>();principal.Setup(p=>p.GetEmpresaId()).Returns(company);
        var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier,actor.ToString()),new("SessionId",session.SessionId.ToString()),new("AuthorizationVersion","1"),new("IdEmpresa",company.ToString())],"test"))};
        var endpoint=new Endpoint{ControllerContext=new(){HttpContext=http,RouteData=new Microsoft.AspNetCore.Routing.RouteData(),ActionDescriptor=new ControllerActionDescriptor{ControllerName="Tag",ActionName="Create"}}};
        http.Request.Method="POST";http.Items[CanonicalActionFilter.RequiredAction]="Communication.Create";
        var key=Guid.NewGuid();http.Request.Headers["Idempotency-Key"]=key.ToString();var tagId=Guid.NewGuid();var calls=0;
        async Task<ContentResult> Create(Guid resourceId)
        {
            await using var context=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime));
            return Assert.IsType<ContentResult>(await new QuotaHttpMutation(context,runtime,principal.Object).ExecuteAsync(endpoint,new{resourceId,unit},async()=>{
                calls++;context.Tag.Add(new Tag{Id=resourceId,ModuloId=unit,Nome="TEST-ONLY",Origem="OPCDA"});await context.SaveChangesAsync();
                return new QuotaHttpResponse(201,"{\"saved\":true}",unit);
            },typeof(Tag)));
        }
        var first=await Create(tagId);var replay=await Create(tagId);
        Assert.Equal(first.Content,replay.Content);Assert.Equal(1,calls);
        await Assert.ThrowsAsync<QuotaMutationException>(()=>Create(Guid.NewGuid())); // Same key, different payload.
        http.Request.Headers["Idempotency-Key"]=Guid.NewGuid().ToString();
        Assert.Equal("QUOTA_EXCEEDED",(await Assert.ThrowsAsync<QuotaMutationException>(()=>Create(Guid.NewGuid()))).Code);
        using(var check=new APsiOpcDaApiContext(options))Assert.Equal(1,await check.Tag.CountAsync());
        http.Request.Method="DELETE";http.Items[CanonicalActionFilter.RequiredAction]="Communication.Delete";
        endpoint.ControllerContext.ActionDescriptor.ActionName="Delete";http.Request.Headers["Idempotency-Key"]=Guid.NewGuid().ToString();
        var deletes=0;
        async Task<ContentResult> Delete()
        {
            await using var context=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime));
            return Assert.IsType<ContentResult>(await new QuotaHttpMutation(context,runtime,principal.Object).ExecuteAsync(endpoint,new{tagId},async()=>{
                deletes++;var tag=await context.Tag.SingleAsync(t=>t.Id==tagId);context.Tag.Remove(tag);await context.SaveChangesAsync();return new QuotaHttpResponse(204,null,unit);
            },typeof(Tag)));
        }
        Assert.Equal(204,(await Delete()).StatusCode);Assert.Equal(204,(await Delete()).StatusCode);Assert.Equal(1,deletes);
        var deleteOperation=http.Request.Headers["Idempotency-Key"].ToString();
        http.Items[typeof(PrincipalAccess)]=session.Principal;
        await VerifyGroupBatch(options,runtime,http,unit);
        http.Request.Method="DELETE";http.Items[CanonicalActionFilter.RequiredAction]="Communication.Delete";http.Request.Headers["Idempotency-Key"]=deleteOperation;
        authorization.RevokeSession(session.SessionId,actor,1);
        await Assert.ThrowsAsync<QuotaMutationException>(()=>Delete());
        using(var check=new APsiOpcDaApiContext(options))Assert.Empty(await check.Tag.ToListAsync());
    }
    private static async Task VerifyGroupBatch(DbContextOptions<APsiOpcDaApiContext> options,Runtime runtime,DefaultHttpContext http,Guid unit)
    {
        var serverId=Guid.NewGuid();var groupId=Guid.NewGuid();
        await using(var db=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime)))
        {
            var server=(OpcServer)Activator.CreateInstance(typeof(OpcServer),true)!;
            server.Id=serverId;server.ModuloId=unit;server.Tipo=APsiOpcDaApi.Domain.Enum.TipoOpcServer.Da;server.Nome="TEST-ONLY";
            db.OpcServers.Add(server);await db.SaveChangesAsync();
            var group=(OpcGroup)Activator.CreateInstance(typeof(OpcGroup),true)!;
            group.Id=groupId;group.ServerId=serverId;group.Name="TEST-ONLY";db.OpcGroups.Add(group);await db.SaveChangesAsync();
        }
        VerifyOperationAuthorizationBeforeLicensing(options,runtime,serverId,unit);
        http.Request.Method="POST";http.Items[CanonicalActionFilter.RequiredAction]="Communication.Create";
        async Task<IActionResult> Add(int count)
        {
            await using var db=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime));
            var scope=new CanonicalResourceUserContextService(new HttpContextAccessor{HttpContext=http},db,runtime);
            var mapper=new AutoMapper.MapperConfiguration(c=>{APsiC.Security.MappingSafety.Apply(c);c.AddProfile<APsiOpcDaApi.Application.Mappings.MappingProfile>();}).CreateMapper();
            var tagRepository=new APsiOpcDaApi.Infrastructure.Repositories.TagRepository(db);
            var tags=new APsiOpcDaApi.Application.Services.TagService(tagRepository,mapper,scope,tagRepository);
            var nodes=new APsiOpcDaApi.Application.Services.OpcNodeService(new APsiOpcDaApi.Infrastructure.Repositories.OpcNodeRepository(db),mapper,scope);
            using var transaction=new APsiOpcDaApi.Infrastructure.Repositories.UnitOfWork(db);
            var groups=new APsiOpcDaApi.Application.Services.OpcGroupService(new APsiOpcDaApi.Infrastructure.Repositories.OpcGroupRepository(db),mapper,scope,nodes,tags,transaction);
            var servers=new APsiOpcDaApi.Application.Services.OpcServerService(new APsiOpcDaApi.Infrastructure.Repositories.OpcServerRepository(db),mapper,scope,new APsiOpcDaApi.Infrastructure.Repositories.OpcServerRepository(db));
            var controller=new APsiOpcDaApi.API.Controllers.OpcGroupController(groups,servers,new QuotaHttpMutation(db,runtime,scope)){
                ControllerContext=new(){HttpContext=http,RouteData=new Microsoft.AspNetCore.Routing.RouteData(),ActionDescriptor=new ControllerActionDescriptor{ControllerName="OpcGroup",ActionName="AddTagsToGroup"}}};
            var request=new APsiOpcDaApi.API.Controllers.AddTagsWithNodesToGroupRequest{Tags=Enumerable.Range(0,count)
                .Select(n=>new APsiOpcDaApi.Application.DTOs.TagDTO{Nome="TEST-ONLY batch "+n,ModuloId=unit,Origem="OPCDA",NodeIdOpc="TEST-ONLY.item."+n}).ToList()};
            return await controller.AddTagsToGroup(groupId,request);
        }
        http.Request.Headers["Idempotency-Key"]=Guid.NewGuid().ToString();
        Assert.Equal("QUOTA_EXCEEDED",(await Assert.ThrowsAsync<QuotaMutationException>(()=>Add(2))).Code);
        using(var db=new APsiOpcDaApiContext(options))
        {Assert.Empty(await db.Tag.ToListAsync());Assert.Empty(await db.OpcNodes.ToListAsync());}
        http.Request.Headers["Idempotency-Key"]=Guid.NewGuid().ToString();
        var first=Assert.IsType<ContentResult>(await Add(1));var replay=Assert.IsType<ContentResult>(await Add(1));
        Assert.Equal(first.Content,replay.Content);
        await using(var db=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime)))
        {
            Assert.Equal(1,await db.Tag.CountAsync());Assert.Equal(1,await db.OpcNodes.CountAsync());
            db.Tag.RemoveRange(await db.Tag.ToListAsync());await db.SaveChangesAsync();
        }
        await VerifyDurableWriteGate(options,runtime,serverId,unit);
    }
    private static async Task VerifyDurableWriteGate(DbContextOptions<APsiOpcDaApiContext> options,Runtime runtime,Guid server,Guid unit)
    {
        var auth=runtime.OpenAuthorization();var principal=Guid.NewGuid();
        auth.InitializeForCutover(principal,[new("Communication.Write",unit,null,false),new("Communication.Connect",unit,null,false)],"TEST-ONLY","Explicit write scope");
        var session=auth.CreateSession(principal);
        var http=new HttpContextAccessor{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([
            new(ClaimTypes.NameIdentifier,principal.ToString()),new("SessionId",session.SessionId.ToString()),new("AuthorizationVersion","1")],"TEST-ONLY"))}};
        var gate=new OperationAdmission(options,runtime,http);
        var operation=new APsiC.Operations.ResourceOperation("OpcServer",server,APsiC.Operations.OperationKind.OperationalWrite,"Communication.Write","opc.write:TEST-ONLY.item",ItemIds:new[]{"TEST-ONLY.item"},Value:0.5);
        // Configured item identity must be persisted; no transport is invoked.
        using(var db=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime)))
        {
            var group=await db.OpcGroups.SingleAsync(g=>g.ServerId==server);
            db.Tag.Add(new Tag{Id=Guid.NewGuid(),ModuloId=unit,GroupId=group.Id,Nome="TEST-ONLY write",NodeIdOpc="TEST-ONLY.item",Origem="OPCDA"});await db.SaveChangesAsync();
        }
        var resource=gate.Describe(operation);
        using(var db=new APsiOpcDaApiContext(options))
        {
            var connection=db.Database.GetDbConnection().ConnectionString;
            PostgresOperationalStore.ProvisionForSetup(connection,true);
            PostgresOperationalStore.RegisterQualifiedProfilesForSetup(connection,runtime.OpenQuota((NpgsqlConnection)db.Database.GetDbConnection()).InstallationId,runtime.EmpresaId,
                [new QualifiedOperationalProfile(Guid.NewGuid(),1,resource,"TEST-ONLY write qualification",new string('d',64),principal,true,
                    ["opc.connect","opc.write:TEST-ONLY.item"],[],null,0,new Dictionary<string,OperationalValueBounds>{{"opc.write:TEST-ONLY.item",new(-1,1)}})],"TEST-ONLY","Disposable write gate");
        }
        gate.Demand(operation with{Operation=APsiC.Operations.OperationKind.Connect,Permission="Communication.Connect",CommandKey="opc.connect",Value=null});
        var sends=0;Task<bool> Send(){sends++;return Task.FromResult(true);}
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED",(await Assert.ThrowsAsync<APsiC.Operations.OperationAdmissionDeniedException>(()=>gate.ExecuteAsync(operation,Send))).Code);
        http.HttpContext.Request.Headers["Idempotency-Key"]=Guid.NewGuid().ToString();
        Assert.True(await gate.ExecuteAsync(operation,Send));Assert.True(await gate.ExecuteAsync(operation,Send));Assert.Equal(1,sends);
        Assert.Equal("IDEMPOTENCY_CONFLICT",(await Assert.ThrowsAsync<APsiC.Operations.OperationAdmissionDeniedException>(()=>gate.ExecuteAsync(operation with{Value=-0.5},Send))).Code);Assert.Equal(1,sends);
        using(var db=new APsiOpcDaApiContext(options,new QuotaSaveBoundary(runtime))){db.Tag.RemoveRange(await db.Tag.ToListAsync());await db.SaveChangesAsync();}
    }
    private static void VerifyOperationAuthorizationBeforeLicensing(DbContextOptions<APsiOpcDaApiContext> options,Runtime runtime,Guid server,Guid unit)
    {
        var auth=runtime.OpenAuthorization();
        foreach(var scenario in new[]{"foreign","missing","revoked","permission","anonymous"})
        {
            var actor=Guid.NewGuid();
            auth.InitializeForCutover(actor,[new(scenario=="permission" ? "Licensing.View" : "Communication.View",scenario=="foreign" ? Guid.NewGuid() : unit,null,false)],"TEST-ONLY","Explicit DA scope test");
            var session=auth.CreateSession(actor);
            if(scenario=="revoked")auth.RevokeSession(session.SessionId,actor,1);
            var http=new HttpContextAccessor{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([
                new(ClaimTypes.NameIdentifier,actor.ToString()),new("SessionId",session.SessionId.ToString()),new("AuthorizationVersion","1")],scenario=="anonymous" ? null : "TEST-ONLY"))}};
            var strict=new Mock<ILocalLicensingRuntime>(MockBehavior.Strict);strict.SetupGet(r=>r.EmpresaId).Returns(runtime.EmpresaId);strict.Setup(r=>r.OpenAuthorization()).Returns(auth);
            var gate=new OperationAdmission(options,strict.Object,http);
            var request=new APsiC.Operations.ResourceOperation("OpcServer",scenario=="missing" ? Guid.NewGuid() : server,APsiC.Operations.OperationKind.Connect,"Communication.View","opc.connect");
            var expected=scenario switch{"anonymous"=>"CANONICAL_SESSION_REQUIRED","revoked"=>"AUTH_VERSION_STALE","permission"=>"PERMISSION_DENIED",_=>"RESOURCE_OWNERSHIP_UNRESOLVED"};
            Assert.Equal(expected,Assert.Throws<APsiC.Operations.OperationAdmissionDeniedException>(()=>gate.Demand(request)).Code);
            strict.Verify(r=>r.OpenQuota(It.IsAny<NpgsqlConnection>()),Times.Never);
            strict.Verify(r=>r.OpenOperational(),Times.Never);
        }
    }

}
