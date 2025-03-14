using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EmailSenderSubscriptionExpiryQueueListener.Models
{
    public class SubscriptionDetail
    {
        public string SubscriptionType { get; set; }

        // ✅ Ensures proper DateTime handling
        [JsonPropertyName("ExpiryDate")]
        public DateTime ExpiryDate { get; set; }
    }
}
