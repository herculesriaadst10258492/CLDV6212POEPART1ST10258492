// File: Models/HomeViewModel.cs
using System.Collections.Generic;

namespace ABCRetail.Models
{
    public class HomeViewModel
    {
        public int CustomersCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }

        // Aliases if any code uses the singular names
        public int CustomerCount { get => CustomersCount; set => CustomersCount = value; }
        public int ProductCount { get => ProductsCount; set => ProductsCount = value; }
        public int OrderCount { get => OrdersCount; set => OrdersCount = value; }

        public IEnumerable<Product>? FeaturedProducts { get; set; }
    }
}
