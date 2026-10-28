using System.Text;
using System.Text.Json;
using APsiC.OpcDa.PrivateIpc;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.Extensions.Logging.Abstractions;

namespace APsiOpcDaApi.Tests.Services;

// Portable framing/peer-verification boundaries only. No Windows SID/ACL/COM call.
public sealed class PrivatePipeProtocolTests
{
    private static readonly Guid RequestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string Nonce = new('a', 64);
    private static OpcServerDTO Server() => new() { Id = Guid.NewGuid(), Host = "TEST-ONLY-host", ProgId = "TEST-ONLY-prog", Tipo = TipoOpcServer.Da };
    private static async Task<byte[]> Frame(string text)
    {
        using var output = new MemoryStream(); await PrivatePipeProtocol.WriteAsync(output, text, default); return output.ToArray();
    }
    [Fact] public async Task FramingUsesBoundedBigEndianUtf8AndHandlesPartialReads()
    {
        const string text = "{\"value\":\"avaliação\"}";
        var bytes = await Frame(text);
        Assert.Equal(Encoding.UTF8.GetByteCount(text), bytes[3]); Assert.Equal(new byte[3], bytes.Take(3));
        using var fragmented = new Duplex(bytes, 1);
        Assert.Equal(text, await PrivatePipeProtocol.ReadAsync(fragmented, default));
    }
    [Theory] [InlineData(0)] [InlineData(262145)] [InlineData(-1)]
    public async Task InvalidFrameLengthIsRejectedBeforePayloadAllocation(int length)
    {
        var header = new[] { (byte)(length >> 24), (byte)(length >> 16), (byte)(length >> 8), (byte)length };
        using var stream = new MemoryStream(header);
        await Assert.ThrowsAsync<InvalidDataException>(() => PrivatePipeProtocol.ReadAsync(stream, default));
    }
    [Fact] public async Task TruncationInvalidUtf8BomAndOversizeWritesAreRejected()
    {
        using var truncated = new MemoryStream([0,0,0,2,123]);
        await Assert.ThrowsAsync<EndOfStreamException>(() => PrivatePipeProtocol.ReadAsync(truncated, default));
        using var invalid = new MemoryStream([0,0,0,2,0xc0,0x80]);
        await Assert.ThrowsAsync<DecoderFallbackException>(() => PrivatePipeProtocol.ReadAsync(invalid, default));
        using var bom = new MemoryStream(await Frame("\ufeff{}"));
        await Assert.ThrowsAsync<InvalidDataException>(() => PrivatePipeProtocol.ReadAsync(bom, default));
        using var oversized = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => PrivatePipeProtocol.WriteAsync(oversized, new string('x', PrivatePipeProtocol.MaximumFrameBytes + 1), default));
        Assert.Equal(0, oversized.Length);
    }
    private static string Result(Guid? request = null, string? nonce = null, string item = "tag.a", string timestamp = "2026-10-21T12:00:00Z") => JsonSerializer.Serialize(new
    {
        version = 1, requestId = request ?? RequestId, nonce = nonce ?? Nonce,
        result = new { Items = new[] { new { ItemId = item, Value = "12.5", Quality = "Good", Timestamp = timestamp } }, Errors = Array.Empty<string>() }
    });
    private static async Task<Duplex> Conversation(string response, string? challenge = null) => new((await Frame(challenge ?? JsonSerializer.Serialize(new { version = 1, nonce = Nonce }))).Concat(await Frame(response)).ToArray(), 3);
    [Fact] public async Task PeerIdentityIsVerifiedBeforeReadingOrSendingConfiguredTargets()
    {
        using var pipe = await Conversation(Result());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => NamedPipeOpcDaTransport.ExchangeAsync(pipe, Server(), ["tag.a"],
            () => throw new UnauthorizedAccessException("TEST-ONLY counterfeit server"), RequestId, default));
        Assert.Equal(0, pipe.Reads); Assert.Empty(pipe.Written.ToArray());
    }
    [Fact] public async Task ValidResponseBindsRequestNonceServerAndRequestedItems()
    {
        using var pipe = await Conversation(Result()); var server = Server(); var verified = false;
        var values = await NamedPipeOpcDaTransport.ExchangeAsync(pipe, server, ["tag.a"], () => verified = true, RequestId, default);
        Assert.True(verified); Assert.Single(values); Assert.Equal("tag.a", values[0].NodeId); Assert.Equal("12.5", values[0].ValorAtual);
        pipe.Written.Position = 0;
        using var sent = JsonDocument.Parse(await PrivatePipeProtocol.ReadAsync(pipe.Written, default));
        Assert.Equal(server.Id, sent.RootElement.GetProperty("serverId").GetGuid());
        Assert.Equal(RequestId, sent.RootElement.GetProperty("requestId").GetGuid());
        Assert.Equal(Nonce, sent.RootElement.GetProperty("nonce").GetString());
    }
    [Theory] [InlineData("request")] [InlineData("nonce")] [InlineData("item")] [InlineData("timestamp")]
    public async Task ResponsesCannotSubstituteAnotherRequestOrItem(string mismatch)
    {
        using var pipe = await Conversation(Result(mismatch == "request" ? Guid.NewGuid() : null,
            mismatch == "nonce" ? new string('b',64) : null, mismatch == "item" ? "foreign.tag" : "tag.a",
            mismatch == "timestamp" ? "invalid" : "2026-10-21T12:00:00Z"));
        await Assert.ThrowsAsync<InvalidDataException>(() => NamedPipeOpcDaTransport.ExchangeAsync(pipe, Server(), ["tag.a"], () => {}, RequestId, default));
    }
    [Theory]
    [InlineData("{\"version\":1,\"version\":1,\"nonce\":\"x\"}")]
    [InlineData("{\"version\":2,\"nonce\":\"x\"}")]
    [InlineData("{\"version\":1,\"nonce\":\"x\",\"host\":\"desktop\"}")]
    public async Task InvalidChallengeCannotCauseTargetDisclosure(string challenge)
    {
        using var pipe = await Conversation(Result(), challenge);
        await Assert.ThrowsAsync<InvalidDataException>(() => NamedPipeOpcDaTransport.ExchangeAsync(pipe, Server(), ["tag.a"], () => {}, RequestId, default));
        Assert.Empty(pipe.Written.ToArray());
    }
    [Fact] public void LegacyHttpConfigurationCannotFallBackToHttpOrDirectCom()
    {
        var priorUrl = Environment.GetEnvironmentVariable("OPC_DA_BRIDGE_URL"); var priorPipe = Environment.GetEnvironmentVariable("OPC_DA_BRIDGE_PIPE");
        try
        {
            Environment.SetEnvironmentVariable("OPC_DA_BRIDGE_URL", "http://127.0.0.1:1"); Environment.SetEnvironmentVariable("OPC_DA_BRIDGE_PIPE", null);
            var service = new OpcDaClientService(NullLogger<OpcDaClientService>.Instance, isWindows: true,admission:Moq.Mock.Of<APsiC.Operations.IOperationAdmission>());
            var error = Assert.Throws<InvalidOperationException>((Action)(() => { _ = service.ReadValuesAsync(Server(), ["tag.a"]); }));
            Assert.Equal("BRIDGE_PRIVATE_IPC_MIGRATION_REQUIRED", error.Message);
        }
        finally { Environment.SetEnvironmentVariable("OPC_DA_BRIDGE_URL", priorUrl); Environment.SetEnvironmentVariable("OPC_DA_BRIDGE_PIPE", priorPipe); }
    }
    private sealed class Duplex(byte[] input, int fragment) : Stream
    {
        private readonly MemoryStream source = new(input);
        public MemoryStream Written { get; } = new(); public int Reads { get; private set; }
        public override bool CanRead => true; public override bool CanWrite => true; public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) { Reads++; return source.Read(buffer, offset, Math.Min(count, fragment)); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { Reads++; return source.ReadAsync(buffer, offset, Math.Min(count, fragment), cancellationToken); }
        public override void Write(byte[] buffer, int offset, int count) => Written.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Written.WriteAsync(buffer, offset, count, cancellationToken);
        public override void Flush() {} public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) { source.Dispose(); Written.Dispose(); } base.Dispose(disposing); }
    }
}
