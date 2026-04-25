import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { PolicyService } from '../../../core/services/policy.service';
import { ClaimService } from '../../../core/services/claim.service';
import { Policy } from '../../../core/models/policy.models';
import { Claim } from '../../../core/models/claim.models';
import { forkJoin } from 'rxjs';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, CurrencyPipe],
  templateUrl: './customer-dashboard.component.html'
})
export class CustomerDashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly policyService = inject(PolicyService);
  private readonly claimService = inject(ClaimService);

  readonly userName = this.authService.getCurrentUserName();

  isLoading = true;
  policies: Policy[] = [];
  claims: Claim[] = [];

  get totalPolicies(): number {
    return this.policies.length;
  }

  get activePolicies(): number {
    return this.policies.filter((policy) => policy.status?.toLowerCase() === 'active').length;
  }

  get totalClaims(): number {
    return this.claims.length;
  }

  get pendingClaims(): number {
    return this.claims.filter((claim) => ['submitted', 'underreview'].includes((claim.status || '').toLowerCase())).length;
  }

  get recentPolicies(): Policy[] {
    return [...this.policies]
      .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
      .slice(0, 5);
  }

  ngOnInit(): void {
    forkJoin({
      policies: this.policyService.getMyPolicies(),
      claims: this.claimService.getMyClaims()
    }).subscribe({
      next: ({ policies, claims }) => {
        this.policies = policies;
        this.claims = claims;
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