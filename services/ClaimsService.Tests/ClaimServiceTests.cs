using System.Text.Json;
using ClaimsService.Application.DTOs;
using ClaimsService.Application.Services;
using ClaimsService.Domain.Entities;
using ClaimsService.Domain.Enums;
using ClaimsService.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ClaimsService.Tests;

[TestFixture]
public class ClaimServiceTests
{
    private Mock<IClaimRepository> _repoMock = null!;
    private ClaimAppService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IClaimRepository>(MockBehavior.Strict);
        _sut = new ClaimAppService(_repoMock.Object);
    }

    // ── CreateClaim ───────────────────────────────────────────────────────────

    [Test]
    public async Task CreateClaim_WithValidData_CreatesDraftClaimWithClmPrefix()
    {
        var created = new Claim
        {
            Id = 1, PolicyId = 10, CustomerId = 5,
            ClaimNumber = "CLM-12345", Status = ClaimStatus.Draft,
            Description = "Water damage", IncidentDate = DateTime.UtcNow.AddDays(-2),
            ClaimDocuments = new List<ClaimDocument>()
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Claim>())).ReturnsAsync(created);

        var result = await _sut.CreateClaimAsync(5, new CreateClaimDto
        {
            PolicyId = 10,
            IncidentDate = DateTime.UtcNow.AddDays(-2),
            Description = "Water damage"
        });

        result.Status.Should().Be("Draft");
        result.ClaimNumber.Should().StartWith("CLM-");
        result.CustomerId.Should().Be(5);
    }

    [Test]
    public async Task CreateClaim_DescriptionIsTrimmed()
    {
        Claim? capturedClaim = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Claim>()))
                 .Callback<Claim>(c => capturedClaim = c)
                 .ReturnsAsync((Claim c) =>
                 {
                     c.Id = 1;
                     c.ClaimDocuments = new List<ClaimDocument>();
                     return c;
                 });

        await _sut.CreateClaimAsync(1, new CreateClaimDto
        {
            PolicyId = 1, IncidentDate = DateTime.UtcNow, Description = "  fire damage  "
        });

        capturedClaim!.Description.Should().Be("fire damage");
    }

    [Test]
    public async Task CreateClaim_MultipleCallsProduceUniqueClaimNumbers()
    {
        var numbers = new HashSet<string>();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Claim>()))
                 .ReturnsAsync((Claim c) =>
                 {
                     c.Id = 1;
                     c.ClaimDocuments = new List<ClaimDocument>();
                     return c;
                 });

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(1); // ensure Ticks differ
            var r = await _sut.CreateClaimAsync(1, new CreateClaimDto
            {
                PolicyId = 1, IncidentDate = DateTime.UtcNow, Description = "desc"
            });
            numbers.Add(r.ClaimNumber);
        }

        numbers.Should().HaveCount(5);
    }

    // ── SubmitClaim ───────────────────────────────────────────────────────────

    [Test]
    public async Task SubmitClaim_FromDraftStatus_ChangesStatusToSubmitted()
    {
        var claim = new Claim { Id = 1, CustomerId = 5, Status = ClaimStatus.Draft, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>()))
                 .ReturnsAsync((Claim c) => c);

        var result = await _sut.SubmitClaimAsync(1, 5);

        result.Status.Should().Be("Submitted");
    }

    [Test]
    public async Task SubmitClaim_WhenClaimNotFound_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Claim?)null);

        Func<Task> act = () => _sut.SubmitClaimAsync(999, 1);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Test]
    public async Task SubmitClaim_FromAlreadySubmittedStatus_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 2, CustomerId = 5, Status = ClaimStatus.Submitted };
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.SubmitClaimAsync(2, 5);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already submitted*");
    }

    [Test]
    public async Task SubmitClaim_ByDifferentCustomer_ThrowsUnauthorized()
    {
        var claim = new Claim { Id = 3, CustomerId = 5, Status = ClaimStatus.Draft };
        _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.SubmitClaimAsync(3, customerId: 99);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── GetClaimById ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetClaimById_WithValidId_ReturnsClaim()
    {
        var claim = new Claim { Id = 5, CustomerId = 2, Status = ClaimStatus.Submitted, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(claim);

        var result = await _sut.GetClaimByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Status.Should().Be("Submitted");
    }

    [Test]
    public async Task GetClaimById_WithInvalidId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Claim?)null);

        var result = await _sut.GetClaimByIdAsync(999);

        result.Should().BeNull();
    }

    // ── GetMyClaims ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetMyClaims_ReturnsOnlyClaimsForGivenCustomer()
    {
        var claims = new List<Claim>
        {
            new() { Id = 1, CustomerId = 7, Status = ClaimStatus.Draft,     ClaimDocuments = new List<ClaimDocument>() },
            new() { Id = 2, CustomerId = 7, Status = ClaimStatus.Submitted, ClaimDocuments = new List<ClaimDocument>() },
            new() { Id = 3, CustomerId = 7, Status = ClaimStatus.Approved,  ClaimDocuments = new List<ClaimDocument>() }
        };
        _repoMock.Setup(r => r.GetByCustomerIdAsync(7)).ReturnsAsync(claims);

        var result = await _sut.GetMyClaimsAsync(7);

        result.Should().HaveCount(3);
        result.All(c => c.CustomerId == 7).Should().BeTrue();
    }

    // ── GetAllClaims ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllClaims_ReturnsEveryClaimInSystem()
    {
        var claims = Enumerable.Range(1, 7).Select(i => new Claim
        {
            Id = i, CustomerId = i, Status = ClaimStatus.Draft, ClaimDocuments = new List<ClaimDocument>()
        }).ToList();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(claims);

        var result = await _sut.GetAllClaimsAsync();

        result.Should().HaveCount(7);
    }

    // ── UpdateClaimStatus – valid transitions ─────────────────────────────────

    [Test]
    public async Task UpdateStatus_SubmittedToUnderReview_Succeeds()
    {
        var claim = new Claim { Id = 1, Status = ClaimStatus.Submitted, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>())).ReturnsAsync((Claim c) => c);

        var result = await _sut.UpdateClaimStatusAsync(1, new UpdateClaimStatusDto { Status = "UnderReview", AdminNote = "reviewing" });

        result.Status.Should().Be("UnderReview");
    }

    [Test]
    public async Task UpdateStatus_UnderReviewToApproved_Succeeds()
    {
        var claim = new Claim { Id = 2, Status = ClaimStatus.UnderReview, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>())).ReturnsAsync((Claim c) => c);

        var result = await _sut.UpdateClaimStatusAsync(2, new UpdateClaimStatusDto { Status = "Approved", AdminNote = "verified" });

        result.Status.Should().Be("Approved");
    }

    [Test]
    public async Task UpdateStatus_UnderReviewToRejected_Succeeds()
    {
        var claim = new Claim { Id = 3, Status = ClaimStatus.UnderReview, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>())).ReturnsAsync((Claim c) => c);

        var result = await _sut.UpdateClaimStatusAsync(3, new UpdateClaimStatusDto { Status = "Rejected", AdminNote = "invalid" });

        result.Status.Should().Be("Rejected");
    }

    [Test]
    public async Task UpdateStatus_ApprovedToClosed_Succeeds()
    {
        var claim = new Claim { Id = 4, Status = ClaimStatus.Approved, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>())).ReturnsAsync((Claim c) => c);

        var result = await _sut.UpdateClaimStatusAsync(4, new UpdateClaimStatusDto { Status = "Closed" });

        result.Status.Should().Be("Closed");
    }

    [Test]
    public async Task UpdateStatus_RejectedToClosed_Succeeds()
    {
        var claim = new Claim { Id = 5, Status = ClaimStatus.Rejected, ClaimDocuments = new List<ClaimDocument>() };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>())).ReturnsAsync((Claim c) => c);

        var result = await _sut.UpdateClaimStatusAsync(5, new UpdateClaimStatusDto { Status = "Closed" });

        result.Status.Should().Be("Closed");
    }

    // ── UpdateClaimStatus – invalid transitions ───────────────────────────────

    [Test]
    public async Task UpdateStatus_DraftToApproved_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 6, Status = ClaimStatus.Draft };
        _repoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.UpdateClaimStatusAsync(6, new UpdateClaimStatusDto { Status = "Approved" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid status transition*");
    }

    [Test]
    public async Task UpdateStatus_ApprovedToSubmitted_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 7, Status = ClaimStatus.Approved };
        _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.UpdateClaimStatusAsync(7, new UpdateClaimStatusDto { Status = "Submitted" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid status transition*");
    }

    [Test]
    public async Task UpdateStatus_SubmittedToRejected_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 8, Status = ClaimStatus.Submitted };
        _repoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.UpdateClaimStatusAsync(8, new UpdateClaimStatusDto { Status = "Rejected" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid status transition*");
    }

    [Test]
    public async Task UpdateStatus_ClosedToAny_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 9, Status = ClaimStatus.Closed };
        _repoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.UpdateClaimStatusAsync(9, new UpdateClaimStatusDto { Status = "Approved" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid status transition*");
    }

    [Test]
    public async Task UpdateStatus_WithInvalidStatusString_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 10, Status = ClaimStatus.Submitted };
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.UpdateClaimStatusAsync(10, new UpdateClaimStatusDto { Status = "Banana" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid claim status*");
    }

    [Test]
    public async Task UpdateStatus_WhenClaimNotFound_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Claim?)null);

        Func<Task> act = () => _sut.UpdateClaimStatusAsync(999, new UpdateClaimStatusDto { Status = "Approved" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── AddDocument ───────────────────────────────────────────────────────────

    [Test]
    public async Task AddDocument_WithValidClaim_SavesDocumentAndReturnsDto()
    {
        var claim = new Claim { Id = 1, CustomerId = 1, Status = ClaimStatus.Draft, ClaimDocuments = new List<ClaimDocument>() };
        var doc = new ClaimDocument
        {
            Id = 10, ClaimId = 1, FileName = "receipt.pdf",
            FilePath = "wwwroot/uploads/receipt.pdf", FileType = "application/pdf", FileSize = 2048
        };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.AddDocumentAsync(It.IsAny<ClaimDocument>())).ReturnsAsync(doc);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>())).ReturnsAsync(claim);

        var result = await _sut.AddDocumentAsync(1, "receipt.pdf", "wwwroot/uploads/receipt.pdf", "application/pdf", 2048);

        result.FileName.Should().Be("receipt.pdf");
        result.FileType.Should().Be("application/pdf");
        result.FileSize.Should().Be(2048);
        result.FileUrl.Should().NotStartWith("wwwroot");
    }

    [Test]
    public async Task AddDocument_WhenClaimNotFound_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Claim?)null);

        Func<Task> act = () => _sut.AddDocumentAsync(999, "file.pdf", "/path/file.pdf", "application/pdf", 1024);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── DeleteDocument ────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteDocument_ByDifferentCustomer_ThrowsUnauthorized()
    {
        var claim = new Claim { Id = 1, CustomerId = 5, Status = ClaimStatus.Draft };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.DeleteDocumentAsync(1, 10, customerId: 99);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task DeleteDocument_OnNonDraftClaim_ThrowsInvalidOperation()
    {
        var claim = new Claim { Id = 1, CustomerId = 5, Status = ClaimStatus.Submitted };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(claim);

        Func<Task> act = () => _sut.DeleteDocumentAsync(1, 10, customerId: 5);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*draft*");
    }

    // ── GetClaimsStats ────────────────────────────────────────────────────────

    [Test]
    public async Task GetClaimsStats_ReturnsCorrectCounts()
    {
        _repoMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(10);
        _repoMock.Setup(r => r.GetCountByStatusAsync("Draft")).ReturnsAsync(2);
        _repoMock.Setup(r => r.GetCountByStatusAsync("Submitted")).ReturnsAsync(3);
        _repoMock.Setup(r => r.GetCountByStatusAsync("UnderReview")).ReturnsAsync(1);
        _repoMock.Setup(r => r.GetCountByStatusAsync("Approved")).ReturnsAsync(2);
        _repoMock.Setup(r => r.GetCountByStatusAsync("Rejected")).ReturnsAsync(1);
        _repoMock.Setup(r => r.GetCountByStatusAsync("Closed")).ReturnsAsync(1);

        var result = await _sut.GetClaimsStatsAsync();

        result.Should().NotBeNull();

        // Serialize to JSON and parse as dictionary to avoid cross-assembly dynamic binding issues
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("totalClaims").GetInt32().Should().Be(10);
        doc.RootElement.GetProperty("submittedClaims").GetInt32().Should().Be(3);
        doc.RootElement.GetProperty("approvedClaims").GetInt32().Should().Be(2);
    }

    // ── Documents in response ─────────────────────────────────────────────────

    [Test]
    public async Task GetClaimById_ClaimWithDocuments_ReturnsDocumentsInResponse()
    {
        var docs = new List<ClaimDocument>
        {
            new() { Id = 1, ClaimId = 5, FileName = "doc1.pdf", FilePath = "wwwroot/uploads/doc1.pdf", FileType = "pdf", FileSize = 1024 },
            new() { Id = 2, ClaimId = 5, FileName = "doc2.jpg", FilePath = "wwwroot/uploads/doc2.jpg", FileType = "jpg", FileSize = 2048 }
        };
        var claim = new Claim
        {
            Id = 5, CustomerId = 2, Status = ClaimStatus.Submitted,
            ClaimDocuments = docs
        };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(claim);

        var result = await _sut.GetClaimByIdAsync(5);

        result!.Documents.Should().HaveCount(2);
        result.Documents[0].FileName.Should().Be("doc1.pdf");
    }

    // ── AdminNote is saved ────────────────────────────────────────────────────

    [Test]
    public async Task UpdateStatus_AdminNoteIsSavedOnClaim()
    {
        var claim = new Claim { Id = 1, Status = ClaimStatus.Submitted, ClaimDocuments = new List<ClaimDocument>() };
        Claim? savedClaim = null;

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(claim);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Claim>()))
                 .Callback<Claim>(c => savedClaim = c)
                 .ReturnsAsync((Claim c) => c);

        await _sut.UpdateClaimStatusAsync(1, new UpdateClaimStatusDto { Status = "UnderReview", AdminNote = "Checking docs" });

        savedClaim!.AdminNote.Should().Be("Checking docs");
    }
}
