import { CommonModule, CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChartModule } from 'primeng/chart';
import { PolicyService } from '../../../core/services/policy.service';
import { PolicyType } from '../../../core/models/policy.models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-admin-policy-management',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, ChartModule],
  templateUrl: './admin-policy-management.component.html'
})
export class AdminPolicyManagementComponent implements OnInit {
  private readonly policyService = inject(PolicyService);
  private readonly messageService = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);

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

  doughnutData: any = null;
  barData: any = null;
  doughnutOptions: any = null;
  barOptions: any = null;

  form: Partial<PolicyType> = this.emptyForm();

  ngOnInit(): void {
    this.loadPolicies();
  }

  loadPolicies(): void {
    this.isLoading = true;
    this.policyService.getAdminPolicyTypes()
      .subscribe({
        next: (policies) => {
          this.policies = policies;
          this.applyFilters();
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.detectChanges();
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
    this.statsData = null;
    this.doughnutData = null;
    this.barData = null;
    this.statsOpen = true;
    this.policyService.getPolicyTypeStats(policy.id).subscribe({
      next: (raw) => {
        const stats = raw as Record<string, unknown>;
        this.statsData = stats;
        this.buildStatsCharts(stats);
        this.cdr.detectChanges();
      },
      error: () => {
        this.statsData = {};
        this.cdr.detectChanges();
      }
    });
  }

  private buildStatsCharts(stats: Record<string, unknown>): void {
    const total  = Number(stats['totalPolicies']  ?? 0);
    const active = Number(stats['activePolicies'] ?? 0);
    const inactive = total - active;
    const premium = Number(stats['totalPremium'] ?? 0);

    this.doughnutData = {
      labels: ['Active', 'Inactive'],
      datasets: [{
        data: [active, inactive],
        backgroundColor: ['#10b981', '#6b7280'],
        hoverBackgroundColor: ['#059669', '#4b5563'],
        borderWidth: 0
      }]
    };

    this.doughnutOptions = {
      cutout: '65%',
      plugins: {
        legend: { position: 'bottom', labels: { usePointStyle: true, padding: 16, font: { size: 12 } } }
      }
    };

    this.barData = {
      labels: ['Total Policies', 'Active', 'Inactive'],
      datasets: [{
        label: 'Count',
        data: [total, active, inactive],
        backgroundColor: ['#3b82f6', '#10b981', '#6b7280'],
        borderRadius: 6,
        borderSkipped: false
      }]
    };

    this.barOptions = {
      plugins: { legend: { display: false } },
      scales: {
        y: { beginAtZero: true, ticks: { precision: 0, font: { size: 11 } }, grid: { color: 'rgba(0,0,0,.05)' } },
        x: { grid: { display: false }, ticks: { font: { size: 11 } } }
      }
    };
  }

  closePanels(): void {
    this.createOpen = false;
    this.editOpen = false;
    this.statsOpen = false;
    this.selectedPolicy = null;
    this.statsData = null;
    this.doughnutData = null;
    this.barData = null;
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
    if (!this.selectedPolicy) return;
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
    const newActive = !policy.isActive;
    this.policyService.togglePolicyTypeStatus(policy.id, newActive).subscribe({
      next: () => {
        policy.isActive = newActive;
        this.messageService.add({ severity: 'success', summary: 'Updated', detail: `Policy type ${newActive ? 'activated' : 'deactivated'}.` });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not update policy status.' });
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
