namespace APsiOpcDaApi.Application.DTOs
{
    public class OpcTagDTO
    {
        public string NodeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BrowseName { get; set; } = string.Empty;
        public string NodeClass { get; set; } = string.Empty;
        public string? ValorAtual { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }
        public string AccessLevel { get; set; } = string.Empty;
        public string Icon { get; set; } = "tag";
        public bool HasChildren { get; set; }
        public string? Description { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string? Unit { get; set; }
    }
}

