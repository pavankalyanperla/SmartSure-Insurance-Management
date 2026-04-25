using IdentityService.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var smtpHost = emailSettings["SmtpHost"] ?? throw new InvalidOperationException("EmailSettings:SmtpHost is missing.");
        var smtpPortValue = emailSettings["SmtpPort"] ?? throw new InvalidOperationException("EmailSettings:SmtpPort is missing.");
        var fromEmail = emailSettings["FromEmail"] ?? throw new InvalidOperationException("EmailSettings:FromEmail is missing.");
        var fromName = emailSettings["FromName"] ?? "SmartSure Insurance";
      var smtpUser = emailSettings["Username"] ?? fromEmail;
      var smtpPassword = emailSettings["Password"] ?? emailSettings["AppPassword"];
      var useAuthentication = bool.TryParse(emailSettings["UseAuthentication"], out var authFlag) ? authFlag : true;
      var useStartTls = bool.TryParse(emailSettings["UseStartTls"], out var tlsFlag) ? tlsFlag : true;
        var smtpPort = int.Parse(smtpPortValue);

      if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromEmail))
      {
        throw new InvalidOperationException("Email delivery is not configured correctly. Set EmailSettings in IdentityService appsettings.");
      }

      if (useAuthentication && string.IsNullOrWhiteSpace(smtpPassword))
      {
        throw new InvalidOperationException("EmailSettings:Password (or AppPassword) is required when UseAuthentication is true.");
      }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(fullName, toEmail));
        message.Subject = "SmartSure - Your Registration OTP";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f9fafb; margin: 0; padding: 20px; }}
    .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
    .header {{ background: #1a56db; padding: 30px; text-align: center; }}
    .header h1 {{ color: white; margin: 0; font-size: 24px; }}
    .header p {{ color: #bfdbfe; margin: 8px 0 0; font-size: 14px; }}
    .body {{ padding: 30px; }}
    .greeting {{ font-size: 16px; color: #111928; margin-bottom: 16px; }}
    .otp-box {{ background: #eff6ff; border: 2px dashed #1a56db; border-radius: 12px; padding: 24px; text-align: center; margin: 24px 0; }}
    .otp-label {{ font-size: 13px; color: #6b7280; margin-bottom: 8px; }}
    .otp-code {{ font-size: 42px; font-weight: 700; color: #1a56db; letter-spacing: 12px; font-family: monospace; }}
    .expiry {{ font-size: 13px; color: #ef4444; margin-top: 12px; }}
    .info {{ font-size: 13px; color: #6b7280; line-height: 1.6; }}
    .footer {{ background: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb; }}
    .footer p {{ font-size: 12px; color: #9ca3af; margin: 0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>SmartSure</h1>
      <p>Insurance Management System</p>
    </div>
    <div class='body'>
      <p class='greeting'>Hello <strong>{fullName}</strong>,</p>
      <p class='info'>Thank you for registering with SmartSure! Use the OTP below to verify your email address and complete your registration.</p>
      <div class='otp-box'>
        <div class='otp-label'>Your One-Time Password</div>
        <div class='otp-code'>{otpCode}</div>
        <div class='expiry'>This OTP expires in 15 minutes</div>
      </div>
      <p class='info'>If you did not request this OTP, please ignore this email. Your account will not be created.</p>
    </div>
    <div class='footer'>
      <p>Copyright 2026 SmartSure Insurance Management System. All rights reserved.</p>
    </div>
  </div>
</body>
</html>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = 10000;
        await client.ConnectAsync(
            smtpHost,
            smtpPort,
          useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

        if (useAuthentication)
        {
          await client.AuthenticateAsync(
            smtpUser,
            smtpPassword!);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
