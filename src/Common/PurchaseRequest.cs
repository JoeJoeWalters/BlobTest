using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class PurchaseRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AccountNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.0m;
        public string Merchant { get; set; } = string.Empty;
    }
}
