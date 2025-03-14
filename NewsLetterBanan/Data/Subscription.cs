using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Data
{
    public class Subscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Ensures auto-increment
        public int Id { get; set; }


        public int SubscriptionTypeId { get; set; }
        public SubscriptionType SubscriptionType { get; set; } // Navigation property (relation TO ONE)

 

        public required DateTime Created { get; set; }
        public required double Price { get; set; }
        

        public required DateTime Expires { get; set; }

        [Required]
        public required string UserId { get; set; } // FK to User 
        public virtual User User { get; set; } // Navigation property (relation TO ONE)

        public  bool PaymentComplete { get; set; } = false;
       
    }
}
