import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PolicyService } from '../../../core/services/policy.service';
import { ClaimService } from '../../../core/services/claim.service';
import { Policy, Payment } from '../../../core/models/policy.models';
import { Claim } from '../../../core/models/claim.models';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-policy-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, CurrencyPipe],
  templateUrl: './policy-detail.component.html'
})
export class PolicyDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly policyService = inject(PolicyService);
  private readonly claimService = inject(ClaimService);

  isLoading = true;
  policy: Policy | null = null;
  payment: Payment | null = null;
  relatedClaims: Claim[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading = false;
      return;
    }

    forkJoin({
      policy: this.policyService.getPolicyById(id),
      payment: this.policyService.getPaymentByPolicyId(id),
      claims: this.claimService.getMyClaims()
    }).subscribe({
      next: ({ policy, payment, claims }) => {
        this.policy = policy;
        this.payment = payment;
        this.relatedClaims = claims.filter((claim) => claim.policyId === policy.id);
      },
      complete: () => {
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
    if (key === 'active') {
      return 'badge-active';
    }
    if (key === 'draft') {
      return 'badge-draft';
    }
    if (key === 'submitted') {
      return 'badge-submitted';
    }
    if (key === 'underreview') {
      return 'badge-underreview';
    }
    if (key === 'approved') {
      return 'badge-approved';
    }
    if (key === 'rejected') {
      return 'badge-rejected';
    }
    return 'badge-closed';
  }
}