import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClaimService } from '../../../core/services/claim.service';
import { Claim } from '../../../core/models/claim.models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './claim-detail.component.html'
})
export class ClaimDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly claimService = inject(ClaimService);
  private readonly messageService = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);

  claim: Claim | null = null;
  isLoading = true;
  isSubmitting = false;
  readonly fileBaseUrl = 'http://localhost:5084';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading = false;
      return;
    }

    this.claimService.getClaimById(id).subscribe({
      next: (claim) => {
        this.claim = claim;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  submitClaim(): void {
    if (!this.claim || (this.claim.status || '').toLowerCase() !== 'draft') return;

    this.isSubmitting = true;
    this.claimService.submitClaim(this.claim.id).subscribe({
      next: (claim) => {
        this.claim = claim;
        this.isSubmitting = false;
        this.messageService.add({ severity: 'success', summary: 'Submitted', detail: 'Claim submitted successfully' });
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  adminNoteClass(status: string): string {
    const key = (status || '').toLowerCase();
    if (key === 'approved') return 'admin-note-approved';
    if (key === 'rejected') return 'admin-note-rejected';
    return 'admin-note-info';
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
    if (key === 'draft') return 'badge-draft';
    if (key === 'submitted') return 'badge-submitted';
    if (key === 'underreview') return 'badge-underreview';
    if (key === 'approved') return 'badge-approved';
    if (key === 'rejected') return 'badge-rejected';
    return 'badge-closed';
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
