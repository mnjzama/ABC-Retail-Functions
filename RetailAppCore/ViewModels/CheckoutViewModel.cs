using RetailAppCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailAppCore.ViewModels
{
    public class CheckoutViewModel
    {
        public string CustomerId { get; set; }
        public List<CartItem> CartItems { get; set; } = new();
        public double Total => CartItems.Sum(c => c.LineTotal);
        public string CustomerName { get; set; }

    }
}