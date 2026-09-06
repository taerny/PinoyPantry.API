using System.Net;
using System.Net.Mail;
using System.Text;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IBankDetailsService _bankDetailsService;

    public EmailService(IConfiguration config, IBankDetailsService bankDetailsService)
    {
        _config = config;
        _bankDetailsService = bankDetailsService;
    }

    public async Task SendContactEmailAsync(ContactRequestDto dto)
    {
        using var client = BuildSmtpClient(out var smtpUser, out var toAddress);

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

        AddRecipients(mail.To, toAddress);
        mail.ReplyToList.Add(new MailAddress(dto.Email, dto.Name));

        await client.SendMailAsync(mail);
    }

    public async Task SendOrderConfirmationEmailAsync(Order order)
    {
        using var client = BuildSmtpClient(out var smtpUser, out _);

        var feePending = order.DeliveryFee is null;
        var noticeBlocks = new StringBuilder();

        if (feePending)
        {
            noticeBlocks.Append("""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(249,168,37,0.12); border:1px solid rgba(249,168,37,0.4); border-radius:8px; margin-bottom:16px;">
                  <tr><td style="padding:14px 16px; font-size:13px; color:#5a4200;">
                    Your delivery is outside Dunedin, so the total above <strong>does not include delivery yet</strong> — we'll be in touch shortly to arrange delivery and confirm the final amount to pay.
                  </td></tr>
                </table>
                """);
        }

        noticeBlocks.Append(await BuildPaymentInstructionsBlock(order.InvoiceNumber));

        var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, "PinoyPantry"),
            Subject = $"Order confirmed — {order.InvoiceNumber}",
            Body = BuildOrderEmailHtml(
                bannerText: "🙏 Thank you for your order!",
                bannerColor: "#F9A825",
                bannerTextColor: "#3E2723",
                intro: $"Hi {order.CustomerName}, we've received your order and it's being prepared. Here's a summary for your records.",
                order: order,
                extraBlocks: noticeBlocks.ToString(),
                footer: "Maraming salamat — thank you for shopping with PinoyPantry!"
            ),
            IsBodyHtml = true
        };

        mail.To.Add(order.CustomerEmail);
        await client.SendMailAsync(mail);
    }

    public async Task SendNewOrderNotificationEmailAsync(Order order)
    {
        using var client = BuildSmtpClient(out var smtpUser, out var toAddress);

        var feePending = order.DeliveryFee is null;
        var noticeBlocks = new StringBuilder();

        if (feePending)
        {
            noticeBlocks.Append("""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(211,47,47,0.1); border:1px solid rgba(211,47,47,0.4); border-radius:8px; margin-bottom:16px;">
                  <tr><td style="padding:14px 16px; font-size:13px; color:#7f1d1d;">
                    <strong>⚠️ Action needed:</strong> this order is delivery outside Dunedin — contact the customer to arrange delivery, then set the delivery fee in the admin panel to send them the final total.
                  </td></tr>
                </table>
                """);
        }

        noticeBlocks.Append("""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(249,168,37,0.12); border:1px solid rgba(249,168,37,0.4); border-radius:8px; margin-bottom:8px;">
              <tr><td style="padding:14px 16px; font-size:13px; color:#5a4200;">
                <strong>Status: Pending</strong> — payment is by bank transfer. Once you've confirmed the transfer has come through, mark this order as <strong>Paid</strong> in the admin panel.
              </td></tr>
            </table>
            """);

        var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, "PinoyPantry Website"),
            Subject = $"New order — {order.InvoiceNumber}",
            Body = BuildOrderEmailHtml(
                bannerText: "🔔 New order received — action needed",
                bannerColor: "#D32F2F",
                bannerTextColor: "#FFFFFF",
                intro: $"""
                    <strong>Customer:</strong> {WebUtility.HtmlEncode(order.CustomerName)}<br/>
                    <strong>Email:</strong> <a href="mailto:{order.CustomerEmail}" style="color:#3E2723;">{order.CustomerEmail}</a><br/>
                    {(string.IsNullOrWhiteSpace(order.CustomerPhone) ? "" : $"""<strong>Phone:</strong> <a href="tel:{order.CustomerPhone}" style="color:#3E2723;">{order.CustomerPhone}</a><br/>""")}
                    {(string.IsNullOrWhiteSpace(order.CustomerAddress) ? "" : $"""<strong>Address:</strong> {WebUtilityEncode(order.CustomerAddress)}<br/>""")}
                    {(string.IsNullOrWhiteSpace(order.Notes) ? "" : $"""<strong>Notes:</strong> {WebUtilityEncode(order.Notes)}<br/>""")}
                    """,
                order: order,
                extraBlocks: noticeBlocks.ToString(),
                footer: "Automated notification from the PinoyPantry order system."
            ),
            IsBodyHtml = true
        };

        AddRecipients(mail.To, toAddress);
        await client.SendMailAsync(mail);
    }

    public async Task SendDeliveryFeeConfirmedEmailAsync(Order order)
    {
        using var client = BuildSmtpClient(out var smtpUser, out _);

        var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, "PinoyPantry"),
            Subject = $"Delivery confirmed — {order.InvoiceNumber}",
            Body = BuildOrderEmailHtml(
                bannerText: "🚚 Delivery confirmed — here's your final total",
                bannerColor: "#F9A825",
                bannerTextColor: "#3E2723",
                intro: $"Hi {order.CustomerName}, we've arranged your delivery and confirmed the fee. Here's your updated total.",
                order: order,
                extraBlocks: await BuildPaymentInstructionsBlock(order.InvoiceNumber, introText: "Please pay the total above by bank transfer using the details below."),
                footer: "Maraming salamat — thank you for shopping with PinoyPantry!"
            ),
            IsBodyHtml = true
        };

        mail.To.Add(order.CustomerEmail);
        await client.SendMailAsync(mail);
    }

    public async Task SendWalkInReceiptEmailAsync(Order order)
    {
        using var client = BuildSmtpClient(out var smtpUser, out _);

        var isPaid = order.Status == "Paid";
        var extraBlocks = isPaid
            ? ""
            : $"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(249,168,37,0.12); border:1px solid rgba(249,168,37,0.4); border-radius:8px; margin-bottom:8px;">
                  <tr><td style="padding:14px 16px; font-size:13px; color:#5a4200;">
                    <strong>Pay later:</strong> please settle <strong>${order.Total:F2}</strong> in-store on your next visit. Use <strong>{order.InvoiceNumber}</strong> as your reference.
                  </td></tr>
                </table>
                """;

        var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, "PinoyPantry"),
            Subject = isPaid ? $"Receipt — {order.InvoiceNumber}" : $"Receipt (payment pending) — {order.InvoiceNumber}",
            Body = BuildOrderEmailHtml(
                bannerText: isPaid ? "✅ Thank you for your purchase!" : "🙏 Thanks for shopping with us",
                bannerColor: "#F9A825",
                bannerTextColor: "#3E2723",
                intro: isPaid
                    ? $"Hi {order.CustomerName}, thank you for shopping with us in-store. Here's your receipt for your records."
                    : $"Hi {order.CustomerName}, thanks for shopping with us in-store. Here's a summary of what you picked up.",
                order: order,
                extraBlocks: extraBlocks,
                footer: "Maraming salamat — thank you for shopping with PinoyPantry!"
            ),
            IsBodyHtml = true
        };

        mail.To.Add(order.CustomerEmail);
        await client.SendMailAsync(mail);
    }

    public async Task SendWalkInOwnerNotificationEmailAsync(Order order)
    {
        using var client = BuildSmtpClient(out var smtpUser, out var toAddress);

        var isPaid = order.Status == "Paid";
        var statusBlock = isPaid
            ? """
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(46,125,50,0.1); border:1px solid rgba(46,125,50,0.4); border-radius:8px; margin-bottom:8px;">
                  <tr><td style="padding:14px 16px; font-size:13px; color:#1b5e20;">
                    <strong>Status: Paid</strong> — this sale has already been settled in-store.
                  </td></tr>
                </table>
                """
            : $"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(211,47,47,0.1); border:1px solid rgba(211,47,47,0.4); border-radius:8px; margin-bottom:8px;">
                  <tr><td style="padding:14px 16px; font-size:13px; color:#7f1d1d;">
                    <strong>Status: Pending</strong> — customer chose to pay later. Mark this order as Paid in the admin panel once they've settled ${order.Total:F2}.
                  </td></tr>
                </table>
                """;

        var intro = string.IsNullOrWhiteSpace(order.CustomerEmail)
            ? $"""<strong>Customer:</strong> {WebUtility.HtmlEncode(order.CustomerName)} <span style="color:#6D4C41;">(no email on file)</span><br/>"""
            : $"""<strong>Customer:</strong> {WebUtility.HtmlEncode(order.CustomerName)}<br/><strong>Email:</strong> <a href="mailto:{order.CustomerEmail}" style="color:#3E2723;">{order.CustomerEmail}</a><br/>""";

        if (!string.IsNullOrWhiteSpace(order.Notes))
            intro += $"""<strong>Notes:</strong> {WebUtilityEncode(order.Notes)}<br/>""";

        var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, "PinoyPantry Website"),
            Subject = $"Walk-in sale recorded — {order.InvoiceNumber}",
            Body = BuildOrderEmailHtml(
                bannerText: "🛍️ Walk-in sale recorded",
                bannerColor: "#D32F2F",
                bannerTextColor: "#FFFFFF",
                intro: intro,
                order: order,
                extraBlocks: statusBlock,
                footer: "Automated notification from the PinoyPantry order system."
            ),
            IsBodyHtml = true
        };

        AddRecipients(mail.To, toAddress);
        await client.SendMailAsync(mail);
    }

    // Email:ToAddress may hold one or several comma-separated addresses (e.g. the
    // owner and a business partner both getting order notifications).
    private static void AddRecipients(MailAddressCollection recipients, string addresses)
    {
        foreach (var address in addresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            recipients.Add(address);
        }
    }

    private SmtpClient BuildSmtpClient(out string smtpUser, out string toAddress)
    {
        var smtpHost = _config["Email:SmtpHost"]!;
        var smtpPort = int.Parse(_config["Email:SmtpPort"]!);
        smtpUser = _config["Email:SmtpUser"]!;
        var smtpPass = _config["Email:SmtpPass"]!;
        toAddress = _config["Email:ToAddress"]!;

        return new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };
    }

    private static string WebUtilityEncode(string s) => WebUtility.HtmlEncode(s).Replace("\n", "<br/>");

    private async Task<string> BuildPaymentInstructionsBlock(string? invoiceNumber, string introText = "Please pay by bank transfer using the details below.")
    {
        var bank = await _bankDetailsService.GetBankDetailsAsync();
        var bankName = bank.BankName;
        var accountName = bank.AccountName;
        var accountNumber = bank.AccountNumber;

        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:rgba(249,168,37,0.12); border:1px solid rgba(249,168,37,0.4); border-radius:8px; margin-bottom:8px;">
              <tr><td style="padding:16px;">
                <p style="margin:0 0 8px; font-size:13px; font-weight:bold; color:#5a4200;">Payment instructions</p>
                <p style="margin:0 0 10px; font-size:13px; color:#5a4200;">{introText} Use <strong>{invoiceNumber}</strong> as your payment reference.</p>
                <p style="margin:0; font-size:14px; color:#3E2723; line-height:1.6;">
                  <strong>Account name:</strong> {WebUtility.HtmlEncode(accountName)}<br/>
                  <strong>Bank:</strong> {WebUtility.HtmlEncode(bankName)}<br/>
                  <strong>Account number:</strong> {WebUtility.HtmlEncode(accountNumber)}<br/>
                  <strong>Reference:</strong> {invoiceNumber}
                </p>
              </td></tr>
            </table>
            """;
    }

    // Shared layout for all order emails — only the banner, intro block and trailing
    // notice/instruction blocks differ between confirmation, notification and delivery-fee emails.
    private static string BuildOrderEmailHtml(
        string bannerText, string bannerColor, string bannerTextColor,
        string intro, Order order, string extraBlocks, string footer)
    {
        var sb = new StringBuilder();
        var placedAt = order.CreatedAt.ToString("d MMM yyyy, h:mm tt") + " UTC";

        sb.Append($"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
            <body style="margin:0; padding:0; background:#3E2723; font-family: Georgia, 'Times New Roman', serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#3E2723; padding:32px 16px;">
            <tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px; background:#FFF8E1; border-radius:12px; overflow:hidden;">

              <tr>
                <td style="background:#3E2723; padding:28px 32px; text-align:center;">
                  <p style="margin:0; color:#F9A825; font-size:22px; font-weight:bold; letter-spacing:0.5px;">PinoyPantry</p>
                  <p style="margin:4px 0 0; color:#FFF8E1; font-size:11px; letter-spacing:3px; text-transform:uppercase; opacity:0.7;">Pasabuy Na Bai!</p>
                </td>
              </tr>

              <tr>
                <td style="background:{bannerColor}; padding:12px 32px; text-align:center;">
                  <p style="margin:0; color:{bannerTextColor}; font-size:14px; font-weight:bold; letter-spacing:0.5px;">{bannerText}</p>
                </td>
              </tr>

              <tr>
                <td style="padding:32px; color:#3E2723;">
                  <p style="margin:0 0 4px; font-size:13px; letter-spacing:1px; text-transform:uppercase; color:#D32F2F; font-weight:bold;">Order #{order.Id} &middot; Invoice {order.InvoiceNumber}</p>
                  <p style="margin:0 0 20px; font-size:13px; color:#6D4C41;">Placed {placedAt}</p>

                  <p style="margin:0 0 24px; font-size:15px; line-height:1.6;">{intro}</p>

                  <p style="margin:0 0 8px; font-size:11px; text-transform:uppercase; letter-spacing:1px; color:#D32F2F; font-weight:bold;">Items</p>
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:20px;">
                    <tr style="background:#FBE9E7;">
                      <td style="padding:8px 12px; font-size:12px; font-weight:bold; color:#D32F2F;">Item</td>
                      <td style="padding:8px 12px; font-size:12px; font-weight:bold; color:#D32F2F; text-align:center;">Qty</td>
                      <td style="padding:8px 12px; font-size:12px; font-weight:bold; color:#D32F2F; text-align:right;">Amount</td>
                    </tr>
            """);

        foreach (var item in order.Items)
        {
            sb.Append($"""
                    <tr style="border-bottom:1px solid #f0e4d0;">
                      <td style="padding:10px 12px; font-size:14px;">{WebUtility.HtmlEncode(item.ProductName)}</td>
                      <td style="padding:10px 12px; font-size:14px; text-align:center;">{item.Quantity}</td>
                      <td style="padding:10px 12px; font-size:14px; text-align:right;">${(item.Price * item.Quantity):F2}</td>
                    </tr>
                """);
        }

        if (order.DeliveryFee is not null)
        {
            sb.Append($"""
                    <tr>
                      <td style="padding:10px 12px 0;"></td>
                      <td style="padding:10px 12px 0; font-size:13px; text-align:right; color:#6D4C41;">Delivery</td>
                      <td style="padding:10px 12px 0; font-size:13px; text-align:right; color:#6D4C41;">${order.DeliveryFee:F2}</td>
                    </tr>
                """);
        }

        var totalSuffix = order.DeliveryFee is null ? " + delivery" : "";
        sb.Append($"""
                    <tr>
                      <td style="padding:6px 12px 0;"></td>
                      <td style="padding:6px 12px 0; font-size:15px; font-weight:bold; text-align:right;">Total</td>
                      <td style="padding:6px 12px 0; font-size:16px; font-weight:bold; text-align:right; color:#D32F2F;">${order.Total:F2}{totalSuffix}</td>
                    </tr>
                  </table>
            """);

        if (order.DeliveryMethod is not null)
        {
            sb.Append($"""
                  <p style="margin:0 0 2px; font-size:11px; text-transform:uppercase; letter-spacing:1px; color:#D32F2F; font-weight:bold;">Delivery method</p>
                  <p style="margin:0 0 20px; font-size:15px;">{order.DeliveryMethod}</p>
                """);
        }

        if (!string.IsNullOrWhiteSpace(order.CustomerAddress))
        {
            sb.Append($"""
                  <p style="margin:0 0 2px; font-size:11px; text-transform:uppercase; letter-spacing:1px; color:#D32F2F; font-weight:bold;">Delivery / pickup address</p>
                  <p style="margin:0 0 20px; font-size:15px; white-space:pre-line;">{WebUtility.HtmlEncode(order.CustomerAddress)}</p>
                """);
        }

        sb.Append($"""
                  {extraBlocks}
                </td>
              </tr>

              <tr>
                <td style="padding:20px 32px; background:#3E2723; text-align:center;">
                  <p style="margin:0; font-size:11px; color:rgba(255,248,225,0.5);">{footer}</p>
                </td>
              </tr>

            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}
