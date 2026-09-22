namespace APsiOpcDaApi.Tests.Smoke;

public class OpcDaSmokePreflightScriptTests
{
    private static readonly string Script = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Smoke", "opcda-smoke-preflight.ps1"));

    [Fact]
    public void Script_PreflightLocal_UsaRegistro32BitESemAtivacaoComOuRede()
    {
        Assert.Contains("RegistryView]::Registry32", Script, StringComparison.Ordinal);
        Assert.Contains("ProcessArchitecture", Script, StringComparison.Ordinal);
        Assert.Contains("ReadUInt16() -eq 0x014c", Script, StringComparison.Ordinal);

        Assert.DoesNotContain("GetTypeFromProgID", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("New-Object -ComObject", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-WebRequest", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Test-NetConnection", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Set-ItemProperty", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Remove-Item", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-Service", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-Process", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Activator", Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("regsvr32", Script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Script_EvidenciaSanitizada_NaoSerializaIdentificadoresOpc()
    {
        var evidenceStart = Script.IndexOf("$evidence = [ordered]@{", StringComparison.Ordinal);
        var evidenceEnd = Script.IndexOf("$resolvedEvidencePath", evidenceStart, StringComparison.Ordinal);

        Assert.True(evidenceStart >= 0);
        Assert.True(evidenceEnd > evidenceStart);

        var evidenceBlock = Script[evidenceStart..evidenceEnd];
        Assert.Contains("SensitiveValuesRecorded = $false", evidenceBlock, StringComparison.Ordinal);
        Assert.Contains("OpcConnectionAttempted = $false", evidenceBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("ServerProgId =", evidenceBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("BrowseItemId =", evidenceBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadItemId =", evidenceBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteItemId =", evidenceBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void Script_WriteSolicitado_ExigeTagAutorizacaoEValorOriginalCapturado()
    {
        Assert.Contains("-not [string]::IsNullOrWhiteSpace($WriteItemId)", Script, StringComparison.Ordinal);
        Assert.Contains("$WriteAuthorized -and", Script, StringComparison.Ordinal);
        Assert.Contains("$RestoreValueCaptured", Script, StringComparison.Ordinal);
        Assert.Contains("WriteSafetySatisfied = [bool]$writeSafetySatisfied", Script, StringComparison.Ordinal);
    }

    [Fact]
    public void Script_EvidenciaExistente_RecusaSobrescrita()
    {
        Assert.Contains("if ([System.IO.File]::Exists($resolvedEvidencePath))", Script, StringComparison.Ordinal);
        Assert.Contains("EvidencePath ja existe", Script, StringComparison.Ordinal);
    }
}
