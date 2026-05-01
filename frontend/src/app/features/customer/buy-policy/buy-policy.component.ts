import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../core/services/policy.service';
import { RazorpayService } from '../../../core/services/razorpay.service';
import { TokenService } from '../../../core/services/token.service';
import { CreatePolicyRequest, PolicyType, PremiumResponse } from '../../../core/models/policy.models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-buy-policy',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, DatePipe, CurrencyPipe, DecimalPipe],
  templateUrl: './buy-policy.component.html'
})
export class BuyPolicyComponent implements OnInit {
  private readonly policyService  = inject(PolicyService);
  private readonly razorpayService = inject(RazorpayService);
  private readonly tokenService   = inject(TokenService);
  private readonly router         = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly cdr            = inject(ChangeDetectorRef);

  step = 1;
  isCalculating = false;
  isPurchasing  = false;
  policyTypes: PolicyType[] = [];
  selectedPolicyType: PolicyType | null = null;
  premium: PremiumResponse | null = null;

  policyTypeId: number | null = null;
  age       = 18;
  startDate = '';
  endDate   = '';

  readonly minStartDate: string = (() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  })();

  ngOnInit(): void {
    this.policyService.getPolicyTypes().subscribe({
      next: (types) => {
        this.policyTypes = types.filter(t => t.isActive);
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  onPolicyTypeChange(): void {
    this.selectedPolicyType = this.policyTypes.find(t => t.id === Number(this.policyTypeId)) || null;
  }

  calculatePremium(): void {
    if (!this.policyTypeId || !this.startDate || !this.endDate) {
      this.messageService.add({ severity: 'warn', summary: 'Missing data', detail: 'Fill all required fields before premium calculation.' });
      return;
    }
    if (this.age < 18) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid age', detail: 'Age must be at least 18 to purchase a policy.' });
      return;
    }
    if (this.age > 100) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid age', detail: 'Age must be 100 or below to purchase a policy.' });
      return;
    }
    if (this.startDate <= new Date().toISOString().split('T')[0]) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid start date', detail: 'Start date must be after today.' });
      return;
    }
    if (this.endDate <= this.startDate) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid end date', detail: 'End date must be after start date.' });
      return;
    }

    this.isCalculating = true;
    this.policyService.calculatePremium({
      policyTypeId: this.policyTypeId,
      age: this.age,
      startDate: this.startDate,
      endDate: this.endDate
    }).subscribe({
      next: (result) => {
        this.premium       = result;
        this.step          = 2;
        this.isCalculating = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.isCalculating = false;
        this.messageService.add({ severity: 'error', summary: 'Calculation failed', detail: error?.error?.message || 'Unable to calculate premium.' });
        this.cdr.detectChanges();
      }
    });
  }

  nextStep(): void {
    if (this.step < 3) this.step++;
  }

  prevStep(): void {
    if (this.step > 1) this.step--;
  }

  initiatePayment(): void {
    if (!this.selectedPolicyType || !this.premium) return;

    const finalAmount   = this.premium.finalAmount;
    const customerName  = this.tokenService.getUserName() || 'Customer';
    const customerEmail = this.tokenService.getEmail()    || '';

    this.isPurchasing = true;

    this.razorpayService.openPaymentModal({
      amount:        finalAmount,
      policyName:    this.selectedPolicyType.name,
      customerName:  customerName,
      customerEmail: customerEmail,
      description:   `${this.selectedPolicyType.name} - ${this.premium.durationYears} Year(s)`,
      onSuccess: (response) => {
        this.createPolicyAfterPayment(response.razorpay_payment_id);
      },
      onFailure: (error) => {
        console.error('Payment failed:', error);
        this.isPurchasing = false;
        this.messageService.add({ severity: 'error', summary: 'Payment failed', detail: error?.message || 'Payment was cancelled or failed.' });
        this.cdr.detectChanges();
      }
    });
  }

  createPolicyAfterPayment(razorpayPaymentId: string): void {
    const payload: CreatePolicyRequest = {
      policyTypeId: Number(this.policyTypeId),
      startDate:    this.startDate,
      endDate:      this.endDate,
      age:          this.age
    };

    this.policyService.createPolicy(payload).subscribe({
      next: (policy) => {
        this.isPurchasing = false;
        this.messageService.add({
          severity: 'success',
          summary:  'Policy Activated!',
          detail:   `Policy ${policy.policyNumber} created. Payment ID: ${razorpayPaymentId}`
        });
        void this.router.navigate(['/customer/policies']);
      },
      error: (err) => {
        console.error('Policy creation error:', err);
        this.isPurchasing = false;
        this.messageService.add({
          severity: 'warn',
          summary:  'Policy creation failed',
          detail:   `Payment successful (${razorpayPaymentId}) but policy creation failed. Contact support.`
        });
        this.cdr.detectChanges();
      }
    });
  }
}
