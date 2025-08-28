// File: Models/Product.cs
using System;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Product : ITableEntity
    {
        // Azure Table keys
        public string PartitionKey { get; set; } = "PRODUCTS";   // your historic data used this
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        // Domain props
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; } = "";

        // ITableEntity
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
