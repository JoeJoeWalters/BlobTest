using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class PurchaseResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool Authorised { get; set; } = false;
        public string ChallengeMethod { get; set; } = "OOB";
    }
}
