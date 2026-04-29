import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PolicyService } from '../../../core/services/policy.service';
import { ClaimService } from '../../../core/services/claim.service';
import { Policy, Payment } from '../../../core/models/policy.models';
import { Claim } from '../../../core/models/claim.models';

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
  private readonly cdr = inject(ChangeDetectorRef);

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

    this.policyService.getPolicyById(id).subscribe({
      next: (policy) => {
        this.policy = policy;
        this.isLoading = false;
        this.cdr.detectChanges();

        this.policyService.getPaymentByPolicyId(id).subscribe({
          next: (payment) => { this.payment = payment; this.cdr.detectChanges(); },
          error: () => {}
        });

        this.claimService.getMyClaims().subscribe({
          next: (claims) => {
            this.relatedClaims = claims.filter(c => c.policyId === policy.id);
            this.cdr.detectChanges();
          },
          error: () => {}
        });
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
