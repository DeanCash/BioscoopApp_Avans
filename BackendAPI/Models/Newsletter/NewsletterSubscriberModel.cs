using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Newsletter
{
    public class NewsletterSubscriberModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = null!;

        public DateTimeOffset SubscribedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
