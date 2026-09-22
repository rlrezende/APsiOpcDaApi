using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.Extensions.Logging.Abstractions;
using Opc;
using Opc.Da;

namespace APsiOpcDaApi.Tests.Services;

public sealed class OpcDaClientServiceCharacterizationTests
{
    private const string UnsupportedPlatformMessage = "OPC DA só é suportado em ambientes Windows.";

    [Fact]
    public void BrowseAsync_SistemaNaoWindows_LancaPlatformNotSupportedAntesDeConectar()
    {
        Assert.False(OperatingSystem.IsWindows());
        var service = CreateService();
        Assert.False(service.IsSupported);

        var exception = Assert.Throws<PlatformNotSupportedException>(
            (Action)(() => _ = service.BrowseAsync(CreateServerWithoutConnectionData(), "Channel.Device.Tag")));

        Assert.Equal(UnsupportedPlatformMessage, exception.Message);
    }

    [Fact]
    public void ReadValuesAsync_SistemaNaoWindowsComBridgeConfigurada_LancaPlatformNotSupportedAntesDaBridge()
    {
        Assert.False(OperatingSystem.IsWindows());
        var previousBridgeUrl = Environment.GetEnvironmentVariable("OPC_DA_BRIDGE_URL");
        Environment.SetEnvironmentVariable("OPC_DA_BRIDGE_URL", "uri-invalida-sem-rede");

        try
        {
            var service = CreateService();

            var exception = Assert.Throws<PlatformNotSupportedException>(
                (Action)(() => _ = service.ReadValuesAsync(CreateServerWithoutConnectionData(), ["Channel.Device.Tag"])));

            Assert.Equal(UnsupportedPlatformMessage, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPC_DA_BRIDGE_URL", previousBridgeUrl);
        }
    }

    [Fact]
    public void WriteValueAsync_SistemaNaoWindows_LancaPlatformNotSupportedAntesDeConectar()
    {
        Assert.False(OperatingSystem.IsWindows());
        var service = CreateService();

        var exception = Assert.Throws<PlatformNotSupportedException>(
            (Action)(() => _ = service.WriteValueAsync(CreateServerWithoutConnectionData(), "Channel.Device.Tag", 12.5d)));

        Assert.Equal(UnsupportedPlatformMessage, exception.Message);
    }

    [Fact]
    public void NormalizeItemIds_ColecaoNulaOuSemIdsValidos_RetornaVazio()
    {
        Assert.Empty(OpcDaClientService.NormalizeItemIds(null));
        Assert.Empty(OpcDaClientService.NormalizeItemIds([]));
        Assert.Empty(OpcDaClientService.NormalizeItemIds([null!, string.Empty, "   "]));
    }

    [Fact]
    public void NormalizeItemIds_DuplicatasPorCaixa_PreservaPrimeiraGrafiaEOrdemSemTrim()
    {
        var normalized = OpcDaClientService.NormalizeItemIds(
        [
            "Channel.Device.Tag",
            "channel.device.tag",
            " Other.Tag ",
            "other.tag",
            "OTHER.TAG",
            "Second.Tag"
        ]);

        Assert.Equal(["Channel.Device.Tag", " Other.Tag ", "other.tag", "Second.Tag"], normalized);
    }

    [Fact]
    public void MapReadResults_ResultadosValidosInvalidosENaoRetornados_PreservaMapeamentoLegado()
    {
        var timestamp = new DateTime(2026, 8, 26, 18, 19, 20, DateTimeKind.Utc);
        var results = new[]
        {
            new ItemValueResult("Double.Tag", ResultID.S_OK)
            {
                Value = 12.5d,
                Quality = new Quality(qualityBits.good),
                QualitySpecified = true,
                Timestamp = timestamp,
                TimestampSpecified = true
            },
            new ItemValueResult("Array.Tag", ResultID.S_OK)
            {
                Value = new object?[] { 1, null, "texto" }
            },
            new ItemValueResult("Null.Tag", ResultID.S_OK)
            {
                Value = null
            },
            new ItemValueResult("Failed.Tag", ResultID.E_FAIL)
        };

        var tags = OpcDaClientService.MapReadResults(
            ["Double.Tag", "Array.Tag", "Null.Tag", "Failed.Tag", "Missing.Tag"],
            results);

        Assert.Collection(
            tags,
            tag => AssertTag(tag, "Double.Tag", "12.5", typeof(double).FullName!, "good", timestamp),
            tag => AssertTag(tag, "Array.Tag", "[1, null, texto]", typeof(object[]).FullName!, "Good", null),
            tag => AssertTag(tag, "Null.Tag", null, string.Empty, "Good", null),
            tag => AssertTag(tag, "Failed.Tag", "Erro", string.Empty, ResultID.E_FAIL.ToString(), null),
            tag => AssertTag(tag, "Missing.Tag", "Sem retorno", string.Empty, "Unknown", null));
    }

    private static void AssertTag(
        APsiOpcDaApi.Application.DTOs.OpcTagDTO tag,
        string expectedId,
        string? expectedValue,
        string expectedDataType,
        string expectedQuality,
        DateTime? expectedTimestamp)
    {
        Assert.Equal(expectedId, tag.NodeId);
        Assert.Equal(expectedId, tag.DisplayName);
        Assert.Equal(expectedId, tag.BrowseName);
        Assert.Equal("Variable", tag.NodeClass);
        Assert.Equal("tag", tag.Icon);
        Assert.False(tag.HasChildren);
        Assert.Equal(expectedValue, tag.ValorAtual);
        Assert.Equal(expectedDataType, tag.DataType);
        Assert.Equal(expectedQuality, tag.Quality);
        Assert.Equal(expectedTimestamp, tag.Timestamp);
    }

    private static OpcDaClientService CreateService() =>
        new(NullLogger<OpcDaClientService>.Instance);

    private static OpcServerDTO CreateServerWithoutConnectionData() => new()
    {
        Tipo = TipoOpcServer.Da
    };
}
