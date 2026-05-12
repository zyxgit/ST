using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ST.Infra.Email.Abstractions;
using ST.Infra.Email.Exceptions;

namespace ST.Infra.Email.Smtp;

public class SmtpEmailSender : IEmailSender
{
	private readonly SmtpOptions _options;

	public SmtpEmailSender(IOptions<SmtpOptions> options)
	{
		_options = options.Value;
	}

	public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
	{
		var message = new MimeMessage();

		message.From.Add(new MailboxAddress(
			_options.FromName,
			_options.From));

		message.To.Add(MailboxAddress.Parse(to));
		message.Subject = subject;

		message.Body = new BodyBuilder
		{
			HtmlBody = htmlBody
		}.ToMessageBody();

		try
		{
			using var client = new SmtpClient();

			await client.ConnectAsync(
				_options.Host,
				_options.Port,
				_options.UseSsl
					? SecureSocketOptions.SslOnConnect
					: SecureSocketOptions.StartTls,
				cancellationToken);

			await client.AuthenticateAsync(
				_options.UserName,
				_options.Password,
				cancellationToken);

			await client.SendAsync(message, cancellationToken);
			await client.DisconnectAsync(true, cancellationToken);
		}
		catch (Exception ex)
		{
			throw new EmailSendException("邮件发送失败", ex);
		}
	}
}
