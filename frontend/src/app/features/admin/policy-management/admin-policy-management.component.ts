import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../core/services/policy.service';
import { PolicyType } from '../../../core/models/policy.models';
import { MessageService } from 'primeng/api';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-admin-policy-management',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe],
  templateUrl: './admin-policy-management.component.html'
})
export class AdminPolicyManagementComponent implements OnInit {
  private readonly policyService = inject(PolicyService);
  private readonly messageService = inject(MessageService);

  isLoading = true;
  policies: PolicyType[] = [];
  filteredPolicies: PolicyType[] = [];

  search = '';
  statusFilter = 'All';

  createOpen = false;
  editOpen = false;
  statsOpen = false;

  selectedPolicy: PolicyType | null = null;
  statsData: Record<string, unknown> | null = null;

  form: Partial<PolicyType> = this.emptyForm();

  ngOnInit(): void {
    this.loadPolicies();
  }

  loadPolicies(): void {
    this.isLoading = true;
    this.policyService.getAdminPolicyTypes().subscribe({
      next: (policies) => {
        this.policies = policies;
        this.applyFilters();
      },
      complete: () => {
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    const term = this.search.trim().toLowerCase();
    this.filteredPolicies = this.policies.filter((policy) => {
      const statusMatch = this.statusFilter === 'All' || (this.statusFilter === 'Active' ? policy.isActive : !policy.isActive);
      const textMatch = term.length === 0 || policy.name.toLowerCase().includes(term) || (policy.description || '').toLowerCase().includes(term);
      return statusMatch && textMatch;
    });
  }

  openCreate(): void {
    this.form = this.emptyForm();
    this.createOpen = true;
  }

  openEdit(policy: PolicyType): void {
    this.selectedPolicy = policy;
    this.form = { ...policy };
    this.editOpen = true;
  }

  openStats(policy: PolicyType): void {
    this.selectedPolicy = policy;
    this.statsOpen = true;
    this.policyService.getPolicyTypeStats(policy.id).subscribe((stats) => {
      this.statsData = stats as Record<string, unknown>;
    });
  }

  closePanels(): void {
    this.createOpen = false;
    this.editOpen = false;
    this.statsOpen = false;
    this.selectedPolicy = null;
    this.statsData = null;
  }

  createPolicy(): void {
    this.policyService.createPolicyType(this.form).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Created', detail: 'Policy type created successfully.' });
        this.closePanels();
        this.loadPolicies();
      }
    });
  }

  updatePolicy(): void {
    if (!this.selectedPolicy) {
      return;
    }
    this.policyService.updatePolicyType(this.selectedPolicy.id, this.form).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Updated', detail: 'Policy type updated successfully.' });
        this.closePanels();
        this.loadPolicies();
      }
    });
  }

  deletePolicy(policy: PolicyType): void {
    this.policyService.deletePolicyType(policy.id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Deleted', detail: 'Policy type deleted successfully.' });
        this.loadPolicies();
      }
    });
  }

  togglePolicy(policy: PolicyType): void {
    this.policyService.togglePolicyTypeStatus(policy.id, !policy.isActive).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Updated', detail: `Policy type ${policy.isActive ? 'deactivated' : 'activated'}.` });
        this.loadPolicies();
      }
    });
  }

  get activeCount(): number {
    return this.policies.filter((policy) => policy.isActive).length;
  }

  private emptyForm(): Partial<PolicyType> {
    return {
      name: '',
      description: '',
      baseAmount: 0,
      coverageDetails: '',
      durationMonths: 12,
      minAge: 18,
      maxAge: 100,
      claimLimit: 0,
      exclusions: '',
      waitingPeriod: 0,
      autoRenewal: false,
      gracePeriodDays: 0,
      riskCategory: ''
    };
  }
}