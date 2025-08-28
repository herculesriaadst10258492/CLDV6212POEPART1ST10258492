using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetail.Models
{
    public class Customer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // your existing properties…
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // If these exist on your class, make sure EF ignores them:
        [NotMapped] public Azure.ETag ETag { get; set; }
        [NotMapped] public string? PartitionKey { get; set; }
        [NotMapped] public string? RowKey { get; set; }
        [NotMapped] public DateTimeOffset? Timestamp { get; set; }
    }
}
