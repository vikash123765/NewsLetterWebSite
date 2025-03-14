using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailSenderPersonlisedQueueListener.Models
{
    public class User
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool HasActiveSubscription { get; set; }
        public List<Article> ExclusiveArticles { get; set; }
        public List<Article> LatestArticles { get; set; }
        public List<Article> EditorsChoiceArticles { get; set; }
        public List<Article> MostPopularArticles { get; set; }
    }
}
