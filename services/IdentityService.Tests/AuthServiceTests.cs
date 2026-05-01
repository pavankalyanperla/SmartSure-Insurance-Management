using FluentAssertions;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Interfaces;
using Moq;
using NUnit.Framework;

namespace IdentityService.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IAuthRepository> _repoMock = null!;
    private Mock<IEmailService> _emailMock = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock  = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Loose);
        _sut = new AuthService(_repoMock.Object, _emailMock.Object);
    }

    // ── SendRegistrationOtp ───────────────────────────────────────────────────

    [Test]
    public async Task SendRegistrationOtp_WithNewEmail_SendsOtpAndReturnsMessage()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("new@example.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpsertOtpAsync("new@example.com", It.IsAny<string>(), It.IsAny<DateTime>()))
                 .ReturnsAsync(new OtpVerification { Id = 1, Email = "new@example.com" });

        var result = await _sut.SendRegistrationOtpAsync(new SendOtpRequestDto { Email = "NEW@example.com", FullName = "Test User" });

        result.Message.Should().Be("OTP sent to your email.");
        _emailMock.Verify(e => e.SendOtpEmailAsync("new@example.com", "Test User", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SendRegistrationOtp_WithExistingEmail_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("existing@example.com")).ReturnsAsync(true);

        Func<Task> act = () => _sut.SendRegistrationOtpAsync(new SendOtpRequestDto { Email = "existing@example.com", FullName = "X" });

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already registered*");
    }

    // ── VerifyRegistrationOtp ────────────────────────────────────────────────

    [Test]
    public async Task VerifyRegistrationOtp_WithValidOtp_CreatesUserAndMarksOtpUsed()
    {
        var otp = new OtpVerification { Id = 5, Email = "test@x.com", OtpCode = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = false };
        var createdUser = new User { Id = 10, FullName = "Test User", Email = "test@x.com", Role = "CUSTOMER" };

        _repoMock.Setup(r => r.EmailExistsAsync("test@x.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetLatestOtpAsync("test@x.com")).ReturnsAsync(otp);
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(createdUser);
        _repoMock.Setup(r => r.MarkOtpAsUsedAsync(5)).Returns(Task.CompletedTask);

        var result = await _sut.VerifyRegistrationOtpAsync(new VerifyRegistrationRequestDto
        {
            Email = "test@x.com", FullName = "Test User", Password = "Pass123!", OtpCode = "123456"
        });

        result.Email.Should().Be("test@x.com");
        result.Role.Should().Be("CUSTOMER");
        _repoMock.Verify(r => r.MarkOtpAsUsedAsync(5), Times.Once);
    }

    [Test]
    public async Task VerifyRegistrationOtp_WithExpiredOtp_ThrowsUnauthorized()
    {
        var expiredOtp = new OtpVerification { Id = 1, Email = "test@x.com", OtpCode = "999999", ExpiresAt = DateTime.UtcNow.AddMinutes(-1), IsUsed = false };

        _repoMock.Setup(r => r.EmailExistsAsync("test@x.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetLatestOtpAsync("test@x.com")).ReturnsAsync(expiredOtp);

        Func<Task> act = () => _sut.VerifyRegistrationOtpAsync(new VerifyRegistrationRequestDto
        {
            Email = "test@x.com", FullName = "X", Password = "P", OtpCode = "999999"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
    }

    [Test]
    public async Task VerifyRegistrationOtp_WithWrongCode_ThrowsUnauthorized()
    {
        var otp = new OtpVerification { Id = 2, Email = "u@x.com", OtpCode = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = false };

        _repoMock.Setup(r => r.EmailExistsAsync("u@x.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetLatestOtpAsync("u@x.com")).ReturnsAsync(otp);

        Func<Task> act = () => _sut.VerifyRegistrationOtpAsync(new VerifyRegistrationRequestDto
        {
            Email = "u@x.com", FullName = "X", Password = "P", OtpCode = "999999"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid OTP*");
    }

    [Test]
    public async Task VerifyRegistrationOtp_WithUsedOtp_ThrowsUnauthorized()
    {
        var usedOtp = new OtpVerification { Id = 3, Email = "u@x.com", OtpCode = "111111", ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = true };

        _repoMock.Setup(r => r.EmailExistsAsync("u@x.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetLatestOtpAsync("u@x.com")).ReturnsAsync(usedOtp);

        Func<Task> act = () => _sut.VerifyRegistrationOtpAsync(new VerifyRegistrationRequestDto
        {
            Email = "u@x.com", FullName = "X", Password = "P", OtpCode = "111111"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*No active OTP*");
    }

    [Test]
    public async Task VerifyRegistrationOtp_WhenEmailAlreadyRegistered_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("existing@x.com")).ReturnsAsync(true);

        Func<Task> act = () => _sut.VerifyRegistrationOtpAsync(new VerifyRegistrationRequestDto
        {
            Email = "existing@x.com", FullName = "X", Password = "P", OtpCode = "123456"
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already registered*");
    }

    [Test]
    public async Task VerifyRegistrationOtp_WhenNoOtpExists_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("new@x.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetLatestOtpAsync("new@x.com")).ReturnsAsync((OtpVerification?)null);

        Func<Task> act = () => _sut.VerifyRegistrationOtpAsync(new VerifyRegistrationRequestDto
        {
            Email = "new@x.com", FullName = "X", Password = "P", OtpCode = "000000"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*No active OTP*");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("SecurePass1!");
        var user = new User { Id = 1, FullName = "Jane", Email = "jane@x.com", PasswordHash = hash, Role = "CUSTOMER", IsActive = true };

        _repoMock.Setup(r => r.GetByEmailAsync("jane@x.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginDto { Email = "jane@x.com", Password = "SecurePass1!" });

        result.Email.Should().Be("jane@x.com");
        result.Role.Should().Be("CUSTOMER");
    }

    [Test]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!");
        var user = new User { Id = 2, Email = "u@x.com", PasswordHash = hash, IsActive = true };

        _repoMock.Setup(r => r.GetByEmailAsync("u@x.com")).ReturnsAsync(user);

        Func<Task> act = () => _sut.LoginAsync(new LoginDto { Email = "u@x.com", Password = "WrongPass!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid email or password*");
    }

    [Test]
    public async Task Login_WithNonExistentEmail_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("nobody@x.com")).ReturnsAsync((User?)null);

        Func<Task> act = () => _sut.LoginAsync(new LoginDto { Email = "nobody@x.com", Password = "Pass!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task Login_WithDeactivatedUser_ThrowsUnauthorized()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Pass1!");
        var user = new User { Id = 3, Email = "inactive@x.com", PasswordHash = hash, IsActive = false };

        _repoMock.Setup(r => r.GetByEmailAsync("inactive@x.com")).ReturnsAsync(user);

        Func<Task> act = () => _sut.LoginAsync(new LoginDto { Email = "inactive@x.com", Password = "Pass1!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*deactivated*");
    }

    // ── GetProfile ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetProfile_WithValidId_ReturnsUserProfile()
    {
        var user = new User { Id = 7, FullName = "Admin", Email = "admin@x.com", Role = "ADMIN", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);

        var result = await _sut.GetProfileAsync(7);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Admin");
        result.Role.Should().Be("ADMIN");
    }

    [Test]
    public async Task GetProfile_WithInvalidId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _sut.GetProfileAsync(999);

        result.Should().BeNull();
    }

    // ── GetAllUsers ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllUsers_ReturnsAllUsersAsDtos()
    {
        var users = new List<User>
        {
            new() { Id = 1, FullName = "A", Email = "a@x.com", Role = "CUSTOMER", IsActive = true },
            new() { Id = 2, FullName = "B", Email = "b@x.com", Role = "ADMIN",    IsActive = true },
            new() { Id = 3, FullName = "C", Email = "c@x.com", Role = "CUSTOMER", IsActive = false }
        };
        _repoMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);

        var result = await _sut.GetAllUsersAsync();

        result.Should().HaveCount(3);
        result[0].Email.Should().Be("a@x.com");
    }

    // ── UpdateUserStatus ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateUserStatus_CallsRepositoryAndReturnsResult()
    {
        _repoMock.Setup(r => r.UpdateUserStatusAsync(5, false)).ReturnsAsync(true);

        var result = await _sut.UpdateUserStatusAsync(5, false);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateUserStatusAsync(5, false), Times.Once);
    }

    // ── GetUsersCount / GetActiveUsersCount ───────────────────────────────────

    [Test]
    public async Task GetUsersCount_ReturnsTotalCount()
    {
        _repoMock.Setup(r => r.GetUsersCountAsync()).ReturnsAsync(42);
        var count = await _sut.GetUsersCountAsync();
        count.Should().Be(42);
    }

    [Test]
    public async Task GetActiveUsersCount_ReturnsActiveCount()
    {
        _repoMock.Setup(r => r.GetActiveUsersCountAsync()).ReturnsAsync(30);
        var count = await _sut.GetActiveUsersCountAsync();
        count.Should().Be(30);
    }

    // ── SendPasswordResetOtp ──────────────────────────────────────────────────

    [Test]
    public async Task SendPasswordResetOtp_WithExistingEmail_SendsEmailAndReturnsMessage()
    {
        var user = new User { Id = 1, FullName = "Alice", Email = "alice@x.com" };
        _repoMock.Setup(r => r.GetByEmailAsync("alice@x.com")).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpsertOtpAsync("alice@x.com", It.IsAny<string>(), It.IsAny<DateTime>()))
                 .ReturnsAsync(new OtpVerification());

        var result = await _sut.SendPasswordResetOtpAsync(new ForgotPasswordSendOtpDto { Email = "alice@x.com" });

        result.Message.Should().Contain("Password reset OTP");
        _emailMock.Verify(e => e.SendPasswordResetOtpEmailAsync("alice@x.com", "Alice", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SendPasswordResetOtp_WithNonExistentEmail_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ghost@x.com")).ReturnsAsync((User?)null);

        Func<Task> act = () => _sut.SendPasswordResetOtpAsync(new ForgotPasswordSendOtpDto { Email = "ghost@x.com" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No account found*");
    }

    // ── ResetPassword ─────────────────────────────────────────────────────────

    [Test]
    public async Task ResetPassword_WithValidOtp_UpdatesPasswordAndMarksOtpUsed()
    {
        var otp = new OtpVerification { Id = 8, Email = "u@x.com", OtpCode = "654321", ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = false };

        _repoMock.Setup(r => r.GetLatestOtpAsync("u@x.com")).ReturnsAsync(otp);
        _repoMock.Setup(r => r.UpdatePasswordAsync("u@x.com", It.IsAny<string>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.MarkOtpAsUsedAsync(8)).Returns(Task.CompletedTask);

        await _sut.ResetPasswordAsync(new ResetPasswordDto { Email = "u@x.com", OtpCode = "654321", NewPassword = "NewPass1!" });

        _repoMock.Verify(r => r.UpdatePasswordAsync("u@x.com", It.IsAny<string>()), Times.Once);
        _repoMock.Verify(r => r.MarkOtpAsUsedAsync(8), Times.Once);
    }

    [Test]
    public async Task ResetPassword_WithExpiredOtp_ThrowsUnauthorized()
    {
        var otp = new OtpVerification { Id = 9, Email = "u@x.com", OtpCode = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(-5), IsUsed = false };
        _repoMock.Setup(r => r.GetLatestOtpAsync("u@x.com")).ReturnsAsync(otp);

        Func<Task> act = () => _sut.ResetPasswordAsync(new ResetPasswordDto { Email = "u@x.com", OtpCode = "123456", NewPassword = "X" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
    }

    [Test]
    public async Task ResetPassword_WithWrongOtp_ThrowsUnauthorized()
    {
        var otp = new OtpVerification { Id = 10, Email = "u@x.com", OtpCode = "111111", ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = false };
        _repoMock.Setup(r => r.GetLatestOtpAsync("u@x.com")).ReturnsAsync(otp);

        Func<Task> act = () => _sut.ResetPasswordAsync(new ResetPasswordDto { Email = "u@x.com", OtpCode = "999999", NewPassword = "X" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid OTP*");
    }

    [Test]
    public async Task ResetPassword_WithNullOtp_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.GetLatestOtpAsync("u@x.com")).ReturnsAsync((OtpVerification?)null);

        Func<Task> act = () => _sut.ResetPasswordAsync(new ResetPasswordDto { Email = "u@x.com", OtpCode = "111111", NewPassword = "X" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*No active OTP*");
    }

    // ── Register (legacy direct-registration method) ──────────────────────────

    [Test]
    public async Task Register_WithNewEmail_CreatesUserAndReturnsDto()
    {
        var created = new User { Id = 20, FullName = "Bob Smith", Email = "bob@x.com", Role = "CUSTOMER" };

        _repoMock.Setup(r => r.EmailExistsAsync("bob@x.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(created);

        var result = await _sut.RegisterAsync(new RegisterDto { FullName = "Bob Smith", Email = "bob@x.com", Password = "Pass1!" });

        result.Email.Should().Be("bob@x.com");
        result.FullName.Should().Be("Bob Smith");
    }

    [Test]
    public async Task Register_WithExistingEmail_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("dup@x.com")).ReturnsAsync(true);

        Func<Task> act = () => _sut.RegisterAsync(new RegisterDto { FullName = "X", Email = "dup@x.com", Password = "P" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already registered*");
    }

    // ── Claim Status Email Notifications ─────────────────────────────────────

    [Test]
    public async Task SendClaimStatusEmail_ApprovedStatus_InvokesEmailServiceWithApprovedNotification()
    {
        var notification = new ClaimStatusNotificationDto
        {
            ClaimId       = 1,
            ClaimNumber   = "CLM-001",
            CustomerEmail = "customer@x.com",
            CustomerName  = "Alice",
            OldStatus     = "Submitted",
            NewStatus     = "Approved",
            AdminNote     = "All documents verified",
            ChangedAt     = DateTime.UtcNow
        };

        await _emailMock.Object.SendClaimStatusEmailAsync(notification);

        _emailMock.Verify(e => e.SendClaimStatusEmailAsync(
            It.Is<ClaimStatusNotificationDto>(n =>
                n.NewStatus == "Approved" && n.ClaimId == 1 && n.CustomerEmail == "customer@x.com")),
            Times.Once);
    }

    [Test]
    public async Task SendClaimStatusEmail_RejectedStatus_InvokesEmailServiceWithRejectedNotification()
    {
        var notification = new ClaimStatusNotificationDto
        {
            ClaimId       = 2,
            ClaimNumber   = "CLM-002",
            CustomerEmail = "bob@x.com",
            CustomerName  = "Bob",
            OldStatus     = "UnderReview",
            NewStatus     = "Rejected",
            AdminNote     = "Insufficient documentation",
            ChangedAt     = DateTime.UtcNow
        };

        await _emailMock.Object.SendClaimStatusEmailAsync(notification);

        _emailMock.Verify(e => e.SendClaimStatusEmailAsync(
            It.Is<ClaimStatusNotificationDto>(n =>
                n.NewStatus == "Rejected" && n.AdminNote == "Insufficient documentation")),
            Times.Once);
    }

    [Test]
    public async Task SendClaimStatusEmail_UnderReviewStatus_InvokesEmailServiceWithReviewNotification()
    {
        var notification = new ClaimStatusNotificationDto
        {
            ClaimId       = 3,
            ClaimNumber   = "CLM-003",
            CustomerEmail = "carol@x.com",
            CustomerName  = "Carol",
            OldStatus     = "Submitted",
            NewStatus     = "UnderReview",
            AdminNote     = string.Empty,
            ChangedAt     = DateTime.UtcNow
        };

        await _emailMock.Object.SendClaimStatusEmailAsync(notification);

        _emailMock.Verify(e => e.SendClaimStatusEmailAsync(
            It.Is<ClaimStatusNotificationDto>(n =>
                n.NewStatus == "UnderReview" && n.ClaimNumber == "CLM-003")),
            Times.Once);
    }

    [Test]
    public async Task SendClaimStatusEmail_ClosedStatus_InvokesEmailServiceWithClosedNotification()
    {
        var notification = new ClaimStatusNotificationDto
        {
            ClaimId       = 4,
            ClaimNumber   = "CLM-004",
            CustomerEmail = "dave@x.com",
            CustomerName  = "Dave",
            OldStatus     = "Approved",
            NewStatus     = "Closed",
            AdminNote     = "Claim paid out",
            ChangedAt     = DateTime.UtcNow
        };

        await _emailMock.Object.SendClaimStatusEmailAsync(notification);

        _emailMock.Verify(e => e.SendClaimStatusEmailAsync(
            It.Is<ClaimStatusNotificationDto>(n =>
                n.NewStatus == "Closed" && n.CustomerName == "Dave")),
            Times.Once);
    }
}
