using IdentityService.Application.DTOs;
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

    public async Task SendPasswordResetOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var smtpHost     = emailSettings["SmtpHost"]     ?? throw new InvalidOperationException("EmailSettings:SmtpHost is missing.");
        var smtpPortStr  = emailSettings["SmtpPort"]     ?? throw new InvalidOperationException("EmailSettings:SmtpPort is missing.");
        var fromEmail    = emailSettings["FromEmail"]    ?? throw new InvalidOperationException("EmailSettings:FromEmail is missing.");
        var fromName     = emailSettings["FromName"]     ?? "SmartSure Insurance";
        var smtpUser     = emailSettings["Username"]     ?? fromEmail;
        var smtpPassword = emailSettings["Password"]     ?? emailSettings["AppPassword"];
        var useAuth      = bool.TryParse(emailSettings["UseAuthentication"], out var af) ? af : true;
        var useTls       = bool.TryParse(emailSettings["UseStartTls"],       out var tf) ? tf : true;
        var smtpPort     = int.Parse(smtpPortStr);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(fullName, toEmail));
        message.Subject = "SmartSure - Password Reset OTP";

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
    .header {{ background: linear-gradient(135deg,#1a56db,#7c3aed); padding: 30px; text-align: center; }}
    .header h1 {{ color: white; margin: 0; font-size: 24px; }}
    .header p {{ color: rgba(255,255,255,0.8); margin: 8px 0 0; font-size: 14px; }}
    .body {{ padding: 30px; }}
    .greeting {{ font-size: 16px; color: #111928; margin-bottom: 16px; }}
    .otp-box {{ background: #f5f3ff; border: 2px dashed #7c3aed; border-radius: 12px; padding: 24px; text-align: center; margin: 24px 0; }}
    .otp-label {{ font-size: 13px; color: #6b7280; margin-bottom: 8px; }}
    .otp-code {{ font-size: 42px; font-weight: 700; color: #7c3aed; letter-spacing: 12px; font-family: monospace; }}
    .expiry {{ font-size: 13px; color: #ef4444; margin-top: 12px; }}
    .info {{ font-size: 13px; color: #6b7280; line-height: 1.6; }}
    .warning {{ font-size: 13px; color: #92400e; background: #fef3c7; border: 1px solid #fde68a; border-radius: 8px; padding: 10px 14px; margin-top: 16px; }}
    .footer {{ background: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb; }}
    .footer p {{ font-size: 12px; color: #9ca3af; margin: 0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>SmartSure</h1>
      <p>Password Reset Request</p>
    </div>
    <div class='body'>
      <p class='greeting'>Hello <strong>{fullName}</strong>,</p>
      <p class='info'>We received a request to reset your SmartSure account password. Use the OTP below to set a new password.</p>
      <div class='otp-box'>
        <div class='otp-label'>Your Password Reset OTP</div>
        <div class='otp-code'>{otpCode}</div>
        <div class='expiry'>This OTP expires in 15 minutes</div>
      </div>
      <div class='warning'>If you did not request a password reset, please ignore this email. Your password will not be changed.</div>
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
        await client.ConnectAsync(smtpHost, smtpPort, useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        if (useAuth) await client.AuthenticateAsync(smtpUser, smtpPassword!);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendClaimStatusEmailAsync(ClaimStatusNotificationDto notification)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var smtpHost     = emailSettings["SmtpHost"]     ?? throw new InvalidOperationException("EmailSettings:SmtpHost is missing.");
        var smtpPortStr  = emailSettings["SmtpPort"]     ?? throw new InvalidOperationException("EmailSettings:SmtpPort is missing.");
        var fromEmail    = emailSettings["FromEmail"]    ?? throw new InvalidOperationException("EmailSettings:FromEmail is missing.");
        var fromName     = emailSettings["FromName"]     ?? "SmartSure Insurance";
        var smtpUser     = emailSettings["Username"]     ?? fromEmail;
        var smtpPassword = emailSettings["Password"]     ?? emailSettings["AppPassword"];
        var useAuth      = bool.TryParse(emailSettings["UseAuthentication"], out var af) ? af : true;
        var useTls       = bool.TryParse(emailSettings["UseStartTls"],       out var tf) ? tf : true;
        var smtpPort     = int.Parse(smtpPortStr);

        var statusMessage = notification.NewStatus switch
        {
            "UnderReview" => "Our team is currently reviewing your claim documents. We will notify you once a decision is made.",
            "Approved"    => "Congratulations! Your claim has been approved. Our team will process the payment shortly.",
            "Rejected"    => "Unfortunately, your claim has been rejected. Please contact support for more information.",
            "Closed"      => "Your claim has been closed. Thank you for choosing SmartSure.",
            _             => "Your claim status has been updated. Please log in to view details."
        };

        var adminNoteSection = string.IsNullOrWhiteSpace(notification.AdminNote)
            ? string.Empty
            : $"<div class='info-row'><span>Admin Note:</span><span>{notification.AdminNote}</span></div>";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(notification.CustomerName, notification.CustomerEmail));
        message.Subject = $"SmartSure — Your Claim {notification.ClaimNumber} Status Update";

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
    .body {{ padding: 30px; }}
    .status-badge {{
      display: inline-block; padding: 8px 20px; border-radius: 20px;
      font-weight: 700; font-size: 15px; margin: 4px 0;
    }}
    .UnderReview {{ background: #ede9fe; color: #7c3aed; }}
    .Approved    {{ background: #dcfce7; color: #16a34a; }}
    .Rejected    {{ background: #fee2e2; color: #dc2626; }}
    .Closed      {{ background: #f3f4f6; color: #6b7280; }}
    .Submitted   {{ background: #dbeafe; color: #1d4ed8; }}
    .info-row {{ display: flex; justify-content: space-between; align-items: center; padding: 10px 0; border-bottom: 1px solid #f3f4f6; font-size: 14px; color: #374151; }}
    .info-row span:first-child {{ color: #6b7280; }}
    .message-box {{ background: #f0f9ff; border-left: 4px solid #1a56db; padding: 14px 16px; margin: 20px 0; border-radius: 0 8px 8px 0; font-size: 14px; color: #1e40af; line-height: 1.6; }}
    .footer {{ background: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb; font-size: 12px; color: #9ca3af; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>SmartSure</h1>
    </div>
    <div class='body'>
      <p style='font-size:16px;color:#111928'>Hello <strong>{notification.CustomerName}</strong>,</p>
      <p style='font-size:14px;color:#6b7280'>Your insurance claim status has been updated.</p>
      <div class='info-row'><span>Claim Number:</span><strong>{notification.ClaimNumber}</strong></div>
      <div class='info-row'><span>Previous Status:</span><span>{notification.OldStatus}</span></div>
      <div class='info-row'>
        <span>New Status:</span>
        <span class='status-badge {notification.NewStatus}'>{notification.NewStatus}</span>
      </div>
      {adminNoteSection}
      <div class='message-box'>{statusMessage}</div>
      <p style='font-size:13px;color:#6b7280'>You can track your claim status by logging into your SmartSure account.</p>
    </div>
    <div class='footer'>© 2026 SmartSure Insurance Management System. All rights reserved.</div>
  </div>
</body>
</html>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = 10000;
        await client.ConnectAsync(smtpHost, smtpPort, useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        if (useAuth) await client.AuthenticateAsync(smtpUser, smtpPassword!);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
