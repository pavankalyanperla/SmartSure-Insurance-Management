import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PolicyService } from '../../../core/services/policy.service';
import { CreatePolicyRequest, PolicyType, PremiumResponse } from '../../../core/models/policy.models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-buy-policy',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, DatePipe, CurrencyPipe],
  templateUrl: './buy-policy.component.html'
})
export class BuyPolicyComponent implements OnInit {
  private readonly policyService = inject(PolicyService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);

  step = 1;
  isCalculating = false;
  isSubmitting = false;
  policyTypes: PolicyType[] = [];
  selectedPolicyType: PolicyType | null = null;
  premiumResponse: PremiumResponse | null = null;

  policyTypeId: number | null = null;
  age = 18;
  startDate = '';
  endDate = '';

  ngOnInit(): void {
    this.policyService.getPolicyTypes().subscribe((types) => {
      this.policyTypes = types.filter((type) => type.isActive);
    });
  }

  onPolicyTypeChange(): void {
    this.selectedPolicyType = this.policyTypes.find((type) => type.id === Number(this.policyTypeId)) || null;
  }

  calculatePremium(): void {
    if (!this.policyTypeId || !this.startDate || !this.endDate || this.age < 18 || this.age > 100) {
      this.messageService.add({ severity: 'warn', summary: 'Missing data', detail: 'Fill all required fields before premium calculation.' });
      return;
    }

    this.isCalculating = true;
    this.policyService
      .calculatePremium({
        policyTypeId: this.policyTypeId,
        age: this.age,
        startDate: this.startDate,
        endDate: this.endDate
      })
      .subscribe({
        next: (result) => {
          this.premiumResponse = result;
          this.step = 2;
        },
        complete: () => {
          this.isCalculating = false;
        },
        error: (error) => {
          this.isCalculating = false;
          this.messageService.add({ severity: 'error', summary: 'Calculation failed', detail: error?.error?.message || 'Unable to calculate premium.' });
        }
      });
  }

  continueToConfirm(): void {
    this.step = 3;
  }

  payAndActivate(): void {
    if (!this.policyTypeId || !this.startDate || !this.endDate) {
      return;
    }

    const payload: CreatePolicyRequest = {
      policyTypeId: this.policyTypeId,
      startDate: this.startDate,
      endDate: this.endDate,
      age: this.age
    };

    this.isSubmitting = true;
    this.policyService.createPolicy(payload).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Policy activated', detail: 'Payment successful and policy activated.' });
        void this.router.navigate(['/customer/policies']);
      },
      complete: () => {
        this.isSubmitting = false;
      },
      error: (error) => {
        this.isSubmitting = false;
        this.messageService.add({ severity: 'error', summary: 'Purchase failed', detail: error?.error?.message || 'Could not create policy.' });
      }
    });
  }

  previousStep(): void {
    if (this.step > 1) {
      this.step -= 1;
    }
  }

  get ageFactor(): number {
    return this.premiumResponse?.ageFactor || 1;
  }

  get durationFactor(): number {
    return this.premiumResponse?.durationFactor || 1;
  }

  get baseAmountFromSelection(): number {
    return this.selectedPolicyType?.baseAmount || 0;
  }

  get computedFinalPremium(): number {
    return this.baseAmountFromSelection * this.ageFactor * this.durationFactor;
  }
}