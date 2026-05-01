import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../core/services/policy.service';
import { Policy } from '../../../core/models/policy.models';

@Component({
  selector: 'app-policy-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, DatePipe, CurrencyPipe],
  templateUrl: './policy-list.component.html'
})
export class PolicyListComponent implements OnInit {
  private readonly policyService = inject(PolicyService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  statusFilter = 'All';
  policies: Policy[] = [];
  filteredPolicies: Policy[] = [];

  // Renewal modal state
  showRenewalModal    = false;
  selectedPolicyForRenewal: Policy | null = null;
  renewalAge   = 30;
  isRenewing   = false;

  ngOnInit(): void {
    this.loadPolicies();
  }

  loadPolicies(): void {
    this.isLoading = true;
    this.policyService.getMyPolicies().subscribe({
      next: (policies) => {
        this.policies = policies;
        this.applyFilter();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilter(): void {
    if (this.statusFilter === 'All') {
      this.filteredPolicies = [...this.policies];
      return;
    }
    this.filteredPolicies = this.policies.filter(p => (p.status || '').toLowerCase() === this.statusFilter.toLowerCase());
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
    if (key === 'active')      return 'badge-active';
    if (key === 'draft')       return 'badge-draft';
    if (key === 'submitted')   return 'badge-submitted';
    if (key === 'underreview') return 'badge-underreview';
    if (key === 'approved')    return 'badge-approved';
    if (key === 'rejected')    return 'badge-rejected';
    if (key === 'expired')     return 'badge-rejected';
    return 'badge-closed';
  }

  openRenewalModal(policy: Policy): void {
    this.selectedPolicyForRenewal = policy;
    this.showRenewalModal = true;
  }

  renewPolicy(): void {
    if (!this.selectedPolicyForRenewal) return;
    this.isRenewing = true;

    this.policyService.renewPolicy(this.selectedPolicyForRenewal.id, this.renewalAge).subscribe({
      next: (result) => {
        this.isRenewing       = false;
        this.showRenewalModal = false;
        alert(`✅ Policy renewed successfully!\nNew expiry: ${new Date(result.newEndDate).toLocaleDateString()}\nNew Premium: ₹${result.newPremiumAmount}\nTransaction: ${result.transactionId}`);
        this.loadPolicies();
      },
      error: (err) => {
        console.error('Renewal error:', err);
        this.isRenewing = false;
        alert('Renewal failed: ' + (err.error?.message || 'Please try again'));
      }
    });
  }
}
