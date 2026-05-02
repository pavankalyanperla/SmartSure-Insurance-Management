using FluentAssertions;
using Moq;
using NUnit.Framework;
using PolicyService.Application.DTOs;
using PolicyService.Application.Exceptions;
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

    // ── CalculatePremium – age factors (additive formula) ────────────────────

    [Test]
    public async Task CalculatePremium_AgeUnder25_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 21,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 1, 1)
        });

        result.AgeFactor.Should().Be(0.10m);
        result.AgeGroup.Should().Be("18-25 years");
        result.BaseAmount.Should().Be(5000);
        result.AgeFactorAmount.Should().Be(500m);
        result.DurationYears.Should().Be(1);
        result.DurationFactor.Should().Be(0.10m);
        result.DurationFactorAmount.Should().Be(500m);
        result.FinalAmount.Should().Be(6000m);
        result.FormulaExplanation.Should().Contain("6000");
    }

    [Test]
    public async Task CalculatePremium_AgeBetween25And40_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 1, 1)
        });

        result.AgeFactor.Should().Be(0.00m);
        result.AgeGroup.Should().Be("26-40 years");
        result.AgeFactorAmount.Should().Be(0m);
        result.FinalAmount.Should().Be(5500m);
    }

    [Test]
    public async Task CalculatePremium_AgeBetween40And55_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 50,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 1, 1)
        });

        result.AgeFactor.Should().Be(0.20m);
        result.AgeGroup.Should().Be("41-55 years");
        result.AgeFactorAmount.Should().Be(1000m);
        result.FinalAmount.Should().Be(6500m);
    }

    [Test]
    public async Task CalculatePremium_AgeOver55_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 60,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 1, 1)
        });

        result.AgeFactor.Should().Be(0.50m);
        result.AgeGroup.Should().Be("56+ years");
        result.AgeFactorAmount.Should().Be(2500m);
        result.FinalAmount.Should().Be(8000m);
    }

    // ── CalculatePremium – duration factors ───────────────────────────────────

    [Test]
    public async Task CalculatePremium_Duration1Year_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 1, 1)
        });

        result.DurationYears.Should().Be(1);
        result.DurationFactor.Should().Be(0.10m);
        result.DurationFactorAmount.Should().Be(500m);
    }

    [Test]
    public async Task CalculatePremium_Duration2Years_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2027, 1, 1)
        });

        result.DurationYears.Should().Be(2);
        result.DurationFactor.Should().Be(1.10m);
        result.DurationFactorAmount.Should().Be(5500m);
        result.FinalAmount.Should().Be(10500m);
    }

    [Test]
    public async Task CalculatePremium_Duration3Years_AppliesCorrectFactor()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 5000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2028, 1, 1)
        });

        result.DurationYears.Should().Be(3);
        result.DurationFactor.Should().Be(2.10m);
        result.DurationFactorAmount.Should().Be(10500m);
        result.FinalAmount.Should().Be(15500m);
    }

    [Test]
    public async Task CalculatePremium_Duration9Months_StillCountsAs1Year()
    {
        var pt = new PolicyType { Id = 1, BaseAmount = 4000 };
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);

        var result = await _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 1, Age = 30,
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 10, 1)
        });

        result.DurationYears.Should().Be(1);
        result.DurationFactor.Should().Be(0.10m);
    }

    [Test]
    public async Task CalculatePremium_WithNonExistentPolicyType_ThrowsPolicyTypeNotFoundException()
    {
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(99)).ReturnsAsync((PolicyType?)null);

        Func<Task> act = () => _sut.CalculatePremiumAsync(new PremiumCalculationDto
        {
            PolicyTypeId = 99, Age = 30, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6)
        });

        await act.Should().ThrowAsync<PolicyTypeNotFoundException>()
                 .WithMessage("*99*");
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
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 1)
        });

        result.FinalAmount.Should().Be(3666.30m);
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
    public async Task CreatePolicy_WithNonExistentPolicyType_ThrowsPolicyTypeNotFoundException()
    {
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(77)).ReturnsAsync((PolicyType?)null);

        Func<Task> act = () => _sut.CreatePolicyAsync(1, new CreatePolicyDto
        {
            PolicyTypeId = 77, Age = 30,
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(3)
        });

        await act.Should().ThrowAsync<PolicyTypeNotFoundException>()
                 .WithMessage("*77*");
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
    public async Task UpdatePolicyStatus_WithNonExistentPolicy_ThrowsPolicyNotFoundException()
    {
        _repoMock.Setup(r => r.GetPolicyByIdAsync(999)).ReturnsAsync((Policy?)null);

        Func<Task> act = () => _sut.UpdatePolicyStatusAsync(999, "Expired");

        await act.Should().ThrowAsync<PolicyNotFoundException>()
                 .WithMessage("*999*");
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
    public async Task GetPaymentByPolicyId_WithNonExistentPolicy_ThrowsPaymentNotFoundException()
    {
        _repoMock.Setup(r => r.GetPaymentByPolicyIdAsync(999)).ReturnsAsync((Payment?)null);

        Func<Task> act = () => _sut.GetPaymentByPolicyIdAsync(999);

        await act.Should().ThrowAsync<PaymentNotFoundException>()
                 .WithMessage("*999*");
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

    // ── RenewPolicy ───────────────────────────────────────────────────────────

    [Test]
    public async Task RenewPolicy_WithExpiredPolicy_StartsFromToday()
    {
        var pt     = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000 };
        var policy = new Policy
        {
            Id = 10, UserId = 5, PolicyTypeId = 1,
            PolicyNumber = "POL-EXP", Status = PolicyStatus.Expired,
            EndDate = DateTime.UtcNow.AddDays(-10), PolicyType = pt
        };
        var payment = new Payment { Id = 1 };

        _repoMock.Setup(r => r.GetPolicyByIdAsync(10)).ReturnsAsync(policy);
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);
        _repoMock.Setup(r => r.UpdatePolicyAsync(It.IsAny<Policy>())).ReturnsAsync(policy);
        _repoMock.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(payment);

        var before = DateTime.UtcNow;
        var result = await _sut.RenewPolicyAsync(10, new RenewPolicyDto { Age = 30 }, userId: 5);
        var after  = DateTime.UtcNow;

        result.NewStartDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.NewEndDate.Should().BeCloseTo(result.NewStartDate.AddYears(1), TimeSpan.FromSeconds(5));
        result.PolicyNumber.Should().Be("POL-EXP");
        result.TransactionId.Should().StartWith("TXN-RENEW");
        _repoMock.Verify(r => r.UpdatePolicyAsync(It.Is<Policy>(p => p.IsRenewed && p.Status == PolicyStatus.Active)), Times.Once);
        _repoMock.Verify(r => r.CreatePaymentAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Test]
    public async Task RenewPolicy_WithActivePolicy_ExtendsDatesAndCreatesPayment()
    {
        var pt        = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000 };
        var oldEndDate = DateTime.UtcNow.AddDays(30);
        var policy    = new Policy
        {
            Id = 7, UserId = 3, PolicyTypeId = 1,
            PolicyNumber = "POL-ACT", Status = PolicyStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-335), EndDate = oldEndDate, PolicyType = pt
        };
        var payment = new Payment { Id = 2 };

        _repoMock.Setup(r => r.GetPolicyByIdAsync(7)).ReturnsAsync(policy);
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);
        _repoMock.Setup(r => r.UpdatePolicyAsync(It.IsAny<Policy>())).ReturnsAsync(policy);
        _repoMock.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(payment);

        var result = await _sut.RenewPolicyAsync(7, new RenewPolicyDto { Age = 30 }, userId: 3);

        result.NewEndDate.Should().BeCloseTo(oldEndDate.AddYears(1), TimeSpan.FromSeconds(5));
        result.RenewalCount.Should().Be(1);
        result.TransactionId.Should().StartWith("TXN-RENEW");
        _repoMock.Verify(r => r.CreatePaymentAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Test]
    public async Task RenewPolicy_WithCancelledPolicy_ThrowsPolicyNotRenewableException()
    {
        var pt     = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000 };
        var policy = new Policy { Id = 4, UserId = 1, PolicyTypeId = 1, Status = PolicyStatus.Cancelled, PolicyType = pt };
        _repoMock.Setup(r => r.GetPolicyByIdAsync(4)).ReturnsAsync(policy);

        Func<Task> act = () => _sut.RenewPolicyAsync(4, new RenewPolicyDto { Age = 30 }, userId: 1);

        await act.Should().ThrowAsync<PolicyNotRenewableException>()
                 .WithMessage("*Active or Expired*");
    }

    [Test]
    public async Task RenewPolicy_NonExistentPolicy_ThrowsPolicyNotFoundException()
    {
        _repoMock.Setup(r => r.GetPolicyByIdAsync(999)).ReturnsAsync((Policy?)null);

        Func<Task> act = () => _sut.RenewPolicyAsync(999, new RenewPolicyDto { Age = 30 }, userId: 1);

        await act.Should().ThrowAsync<PolicyNotFoundException>()
                 .WithMessage("*999*");
    }

    [Test]
    public async Task RenewPolicy_WithWrongUser_ThrowsPolicyAccessDeniedException()
    {
        var pt     = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000 };
        var policy = new Policy { Id = 5, UserId = 1, PolicyTypeId = 1, Status = PolicyStatus.Active, PolicyType = pt };
        _repoMock.Setup(r => r.GetPolicyByIdAsync(5)).ReturnsAsync(policy);

        Func<Task> act = () => _sut.RenewPolicyAsync(5, new RenewPolicyDto { Age = 30 }, userId: 2);

        await act.Should().ThrowAsync<PolicyAccessDeniedException>();
    }

    [Test]
    public async Task RenewPolicy_IncrementsRenewalCount()
    {
        var pt     = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000 };
        var policy = new Policy
        {
            Id = 8, UserId = 6, PolicyTypeId = 1,
            PolicyNumber = "POL-MULTI", Status = PolicyStatus.Expired,
            EndDate = DateTime.UtcNow.AddDays(-5), PolicyType = pt,
            RenewalCount = 2
        };
        var payment = new Payment { Id = 3 };

        _repoMock.Setup(r => r.GetPolicyByIdAsync(8)).ReturnsAsync(policy);
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);
        _repoMock.Setup(r => r.UpdatePolicyAsync(It.IsAny<Policy>())).ReturnsAsync(policy);
        _repoMock.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(payment);

        var result = await _sut.RenewPolicyAsync(8, new RenewPolicyDto { Age = 35 }, userId: 6);

        result.RenewalCount.Should().Be(3);
    }

    [Test]
    public async Task RenewPolicy_Uses1YearDurationFactor()
    {
        var pt     = new PolicyType { Id = 1, Name = "Health", BaseAmount = 5000 };
        var policy = new Policy
        {
            Id = 9, UserId = 7, PolicyTypeId = 1,
            PolicyNumber = "POL-DUR", Status = PolicyStatus.Expired,
            EndDate = DateTime.UtcNow.AddDays(-1), PolicyType = pt
        };
        var payment = new Payment { Id = 4 };

        _repoMock.Setup(r => r.GetPolicyByIdAsync(9)).ReturnsAsync(policy);
        _repoMock.Setup(r => r.GetPolicyTypeByIdAsync(1)).ReturnsAsync(pt);
        _repoMock.Setup(r => r.UpdatePolicyAsync(It.IsAny<Policy>())).ReturnsAsync(policy);
        _repoMock.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(payment);

        var result = await _sut.RenewPolicyAsync(9, new RenewPolicyDto { Age = 30 }, userId: 7);

        result.NewPremiumAmount.Should().Be(5500m);
    }
}
