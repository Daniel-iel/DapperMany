using DapperMany.Attributes;

namespace DapperMany.Samples.Models;

/// <summary>
/// TenantPedido (Tenant Order) entity demonstrating composite primary key support.
/// Maps to the dbo.TenantPedidos table. Uses (TenantId, DocumentNumber) as composite key.
/// This enables multi-tenancy with per-tenant document numbering.
/// </summary>
[System.ComponentModel.DataAnnotations.Schema.Table("TenantPedidos")]
public class TenantPedido
{
    /// <summary>
    /// Tenant identifier (first part of composite key).
    /// Identifies which tenant/customer owns this order.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Key]
    public required string TenantId { get; set; }

    /// <summary>
    /// Document number (second part of composite key).
    /// Must be unique per tenant (not globally).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Key]
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// Order date.
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total value of the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Status of the order (e.g., "Pendente", "Processado", "Cancelado").
    /// </summary>
    public string Status { get; set; } = "Pendente";

    /// <summary>
    /// Timestamp when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the order was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
