using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Domain.Entities
{
public class OpcGroup : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid ServerId { get; set; }
    public virtual OpcServer? Server { get; set; }

    // Configurações da Subscription
    public int UpdateRate { get; set; } = 1000; // PublishingInterval (ms)
    public int KeepAliveCount { get; set; } = 10;
    public int LifetimeCount { get; set; } = 100;
    public int MaxNotificationsPerPublish { get; set; } = 1000;
    public byte Priority { get; set; } = 100;

    public double Deadband { get; set; } = 0.1;
    public int HistorianIntervalSeconds { get; set; } = 30;
    public int AcquisitionMode { get; set; } = 1; // 1=Subscribe, 2=Polling
    public bool IsActive { get; set; } = false;

    // Relacionamento com tags
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    protected OpcGroup() { }
}

}

