using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABCRetail.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public string Status { get; set; } = "Pending"; // matches the UI dropdown

        public List<SelectListItem> Customers { get; set; } = new();
        public List<SelectListItem> Products { get; set; } = new();
    }
}
