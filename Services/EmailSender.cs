using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using PortfolioAPI.Models;

namespace PortfolioAPI.Services;

public interface IEmailSender
{
    /// <summary>Envoie le message de contact au propriétaire du portfolio. Lève une exception en cas d'échec.</summary>
    Task SendContactAsync(ContactRequest contact, CancellationToken ct);
}

/// <summary>
/// Envoi via l'API HTTP Resend (fonctionne là où les ports SMTP sont bloqués)
/// ou via SMTP (ex. Gmail + mot de passe d'application). Choix par Email:Provider.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public EmailSender(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    public async Task SendContactAsync(ContactRequest c, CancellationToken ct)
    {
        var to = _config["Email:To"];
        if (string.IsNullOrWhiteSpace(to))
            throw new InvalidOperationException("Email:To non configuré.");

        // Sujet sur une seule ligne (évite l'injection d'en-têtes), corps en texte brut
        var subject = $"[Portfolio] {OneLine(c.Sujet)} — {OneLine(c.Nom)}";
        var body = $"Nouveau message depuis le portfolio\n\nNom : {OneLine(c.Nom)}\nE-mail : {c.Email}\nSujet : {OneLine(c.Sujet)}\n\n{c.Contenu}\n";

        switch ((_config["Email:Provider"] ?? "").Trim().ToLowerInvariant())
        {
            case "resend":
                await SendWithResendAsync(to, subject, body, c.Email, ct);
                break;
            case "smtp":
                await SendWithSmtpAsync(to, subject, body, c.Email, ct);
                break;
            default:
                throw new InvalidOperationException("Email:Provider doit valoir 'Resend' ou 'Smtp'.");
        }
    }

    private async Task SendWithResendAsync(string to, string subject, string body, string replyTo, CancellationToken ct)
    {
        var apiKey = _config["Email:ResendApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Email:ResendApiKey non configuré.");

        var client = _httpFactory.CreateClient("resend");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from = _config["Email:From"] ?? "Portfolio <onboarding@resend.dev>",
                to = new[] { to },
                subject,
                text = body,
                reply_to = replyTo
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Resend a refusé l'envoi ({(int)response.StatusCode}).");
    }

    private async Task SendWithSmtpAsync(string to, string subject, string body, string replyTo, CancellationToken ct)
    {
        var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        var user = _config["Email:SmtpUser"];
        var password = _config["Email:SmtpPassword"];
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Email:SmtpUser / Email:SmtpPassword non configurés.");

        using var mail = new MailMessage(new MailAddress(user, "Portfolio"), new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        mail.ReplyToList.Add(new MailAddress(replyTo));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password),
            Timeout = 15000
        };
        await smtp.SendMailAsync(mail, ct);
    }

    private static string OneLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
