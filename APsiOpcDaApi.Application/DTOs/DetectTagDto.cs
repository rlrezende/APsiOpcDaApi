using System;

namespace APsiOpcDaApi.Application.DTOs
{
    public class DetectTagDto
    {
        public Guid Id { get; set; }
        public Guid ModuloId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string InstrumentClass { get; set; } = string.Empty;
        public string Isa { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
    }
}

