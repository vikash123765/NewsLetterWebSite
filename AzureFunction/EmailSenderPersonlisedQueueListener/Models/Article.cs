using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailSenderPersonlisedQueueListener.Models
{
    public class Article
    {
        public string Headline { get; set; }
        public string ContentSummary { get; set; }
        public DateTime DateStamp { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Tags { get; set; }
        public string SourceURL { get; set; }
        public string Author { get; set; }
    }
}