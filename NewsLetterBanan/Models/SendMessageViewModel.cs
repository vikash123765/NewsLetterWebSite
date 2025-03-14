using System.ComponentModel.DataAnnotations;

public class SendMessageViewModel
{
    [Required]
    public string ReceiverId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Content { get; set; }
}
