namespace APsiControleApi.Application.DTOs
{
    public class OpcNodeBrowseDTO
    {
        public string NodeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BrowseName { get; set; } = string.Empty;
        public string NodeClass { get; set; } = string.Empty;
        public bool HasChildren { get; set; }
        public string? DataType { get; set; }
        public string? AccessLevel { get; set; }
        public string Icon { get; set; } = "folder";
        public string? Description { get; set; }
    }
}
