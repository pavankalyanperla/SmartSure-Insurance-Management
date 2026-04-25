import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { ClaimService } from '../../../core/services/claim.service';
import { ClaimReview, AdminUser } from '../../../core/models/admin.models';
import { Claim } from '../../../core/models/claim.models';
import { DatePipe } from '@angular/common';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-admin-claims',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './admin-claims.component.html'
})
export class AdminClaimsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly claimService = inject(ClaimService);
  private readonly messageService = inject(MessageService);

  isLoading = true;
  claims: ClaimReview[] = [];
  filteredClaims: ClaimReview[] = [];
  customers: AdminUser[] = [];

  selectedStatus = 'All';
  selectedCustomer = 'All';
  searchText = '';

  reviewPanelOpen = false;
  selectedClaim: ClaimReview | null = null;
  selectedClaimDetails: Claim | null = null;
  adminNote = '';
  actionLoading = false;

  ngOnInit(): void {
    setTimeout(() => {
      this.loadClaims();
      this.loadCustomers();
    }, 0);
  }

  loadClaims(): void {
    this.isLoading = true;
    this.adminService.getAllClaims().subscribe({
      next: (data) => {
        this.claims = data || [];
        this.filteredClaims = [...this.claims];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Claims error:', err);
        this.claims = [];
        this.filteredClaims = [];
        this.isLoading = false;
      }
    });
  }

  loadCustomers(): void {
    this.adminService.getAllUsers().subscribe({
      next: (users) => {
        this.customers = users || [];
      },
      error: (err) => {
        console.error('Customers error:', err);
        this.customers = [];
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.claims];
    if (this.selectedStatus !== 'All') {
      filtered = filtered.filter((c) => c.status === this.selectedStatus);
    }
    if (this.selectedCustomer !== 'All') {
      filtered = filtered.filter((c) => c.customerName?.toLowerCase().includes(this.selectedCustomer.toLowerCase()));
    }
    if (this.searchText.trim()) {
      const s = this.searchText.toLowerCase();
      filtered = filtered.filter((c) =>
        c.claimNumber?.toLowerCase().includes(s) || c.customerName?.toLowerCase().includes(s) || c.description?.toLowerCase().includes(s)
      );
    }
    this.filteredClaims = filtered;
  }

  openReviewPanel(claim: ClaimReview): void {
    this.selectedClaim = claim;
    this.adminNote = claim.adminNote || '';
    this.reviewPanelOpen = true;
    this.claimService.getClaimById(claim.claimId).subscribe((details) => {
      this.selectedClaimDetails = details;
    });
  }

  closeReview(): void {
    this.reviewPanelOpen = false;
    this.selectedClaim = null;
    this.selectedClaimDetails = null;
    this.adminNote = '';
  }

  moveToUnderReview(claim: ClaimReview): void {
    if ((claim.status || '').toLowerCase() !== 'submitted') {
      return;
    }
    this.updateStatus(claim.claimId, 'UnderReview', this.adminNote || 'Moved to review');
  }

  approveClaim(claimId: number): void {
    this.updateStatus(claimId, 'Approved', this.adminNote || 'Approved by admin');
  }

  rejectClaim(claimId: number): void {
    if (!this.adminNote.trim()) {
      this.messageService.add({ severity: 'warn', summary: 'Admin note required', detail: 'Add an admin note before rejecting.' });
      return;
    }
    this.updateStatus(claimId, 'Rejected', this.adminNote.trim());
  }

  updateStatus(claimId: number, status: string, note: string): void {
    this.actionLoading = true;
    this.adminService.updateClaimStatus(claimId, status, note).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Claim updated', detail: `Status changed to ${status}.` });
        this.loadClaims();
        this.closeReview();
      },
      complete: () => {
        this.actionLoading = false;
      },
      error: (error) => {
        this.actionLoading = false;
        this.messageService.add({ severity: 'error', summary: 'Update failed', detail: error?.error?.message || 'Could not update claim status.' });
      }
    });
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
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

  canModerate(status: string): boolean {
    const key = (status || '').toLowerCase();
    return key === 'submitted' || key === 'underreview';
  }
}