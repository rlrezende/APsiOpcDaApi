using APsiOpcDaApi.API.Services;

namespace APsiOpcDaApi.Tests.Services;

public sealed class OpcDaServerEnumeratorCharacterizationTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void EnumerationWithoutQualifiedTargetStopsBeforeAnyComAccess(bool configured)
    {
        var admission=new Moq.Mock<APsiC.Operations.IEndpointOperationAdmission>(Moq.MockBehavior.Strict);
        admission.Setup(a=>a.DemandEndpoint("opcda","TEST-ONLY.invalid","Communication.View","opc.discover"))
            .Throws(new APsiC.Operations.OperationAdmissionDeniedException("OPERATIONAL_QUALIFICATION_REQUIRED"));
        var service=new OpcDaServerEnumerator(configured?admission.Object:null);
        var error=Assert.Throws<APsiC.Operations.OperationAdmissionDeniedException>(()=>service.Enumerate("TEST-ONLY.invalid").ToArray());
        Assert.Equal(configured?"OPERATIONAL_QUALIFICATION_REQUIRED":"OPERATION_ADMISSION_REQUIRED",error.Code);
    }

    [Fact]
    public void MapServerMetadata_UrlCompleta_ExtraiContratoAtual()
    {
        var result = OpcDaServerEnumerator.MapServerMetadata(
            "Servidor Fabrica",
            new OpcDaServerEnumerator.UrlMetadata(
                "opcda",
                "servidor-alvo",
                "Fabrica.Servidor/{11111111-2222-3333-4444-555555555555}"));

        Assert.Equal("Servidor Fabrica", result.Nome);
        Assert.Equal(
            "opcda://servidor-alvo/Fabrica.Servidor/{11111111-2222-3333-4444-555555555555}",
            result.Endpoint);
        Assert.Equal("Fabrica.Servidor", result.ProgId);
        Assert.Equal("11111111-2222-3333-4444-555555555555", result.ClsId);
        Assert.Equal("Servidor Fabrica", result.Descricao);
    }

    [Fact]
    public void MapServerMetadata_NomeAusente_UsaProgIdSemPreencherDescricao()
    {
        var result = OpcDaServerEnumerator.MapServerMetadata(
            null,
            new OpcDaServerEnumerator.UrlMetadata(
                "opcda",
                "servidor-alvo",
                "/Fabrica.Servidor/"));

        Assert.Equal("Fabrica.Servidor", result.Nome);
        Assert.Equal("opcda://servidor-alvo//Fabrica.Servidor", result.Endpoint);
        Assert.Equal("Fabrica.Servidor", result.ProgId);
        Assert.Null(result.ClsId);
        Assert.Null(result.Descricao);
    }

    [Fact]
    public void MapServerMetadata_CaminhoSomenteClsId_UsaEndpointComoProgId()
    {
        var result = OpcDaServerEnumerator.MapServerMetadata(
            null,
            new OpcDaServerEnumerator.UrlMetadata(
                "opcda",
                "servidor-alvo",
                "{11111111-2222-3333-4444-555555555555}"));

        Assert.Equal("Servidor OPC DA", result.Nome);
        Assert.Equal(
            "opcda://servidor-alvo/{11111111-2222-3333-4444-555555555555}",
            result.Endpoint);
        Assert.Equal(result.Endpoint, result.ProgId);
        Assert.Equal("11111111-2222-3333-4444-555555555555", result.ClsId);
        Assert.Null(result.Descricao);
    }

    [Fact]
    public void MapServerMetadata_UrlAusente_PreservaFallbacksAtuais()
    {
        var result = OpcDaServerEnumerator.MapServerMetadata(null, null);

        Assert.Equal("Servidor OPC DA", result.Nome);
        Assert.Equal(string.Empty, result.Endpoint);
        Assert.Equal(string.Empty, result.ProgId);
        Assert.Null(result.ClsId);
        Assert.Null(result.Descricao);
    }
}
