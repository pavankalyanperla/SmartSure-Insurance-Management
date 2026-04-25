import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PolicyService } from '../../../core/services/policy.service';
import { Policy } from '../../../core/models/policy.models';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-policy-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, DatePipe, CurrencyPipe],
  templateUrl: './policy-list.component.html'
})
export class PolicyListComponent implements OnInit {
  private readonly policyService = inject(PolicyService);

  isLoading = true;
  statusFilter = 'All';
  policies: Policy[] = [];
  filteredPolicies: Policy[] = [];

  ngOnInit(): void {
    this.policyService.getMyPolicies().subscribe({
      next: (policies) => {
        this.policies = policies;
        this.applyFilter();
      },
      complete: () => {
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    if (this.statusFilter === 'All') {
      this.filteredPolicies = [...this.policies];
      return;
    }
    this.filteredPolicies = this.policies.filter((policy) => (policy.status || '').toLowerCase() === this.statusFilter.toLowerCase());
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