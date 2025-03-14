using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

namespace NewsLetterBanan.Data
{
    public class SubscriptionType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string TypeName { get; set; }
        public string Description { get; set; }

        public required double Price { get; set; }

       
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
