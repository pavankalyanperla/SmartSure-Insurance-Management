using System.Net;
using System.Text;
using System.Text.Json;
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Application.Services;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace AdminService.Tests;

[TestFixture]
public class AdminServiceTests
{
    private Mock<IAdminRepository> _repoMock = null!;
    private Mock<ILogger<AdminAppService>> _loggerMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private Mock<IHttpContextAccessor> _httpContextMock = null!;
    private Mock<INotificationPublisher> _notificationPublisherMock = null!;

    private AdminAppService BuildSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);

        // The notification path uses _httpClientFactory.CreateClient() to get a scope-independent
        // HttpClient. Wire it to the same handler so notification tests still intercept correctly.
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler));

        return new AdminAppService(
            _repoMock.Object,
            httpClient,
            factoryMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            _httpContextMock.Object,
            _notificationPublisherMock.Object);
    }

    [SetUp]
    public void SetUp()
    {
        _repoMock                    = new Mock<IAdminRepository>(MockBehavior.Loose);
        _loggerMock                  = new Mock<ILogger<AdminAppService>>();
        _configMock                  = new Mock<IConfiguration>();
        _httpContextMock             = new Mock<IHttpContextAccessor>();
        _notificationPublisherMock   = new Mock<INotificationPublisher>(MockBehavior.Loose);

        _notificationPublisherMock
            .Setup(p => p.PublishClaimStatusChangedAsync(It.IsAny<ClaimStatusNotificationDto>()))
            .Returns(Task.CompletedTask);

        // Provide service URL config values
        _configMock.Setup(c => c["ServiceUrls:IdentityService"]).Returns("http://identity");
        _configMock.Setup(c => c["ServiceUrls:PolicyService"]).Returns("http://policy");
        _configMock.Setup(c => c["ServiceUrls:ClaimsService"]).Returns("http://claims");

        // HttpContext returns null bearer → no auth header forwarded
        _httpContextMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
    }

    // ── Helper: create a handler that returns a given JSON body ──────────────

    private static HttpMessageHandler JsonHandler(params (string url, string body)[] responses)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                foreach (var (url, body) in responses)
                {
                    if (req.RequestUri!.ToString().Contains(url))
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(body, Encoding.UTF8, "application/json")
                        };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
        return handlerMock.Object;
    }

    private static HttpMessageHandler SuccessHandler(string body = "{}")
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        return handlerMock.Object;
    }

    private static HttpMessageHandler ErrorHandler()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        return handlerMock.Object;
    }

    // ── GetDashboardSummary ───────────────────────────────────────────────────

    [Test]
    public async Task GetDashboardSummary_WithAllServicesResponding_ReturnsAggregatedData()
    {
        var identityBody = JsonSerializer.Serialize(new { totalUsers = 10, activeUsers = 8, inactiveUsers = 2 });
        var policyBody   = JsonSerializer.Serialize(new { totalPolicies = 6, totalRevenue = 32025.50m });
        var claimsBody   = JsonSerializer.Serialize(new
        {
            totalClaims = 5, draftClaims = 0, submittedClaims = 3,
            underReviewClaims = 1, approvedClaims = 1, rejectedClaims = 0, closedClaims = 0
        });

        var handler = JsonHandler(
            ("users/count", identityBody),
            ("policies/admin/count", policyBody),
            ("claims/admin/stats", claimsBody));

        var sut = BuildSut(handler);
        var result = await sut.GetDashboardSummaryAsync();

        result.TotalUsers.Should().Be(10);
        result.ActiveUsers.Should().Be(8);
        result.TotalPolicies.Should().Be(6);
        result.TotalRevenue.Should().Be(32025.50m);
        result.TotalClaims.Should().Be(5);
        result.PendingClaims.Should().Be(4); // submitted(3) + underReview(1)
        result.ApprovedClaims.Should().Be(1);
    }

    [Test]
    public async Task GetDashboardSummary_WhenAllServicesDown_ReturnsDefaultZeros()
    {
        var sut = BuildSut(ErrorHandler());

        var result = await sut.GetDashboardSummaryAsync();

        result.TotalUsers.Should().Be(0);
        result.TotalPolicies.Should().Be(0);
        result.TotalClaims.Should().Be(0);
    }

    [Test]
    public async Task GetDashboardSummary_WhenIdentityServiceDown_OtherDataStillPopulated()
    {
        var policyBody = JsonSerializer.Serialize(new { totalPolicies = 5, totalRevenue = 10000m });
        var claimsBody = JsonSerializer.Serialize(new
        {
            totalClaims = 3, draftClaims = 0, submittedClaims = 2,
            underReviewClaims = 0, approvedClaims = 1, rejectedClaims = 0, closedClaims = 0
        });

        var handler = JsonHandler(
            ("policies/admin/count", policyBody),
            ("claims/admin/stats", claimsBody));

        var sut = BuildSut(handler);
        var result = await sut.GetDashboardSummaryAsync();

        result.TotalUsers.Should().Be(0);   // identity was down
        result.TotalPolicies.Should().Be(5);
        result.TotalClaims.Should().Be(3);
    }

    // ── GetAllUsers ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllUsers_WhenIdentityServiceReturnsUsers_ReturnsMappedList()
    {
        var usersBody = JsonSerializer.Serialize(new[]
        {
            new { id = 1, fullName = "Alice", email = "alice@x.com", role = "CUSTOMER", isActive = true, createdAt = DateTime.UtcNow },
            new { id = 2, fullName = "Bob",   email = "bob@x.com",   role = "ADMIN",    isActive = true, createdAt = DateTime.UtcNow },
            new { id = 3, fullName = "Carol", email = "carol@x.com", role = "CUSTOMER", isActive = false, createdAt = DateTime.UtcNow }
        });

        var sut = BuildSut(SuccessHandler(usersBody));
        var result = await sut.GetAllUsersAsync();

        result.Should().HaveCount(3);
        result[0].FullName.Should().Be("Alice");
        result[1].Role.Should().Be("ADMIN");
        result[2].IsActive.Should().BeFalse();
    }

    [Test]
    public async Task GetAllUsers_WhenIdentityServiceDown_ReturnsEmptyList()
    {
        var sut = BuildSut(ErrorHandler());
        var result = await sut.GetAllUsersAsync();
        result.Should().BeEmpty();
    }

    // ── GetAllClaims ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllClaims_WhenClaimsServiceReturns_ReturnsMappedList()
    {
        var claimsBody = JsonSerializer.Serialize(new[]
        {
            new { id = 1, claimNumber = "CLM-1", policyId = 10, customerId = 5, incidentDate = DateTime.UtcNow, description = "Fire", status = "Submitted", adminNote = (string?)null, createdAt = DateTime.UtcNow },
            new { id = 2, claimNumber = "CLM-2", policyId = 11, customerId = 6, incidentDate = DateTime.UtcNow, description = "Flood", status = "UnderReview", adminNote = (string?)null, createdAt = DateTime.UtcNow }
        });

        var sut = BuildSut(SuccessHandler(claimsBody));
        var result = await sut.GetAllClaimsAsync();

        result.Should().HaveCount(2);
        result[0].ClaimNumber.Should().Be("CLM-1");
        result[1].Status.Should().Be("UnderReview");
    }

    [Test]
    public async Task GetAllClaims_WhenClaimsServiceDown_ReturnsEmptyList()
    {
        var sut = BuildSut(ErrorHandler());
        var result = await sut.GetAllClaimsAsync();
        result.Should().BeEmpty();
    }

    // ── GetPendingClaims ──────────────────────────────────────────────────────

    [Test]
    public async Task GetPendingClaims_FiltersToSubmittedAndUnderReviewOnly()
    {
        var claimsBody = JsonSerializer.Serialize(new[]
        {
            new { id = 1, claimNumber = "CLM-1", policyId = 1, customerId = 1, incidentDate = DateTime.UtcNow, description = "A", status = "Submitted",  adminNote = (string?)null, createdAt = DateTime.UtcNow },
            new { id = 2, claimNumber = "CLM-2", policyId = 2, customerId = 2, incidentDate = DateTime.UtcNow, description = "B", status = "UnderReview", adminNote = (string?)null, createdAt = DateTime.UtcNow },
            new { id = 3, claimNumber = "CLM-3", policyId = 3, customerId = 3, incidentDate = DateTime.UtcNow, description = "C", status = "Approved",    adminNote = (string?)null, createdAt = DateTime.UtcNow },
            new { id = 4, claimNumber = "CLM-4", policyId = 4, customerId = 4, incidentDate = DateTime.UtcNow, description = "D", status = "Closed",      adminNote = (string?)null, createdAt = DateTime.UtcNow }
        });

        var sut = BuildSut(SuccessHandler(claimsBody));
        var result = await sut.GetPendingClaimsAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Status == "Submitted" || c.Status == "UnderReview");
    }

    // ── UpdateClaimStatus ─────────────────────────────────────────────────────

    [Test]
    public async Task UpdateClaimStatus_WhenClaimsServiceSucceeds_CreatesAdminLog()
    {
        var sut = BuildSut(SuccessHandler());

        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>()))
                 .ReturnsAsync(new AdminLog { Id = 1 });

        var result = await sut.UpdateClaimStatusAsync(
            new UpdateClaimStatusDto { ClaimId = 5, Status = "Approved", AdminNote = "All good" },
            adminId: 1);

        result.ClaimId.Should().Be(5);
        _repoMock.Verify(r => r.CreateLogAsync(It.Is<AdminLog>(l =>
            l.Action == "UpdateClaimStatus" && l.TargetId == 5)), Times.Once);
    }

    [Test]
    public async Task UpdateClaimStatus_WhenClaimsServiceFails_ThrowsException()
    {
        var sut = BuildSut(ErrorHandler());

        Func<Task> act = () => sut.UpdateClaimStatusAsync(
            new UpdateClaimStatusDto { ClaimId = 99, Status = "Approved" },
            adminId: 1);

        await act.Should().ThrowAsync<Exception>();
    }

    // ── UpdateUserStatus ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateUserStatus_WhenIdentityServiceSucceeds_CreatesAdminLog()
    {
        var sut = BuildSut(SuccessHandler());

        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>()))
                 .ReturnsAsync(new AdminLog { Id = 2 });

        var result = await sut.UpdateUserStatusAsync(7, new UpdateUserStatusDto { IsActive = false });

        result.UserId.Should().Be(7);
        _repoMock.Verify(r => r.CreateLogAsync(It.Is<AdminLog>(l =>
            l.Action == "UpdateUserStatus" && l.TargetId == 7)), Times.Once);
    }

    [Test]
    public async Task UpdateUserStatus_WhenIdentityServiceFails_ThrowsException()
    {
        var sut = BuildSut(ErrorHandler());

        Func<Task> act = () => sut.UpdateUserStatusAsync(7, new UpdateUserStatusDto { IsActive = false });

        await act.Should().ThrowAsync<Exception>();
    }

    // ── GenerateReport ────────────────────────────────────────────────────────

    [Test]
    public async Task GenerateReport_SavesReportAndCreatesLog()
    {
        // All 3 downstream calls return empty/default data (service is down, gracefully handles it)
        var sut = BuildSut(ErrorHandler());

        _repoMock.Setup(r => r.CreateReportAsync(It.IsAny<Report>()))
                 .ReturnsAsync(new Report { Id = 10 });
        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>()))
                 .ReturnsAsync(new AdminLog { Id = 20 });

        var result = await sut.GenerateReportAsync("Summary", adminId: 1);

        result.Should().NotBeNull();
        _repoMock.Verify(r => r.CreateReportAsync(It.IsAny<Report>()), Times.Once);
        _repoMock.Verify(r => r.CreateLogAsync(It.Is<AdminLog>(l => l.Action == "GenerateReport")), Times.Once);
    }

    [Test]
    public async Task GenerateReport_WithUnknownReportType_DoesNotThrow()
    {
        var sut = BuildSut(ErrorHandler());

        _repoMock.Setup(r => r.CreateReportAsync(It.IsAny<Report>())).ReturnsAsync(new Report { Id = 1 });
        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>())).ReturnsAsync(new AdminLog { Id = 1 });

        Func<Task> act = () => sut.GenerateReportAsync("InvalidType", adminId: 1);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task GenerateReport_WithRealData_ReturnsCorrectTotals()
    {
        var identityBody = JsonSerializer.Serialize(new { totalUsers = 20, activeUsers = 15, inactiveUsers = 5 });
        var policyBody   = JsonSerializer.Serialize(new { totalPolicies = 12, totalRevenue = 60000m });
        var claimsBody   = JsonSerializer.Serialize(new
        {
            totalClaims = 8, draftClaims = 1, submittedClaims = 2,
            underReviewClaims = 2, approvedClaims = 2, rejectedClaims = 1, closedClaims = 0
        });

        var handler = JsonHandler(
            ("users/count", identityBody),
            ("policies/admin/count", policyBody),
            ("claims/admin/stats", claimsBody));

        var sut = BuildSut(handler);
        _repoMock.Setup(r => r.CreateReportAsync(It.IsAny<Report>())).ReturnsAsync(new Report { Id = 1 });
        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>())).ReturnsAsync(new AdminLog { Id = 1 });

        var result = await sut.GenerateReportAsync("Summary", adminId: 2);

        result.TotalUsers.Should().Be(20);
        result.TotalPolicies.Should().Be(12);
        result.TotalRevenue.Should().Be(60000m);
        result.TotalClaims.Should().Be(8);
    }

    // ── GetAdminLogs ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAdminLogs_ReturnsMappedLogList()
    {
        var logs = new List<AdminLog>
        {
            new() { Id = 1, AdminId = 1, Action = "UpdateClaimStatus", TargetType = "Claim",  TargetId = 10, Notes = "Approved", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, AdminId = 1, Action = "GenerateReport",    TargetType = "Report", TargetId = 5,  Notes = null,       CreatedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetLogsAsync()).ReturnsAsync(logs);

        var sut = BuildSut(SuccessHandler());
        var result = await sut.GetAdminLogsAsync();

        result.Should().HaveCount(2);
        result[0].Action.Should().Be("UpdateClaimStatus");
        result[1].Notes.Should().BeNull();
    }

    [Test]
    public async Task GetAdminLogs_WhenRepositoryEmpty_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetLogsAsync()).ReturnsAsync(new List<AdminLog>());

        var sut = BuildSut(SuccessHandler());
        var result = await sut.GetAdminLogsAsync();

        result.Should().BeEmpty();
    }

    // ── Configuration-driven URLs ─────────────────────────────────────────────

    [Test]
    public async Task GetDashboardSummary_UsesConfiguredServiceUrls()
    {
        // Verify it uses config values not hardcoded urls
        var requestedUrls = new List<string>();
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                requestedUrls.Add(req.RequestUri!.ToString());
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        var sut = BuildSut(handlerMock.Object);
        await sut.GetDashboardSummaryAsync();

        requestedUrls.Should().Contain(u => u.StartsWith("http://identity"));
        requestedUrls.Should().Contain(u => u.StartsWith("http://policy"));
        requestedUrls.Should().Contain(u => u.StartsWith("http://claims"));
    }

    // ── Notification Publisher ────────────────────────────────────────────────

    [Test]
    public async Task UpdateClaimStatus_AfterUpdate_PublishesNotificationWithCorrectStatus()
    {
        var claimDetail = JsonSerializer.Serialize(new
        {
            id = 5, claimNumber = "CLM-005", customerId = 7, status = "Submitted"
        });
        var userDetail = JsonSerializer.Serialize(new
        {
            id = 7, fullName = "Jane Doe", email = "jane@x.com",
            role = "CUSTOMER", isActive = true, createdAt = DateTime.UtcNow
        });

        var handler = JsonHandler(("/claims/5", claimDetail), ("/users/7", userDetail));

        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>()))
                 .ReturnsAsync(new AdminLog { Id = 1 });

        var sut = BuildSut(handler);
        var result = await sut.UpdateClaimStatusAsync(
            new UpdateClaimStatusDto { ClaimId = 5, Status = "Approved", AdminNote = "Verified" },
            adminId: 1);

        result.ClaimId.Should().Be(5);

        // Fire-and-forget: allow background task to complete
        await Task.Delay(300);

        _notificationPublisherMock.Verify(
            p => p.PublishClaimStatusChangedAsync(It.Is<ClaimStatusNotificationDto>(n =>
                n.ClaimId == 5 &&
                n.NewStatus == "Approved" &&
                n.ClaimNumber == "CLM-005" &&
                n.CustomerEmail == "jane@x.com")),
            Times.Once);
    }

    [Test]
    public async Task UpdateClaimStatus_WhenPublisherFails_StatusStillUpdated()
    {
        var claimDetail = JsonSerializer.Serialize(new
        {
            id = 10, claimNumber = "CLM-010", customerId = 3, status = "Submitted"
        });
        var userDetail = JsonSerializer.Serialize(new
        {
            id = 3, fullName = "Bob", email = "bob@x.com",
            role = "CUSTOMER", isActive = true, createdAt = DateTime.UtcNow
        });

        var handler = JsonHandler(("/claims/10", claimDetail), ("/users/3", userDetail));

        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>()))
                 .ReturnsAsync(new AdminLog { Id = 1 });

        // Publisher is set to throw — should not bubble up due to fire-and-forget swallowing exceptions
        _notificationPublisherMock
            .Setup(p => p.PublishClaimStatusChangedAsync(It.IsAny<ClaimStatusNotificationDto>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection failed"));

        var sut = BuildSut(handler);
        var result = await sut.UpdateClaimStatusAsync(
            new UpdateClaimStatusDto { ClaimId = 10, Status = "Rejected", AdminNote = "Denied" },
            adminId: 2);

        // Main response succeeds even though publisher threw
        result.ClaimId.Should().Be(10);
    }

    [Test]
    public async Task UpdateClaimStatus_WhenClaimDetailsMissing_SkipsNotification()
    {
        // Handler returns 404 for claims (so claimNumber is empty → notification skipped)
        var sut = BuildSut(ErrorHandler());

        _repoMock.Setup(r => r.CreateLogAsync(It.IsAny<AdminLog>()))
                 .ReturnsAsync(new AdminLog { Id = 1 });

        // PUT also fails → exception expected
        Func<Task> act = () => sut.UpdateClaimStatusAsync(
            new UpdateClaimStatusDto { ClaimId = 99, Status = "Approved" },
            adminId: 1);

        await act.Should().ThrowAsync<Exception>();

        // Publisher was never called because status update itself failed
        _notificationPublisherMock.Verify(
            p => p.PublishClaimStatusChangedAsync(It.IsAny<ClaimStatusNotificationDto>()),
            Times.Never);
    }
}
