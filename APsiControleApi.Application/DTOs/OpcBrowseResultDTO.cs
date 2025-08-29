namespace APsiControleApi.Application.DTOs
{
    public class OpcBrowseResultDTO
    {
        public IEnumerable<OpcNodeBrowseDTO> Nodes { get; set; } = new List<OpcNodeBrowseDTO>();
        public IEnumerable<OpcTagDTO> Tags { get; set; } = new List<OpcTagDTO>();
    }
}
