namespace PinoyPantry.API.DTOs
{
    public class NewsletterSubscribeDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class NewsletterSubscriberResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime SubscribedAt { get; set; }
    }
}
