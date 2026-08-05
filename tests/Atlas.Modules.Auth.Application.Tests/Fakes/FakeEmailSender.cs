using Atlas.Modules.Auth.Application.Abstractions;

namespace Atlas.Modules.Auth.Application.Tests.Fakes;

public class FakeEmailSender : IEmailSender
{
    public record SentEmail(string ToEmail, string Subject, string Body);

    public List<SentEmail> SentEmails { get; } = new();

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        SentEmails.Add(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }
}
