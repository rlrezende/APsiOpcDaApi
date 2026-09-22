using APsiOpcDaApi.API.Services;

namespace APsiOpcDaApi.Tests.Services;

public sealed class OpcDaServerEnumeratorCharacterizationTests
{
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
