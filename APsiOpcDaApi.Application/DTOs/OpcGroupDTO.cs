using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
public class OpcGroupDTO : IIdentifiable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;

    public int UpdateRate { get; set; }
    public int KeepAliveCount { get; set; }
    public int LifetimeCount { get; set; }
    public int MaxNotificationsPerPublish { get; set; }
    public byte Priority { get; set; }

    public double Deadband { get; set; }
    public int HistorianIntervalSeconds { get; set; } = 30;
    public int AcquisitionMode { get; set; } = 1;
    public bool IsActive { get; set; }
    public int TagCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdate { get; set; }

    public List<Guid> TagIds { get; set; } = new List<Guid>();
}

}

