using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailSenderSubscriptionExpiryQueueListener.Models
{
    public class UserSubscription
    {
        public string UserName { get; set; }
        public string Email { get; set; }

        // ✅ Prevents null issues by initializing the list
        public List<SubscriptionDetail> ExpiringSubscriptions { get; set; } = new List<SubscriptionDetail>();
    }
}
