import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { ClaimService } from '../../../core/services/claim.service';
import { ClaimReview } from '../../../core/models/admin.models';
import { Claim } from '../../../core/models/claim.models';
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
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  errorMessage: string | null = null;
  claims: ClaimReview[] = [];
  filteredClaims: ClaimReview[] = [];
  readonly fileBaseUrl = 'http://localhost:5084';

  selectedStatus = 'All';
  searchText = '';

  reviewPanelOpen = false;
  selectedClaim: ClaimReview | null = null;
  selectedClaimDetails: Claim | null = null;
  adminNote = '';
  actionLoading = false;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.adminService.getAllClaims().subscribe({
      next: (claims) => {
        this.claims = claims || [];
        this.filteredClaims = [...this.claims];
        this.isLoading = false;
        this.cdr.detectChanges();
        this.enrichCustomerNames();
      },
      error: (err) => {
        this.errorMessage = err?.name === 'TimeoutError'
          ? 'Request timed out. Make sure all services are running, then click Retry.'
          : `Failed to load claims (${err?.status ?? 'network error'}). Check that all services are running.`;
        this.claims = [];
        this.filteredClaims = [];
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private enrichCustomerNames(): void {
    this.adminService.getAllUsers().subscribe({
      next: (users) => {
        if (!users?.length) return;
        const userMap = new Map((users || []).map(u => [u.userId, u.fullName]));
        let changed = false;
        this.claims = this.claims.map(c => {
          const idMatch = c.customerName?.match(/Customer #(\d+)/);
          if (idMatch) {
            const name = userMap.get(parseInt(idMatch[1]));
            if (name) { changed = true; return { ...c, customerName: name }; }
          }
          return c;
        });
        if (changed) { this.applyFilters(); this.cdr.detectChanges(); }
      },
      error: () => {}
    });
  }

  applyFilters(): void {
    let filtered = [...this.claims];
    if (this.selectedStatus !== 'All') {
      filtered = filtered.filter(c => c.status === this.selectedStatus);
    }
    if (this.searchText.trim()) {
      const s = this.searchText.toLowerCase();
      filtered = filtered.filter(c =>
        c.claimNumber?.toLowerCase().includes(s) ||
        c.customerName?.toLowerCase().includes(s) ||
        c.description?.toLowerCase().includes(s)
      );
    }
    this.filteredClaims = filtered;
  }

  openReviewPanel(claim: ClaimReview): void {
    this.selectedClaim = claim;
    this.adminNote = claim.adminNote || '';
    this.reviewPanelOpen = true;
    this.claimService.getClaimById(claim.claimId).subscribe(details => {
      this.selectedClaimDetails = details;
      this.cdr.detectChanges();
    });
  }

  closeReview(): void {
    this.reviewPanelOpen = false;
    this.selectedClaim = null;
    this.selectedClaimDetails = null;
    this.adminNote = '';
  }

  moveToUnderReview(claim: ClaimReview): void {
    if ((claim.status || '').toLowerCase() !== 'submitted') return;
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
        this.actionLoading = false;
        this.messageService.add({ severity: 'success', summary: 'Claim updated', detail: `Status changed to ${status}.` });
        this.loadData();
        this.closeReview();
      },
      error: (error) => {
        this.actionLoading = false;
        this.messageService.add({ severity: 'error', summary: 'Update failed', detail: error?.error?.message || 'Could not update claim status.' });
      }
    });
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
    if (key === 'submitted') return 'badge-submitted';
    if (key === 'underreview') return 'badge-underreview';
    if (key === 'approved') return 'badge-approved';
    if (key === 'rejected') return 'badge-rejected';
    return 'badge-closed';
  }

  canModerate(status: string): boolean {
    const key = (status || '').toLowerCase();
    return key === 'submitted' || key === 'underreview';
  }

  fileIcon(fileType: string): string {
    const lower = (fileType || '').toLowerCase();
    if (lower.includes('pdf')) return 'PDF';
    if (lower.includes('png') || lower.includes('jpg') || lower.includes('jpeg')) return 'IMG';
    return 'FILE';
  }

  formatFileSize(size: number): string {
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }
}
