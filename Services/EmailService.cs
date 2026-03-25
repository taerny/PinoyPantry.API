using System.Net;
using System.Net.Mail;
using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendContactEmailAsync(ContactRequestDto dto)
    {
        var smtpHost = _config["Email:SmtpHost"]!;
        var smtpPort = int.Parse(_config["Email:SmtpPort"]!);
        var smtpUser = _config["Email:SmtpUser"]!;
        var smtpPass = _config["Email:SmtpPass"]!;
        var toAddress = _config["Email:ToAddress"]!;

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, "PinoyPantry Website"),
            Subject = $"[PinoyPantry Contact] {dto.Subject} — from {dto.Name}",
            Body = $"""
                New message from your PinoyPantry contact form:

                Name:    {dto.Name}
                Email:   {dto.Email}
                Subject: {dto.Subject}

                Message:
                {dto.Message}

                ---
                Reply directly to this email to respond to {dto.Name}.
                """,
            IsBodyHtml = false
        };

        mail.To.Add(toAddress);

        // Sets Reply-To so clicking Reply in your inbox goes back to the customer
        mail.ReplyToList.Add(new MailAddress(dto.Email, dto.Name));

        await client.SendMailAsync(mail);
    }
}
