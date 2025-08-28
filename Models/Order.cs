using System;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Order : ITableEntity
    {
        [Required] public string PartitionKey { get; set; } = "ORDERS";
        [Required] public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Relations by RowKey
        [Required] public string CustomerRowKey { get; set; } = string.Empty;
        [Required] public string ProductRowKey { get; set; } = string.Empty;

        // Display snapshots (denormalised for convenience)
        public string? CustomerName { get; set; }
        public string? ProductName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required, StringLength(40)]
        public string Status { get; set; } = "Pending";

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }
    }
}
