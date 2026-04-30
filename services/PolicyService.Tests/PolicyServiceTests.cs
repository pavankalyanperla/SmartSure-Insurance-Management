using FluentAssertions;
using Moq;
using NUnit.Framework;
using PolicyService.Application.DTOs;
using PolicyService.Application.Services;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;

namespace PolicyService.Tests;

[TestFixture]
public class PolicyServiceTests
{
    private Mock<IPolicyRepository> _repoMock = null!;
    private PolicyAppService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IPolicyRepository>(MockBehavior.Strict);
        _sut = new PolicyAppService(_repoMock.Object);
    }

    // ── GetAllPolicyTypes ─────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPolicyTypes_ReturnsAllTypes()
    {
        var types = new List<PolicyType>
        {
            new() { Id = 1, Name = "Health", BaseAmount = 5000, IsActive = true },
            new() { Id = 2, Name = "Life",   BaseAmount = 8000, IsActive = true },
            new() { Id = 3, Name = "Auto",   BaseAmount = 3000, IsActive = false }
        };
        _repoMock.Setup(r => r.GetAllPolicyTypesAsync()).ReturnsAsync(types);

        var result = await _sut.GetAllPolicyTypesAsync();

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Health");
        result[2].IsActive.Should().BeFalse();
    }

    // ── GetPolicyTypeById ─────────────────────────────────────────────────────

    [Test]
    public async Task GetPolicyTypeById_WithValidId_ReturnsPolicyType()
    {
        var pt = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000, IsActive = true };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.GetPolicyTypeByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Health");
        result.BaseAmount.Should().Be(5000);
    }

    [Test]
    public async Task GetPolicyTypeById_WithInvalidId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(999)).ReturnsAsync((PolicyType?)null);

        var result = await _sut.GetPolicyTypeByIdAsync(999);

        result.Should().BeNull();
    }

    // ── CalculatePremium – age factors ────────────────────────────────────────

    [Test]
    public async Task CalculatePremium_AgeUnder25_AppliesAgeFactor_1_1()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var start = new DateTime(2025, 1, 1);
        var end   = start.AddMonths(3); // 3 months → durationFactor 1.0

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 20, StartDate = start, EndDate = end
        });

        result.AgeFactor.Should().Be(1.1m);
        result.BaseAmount.Should().Be(5000);
        result.FinalAmount.Should().Be(Math.Round(5000 * 1.1m * 1.0m, 2));
    }

    [Test]
    public async Task CalculatePremium_AgeBetween25And40_AppliesAgeFactor_1_0()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 1)
        });

        result.AgeFactor.Should().Be(1.0m);
    }

    [Test]
    public async Task CalculatePremium_AgeBetween41And55_AppliesAgeFactor_1_2()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 50, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 1)
        });

        result.AgeFactor.Should().Be(1.2m);
    }

    [Test]
    public async Task CalculatePremium_AgeOver55_AppliesAgeFactor_1_5()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 60, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 1)
        });

        result.AgeFactor.Should().Be(1.5m);
    }

    // ── CalculatePremium – duration factors ───────────────────────────────────

    [Test]
    public async Task CalculatePremium_DurationUpTo6Months_AppliesDurationFactor_1_0()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 4000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 1) // 3 months
        });

        result.DurationFactor.Should().Be(1.0m);
    }

    [Test]
    public async Task CalculatePremium_Duration6To12Months_AppliesDurationFactor_1_05()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 4000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 10, 1) // 9 months
        });

        result.DurationFactor.Should().Be(1.05m);
    }

    [Test]
    public async Task CalculatePremium_Duration12To24Months_AppliesDurationFactor_1_1()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 4000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 7, 1) // 18 months
        });

        result.DurationFactor.Should().Be(1.1m);
    }

    [Test]
    public async Task CalculatePremium_DurationOver24Months_AppliesDurationFactor_1_2()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 4000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2028, 1, 1) // 36 months
        });

        result.DurationFactor.Should().Be(1.2m);
    }

    [Test]
    public async Task CalculatePremium_WithNonExistentPolicyType_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(99)).ReturnsAsync((PolicyType?)null);

        Func<Task> act = () => _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 99, Age = 30, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6)
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── CreatePolicy ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePolicy_WithValidData_CreatesPolicyAndPaymentRecord()
    {
        var pt = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000, IsActive = true };
        var policyEntity = new Policy
        {
            Id = 10, UserId = 1, PolicyTypeId = 1, PolicyNumber = "POL-123",
            Status = PolicyStatus.Active, PremiumAmount = 5500,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 7, 1),
            PolicyType = pt,
            Premium = new Premium { BaseAmount = 5000, AgeFactor = 1.1m, DurationFactor = 1.0m, FinalAmount = 5500 }
        };
        var paymentEntity = new Payment { Id = 1, PolicyId = 10, Amount = 5500, TransactionId = "TXN-001" };

        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);
        _repoMock.Setup(r => r.CreatePolicyAsync(It.IsAny<Policy>())).ReturnsAsync(policyEntity);
        _repoMock.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(paymentEntity);

        var result = await _sut.CreatePolicyAsync(1, new CreatePolicyDto
        {
            PolicyTypeId = 1, Age = 20,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 7, 1)
        });

        result.PolicyNumber.Should().Be("POL-123");
        result.Status.Should().Be("Active");
        _repoMock.Verify(r => r.CreatePaymentAsync(It.Is<Payment>(p => p.PolicyId == 10)), Times.Once);
    }

    [Test]
    public async Task CreatePolicy_WithNonExistentPolicyType_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(77)).ReturnsAsync((PolicyType?)null);

        Func<Task> act = () => _sut.CreatePolicyAsync(1, new CreatePolicyDto
        {
            PolicyTypeId = 77, Age = 30,
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(3)
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── GetMyPolicies ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetMyPolicies_ReturnsOnlyPoliciesForGivenUser()
    {
        var pt = new PolicyType { Id = 1, Name = "Health" };
        var policies = new List<Policy>
        {
            new() { Id = 1, UserId = 5, PolicyNumber = "POL-A", PolicyType = pt, Status = PolicyStatus.Active },
            new() { Id = 2, UserId = 5, PolicyNumber = "POL-B", PolicyType = pt, Status = PolicyStatus.Active },
            new() { Id = 3, UserId = 5, PolicyNumber = "POL-C", PolicyType = pt, Status = PolicyStatus.Expired }
        };
        _repoMock.Setup(r => r.GetPoliciesByUserIdAsync(5)).ReturnsAsync(policies);

        var result = await _sut.GetMyPoliciesAsync(5);

        result.Should().HaveCount(3);
        result.All(p => p.PolicyNumber.StartsWith("POL-")).Should().BeTrue();
    }

    [Test]
    public async Task GetMyPolicies_WhenUserHasNoPolicies_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetPoliciesByUserIdAsync(999)).ReturnsAsync(new List<Policy>());

        var result = await _sut.GetMyPoliciesAsync(999);

        result.Should().BeEmpty();
    }

    // ── GetPolicyById ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetPolicyById_WithValidId_ReturnsPolicy()
    {
        var pt = new PolicyType { Id = 1, Name = "Life" };
        var policy = new Policy { Id = 5, UserId = 1, PolicyNumber = "POL-5", PolicyType = pt, Status = PolicyStatus.Active };
        _repoMock.Setup(r => r.GetPolicyByIdAsync(5)).ReturnsAsync(policy);

        var result = await _sut.GetPolicyByIdAsync(5);

        result.Should().NotBeNull();
        result!.PolicyNumber.Should().Be("POL-5");
    }

    [Test]
    public async Task GetPolicyById_WithInvalidId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetPolicyByIdAsync(999)).ReturnsAsync((Policy?)null);

        var result = await _sut.GetPolicyByIdAsync(999);

        result.Should().BeNull();
    }

    // ── UpdatePolicyStatus ────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePolicyStatus_WithValidStatus_UpdatesAndReturns()
    {
        var pt = new PolicyType { Id = 1, Name = "Health" };
        var policy = new Policy { Id = 3, PolicyType = pt, Status = PolicyStatus.Active };

        _repoMock.Setup(r => r.GetPolicyByIdAsync(3)).ReturnsAsync(policy);
        _repoMock.Setup(r => r.UpdatePolicyAsync(It.IsAny<Policy>())).ReturnsAsync(policy);

        var result = await _sut.UpdatePolicyStatusAsync(3, "Expired");

        result.Should().NotBeNull();
        _repoMock.Verify(r => r.UpdatePolicyAsync(It.Is<Policy>(p => p.Status == PolicyStatus.Expired)), Times.Once);
    }

    [Test]
    public async Task UpdatePolicyStatus_WithNonExistentPolicy_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetPolicyByIdAsync(999)).ReturnsAsync((Policy?)null);

        Func<Task> act = () => _sut.UpdatePolicyStatusAsync(999, "Expired");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── Counts and Revenue ────────────────────────────────────────────────────

    [Test]
    public async Task GetTotalPoliciesCount_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetTotalPoliciesCountAsync()).ReturnsAsync(15);

        var result = await _sut.GetTotalPoliciesCountAsync();

        result.Should().Be(15);
    }

    [Test]
    public async Task GetTotalRevenue_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(120000.50m);

        var result = await _sut.GetTotalRevenueAsync();

        result.Should().Be(120000.50m);
    }

    // ── Payment ───────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPaymentByPolicyId_WithValidId_ReturnsPayment()
    {
        var payment = new Payment { Id = 1, PolicyId = 5, Amount = 5500, TransactionId = "TXN-ABC", Status = "Success", PaymentMethod = "Online" };
        _repoMock.Setup(r => r.GetPaymentByPolicyIdAsync(5)).ReturnsAsync(payment);

        var result = await _sut.GetPaymentByPolicyIdAsync(5);

        result.TransactionId.Should().Be("TXN-ABC");
        result.Amount.Should().Be(5500);
    }

    [Test]
    public async Task GetPaymentByPolicyId_WithNonExistentPolicy_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.GetPaymentByPolicyIdAsync(999)).ReturnsAsync((Payment?)null);

        Func<Task> act = () => _sut.GetPaymentByPolicyIdAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Payment not found*");
    }

    [Test]
    public async Task GetMyPayments_ReturnsAllPaymentsForUser()
    {
        var payments = new List<Payment>
        {
            new() { Id = 1, PolicyId = 1, Amount = 5000, TransactionId = "TXN-1", Status = "Success", PaymentMethod = "Online" },
            new() { Id = 2, PolicyId = 2, Amount = 8000, TransactionId = "TXN-2", Status = "Success", PaymentMethod = "Online" }
        };
        _repoMock.Setup(r => r.GetPaymentsByUserIdAsync(3)).ReturnsAsync(payments);

        var result = await _sut.GetMyPaymentsAsync(3);

        result.Should().HaveCount(2);
        result[0].TransactionId.Should().Be("TXN-1");
    }

    // ── Final amount computation ──────────────────────────────────────────────

    [Test]
    public async Task CalculatePremium_FinalAmountIsRoundedToTwoDecimals()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 3333 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 1) // 3 months, factors 1.0/1.0
        });

        result.FinalAmount.Should().Be(3333.00m);
    }
}
