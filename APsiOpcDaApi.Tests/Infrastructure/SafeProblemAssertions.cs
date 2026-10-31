using System.Text.Json;
namespace APsiOpcDaApi.Tests.Infrastructure;

internal static class SafeProblemAssertions
{
    public static async Task AssertAsync(HttpResponseMessage response, string code)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement;
        Assert.Equal((int)response.StatusCode, problem.GetProperty("status").GetInt32());
        Assert.Equal("about:blank", problem.GetProperty("type").GetString());
        Assert.Equal("Request could not be completed", problem.GetProperty("title").GetString());
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal(code, problem.GetProperty("reasonCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("decisionId").GetString()));
        foreach (var field in new[] { "message", "error", "itemId", "serverId", "unidadeId" })
            Assert.False(problem.TryGetProperty(field, out _), "Unsafe legacy error field: " + field);
        if (problem.TryGetProperty("detail", out var detail)) Assert.Equal(JsonValueKind.Null, detail.ValueKind);
    }
}
