namespace APsiOpcDaApi.Application.DTOs
{
    public class OpcDaWriteRequestDTO
    {
        public string ItemId { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
