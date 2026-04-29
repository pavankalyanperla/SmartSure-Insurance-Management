import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { PolicyService } from '../../../core/services/policy.service';
import { ClaimService } from '../../../core/services/claim.service';
import { Policy } from '../../../core/models/policy.models';
import { Claim } from '../../../core/models/claim.models';

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
  private readonly cdr = inject(ChangeDetectorRef);

  readonly userName = this.authService.getCurrentUserName();

  isLoading = true;
  policies: Policy[] = [];
  claims: Claim[] = [];

  get totalPolicies(): number { return this.policies.length; }
  get activePolicies(): number { return this.policies.filter(p => p.status?.toLowerCase() === 'active').length; }
  get totalClaims(): number { return this.claims.length; }
  get pendingClaims(): number { return this.claims.filter(c => ['submitted', 'underreview'].includes((c.status || '').toLowerCase())).length; }
  get recentPolicies(): Policy[] {
    return [...this.policies]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
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
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
    if (key === 'active') return 'badge-active';
    if (key === 'draft') return 'badge-draft';
    if (key === 'submitted') return 'badge-submitted';
    if (key === 'underreview') return 'badge-underreview';
    if (key === 'approved') return 'badge-approved';
    if (key === 'rejected') return 'badge-rejected';
    return 'badge-closed';
  }
}
