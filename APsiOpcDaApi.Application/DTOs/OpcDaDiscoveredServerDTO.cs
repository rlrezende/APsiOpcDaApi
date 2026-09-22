namespace APsiOpcDaApi.Application.DTOs
{
    public sealed class OpcDaDiscoveredServerDTO
    {
        public string? Nome { get; init; }
        public string Endpoint { get; init; } = string.Empty;
        public string? ProgId { get; init; }
        public string? ClsId { get; init; }
        public string? Descricao { get; init; }
    }
}
