import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
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

  claim: Claim | null = null;
  isLoading = true;
  isSubmitting = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading = false;
      return;
    }

    this.claimService.getClaimById(id).subscribe({
      next: (claim) => {
        this.claim = claim;
      },
      complete: () => {
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  submitClaim(): void {
    if (!this.claim || (this.claim.status || '').toLowerCase() !== 'draft') {
      return;
    }

    this.isSubmitting = true;
    this.claimService.submitClaim(this.claim.id).subscribe({
      next: (claim) => {
        this.claim = claim;
        this.messageService.add({ severity: 'success', summary: 'Submitted', detail: 'Claim submitted successfully' });
      },
      complete: () => {
        this.isSubmitting = false;
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }

  statusClass(status: string): string {
    const key = (status || '').toLowerCase();
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

  fileIcon(fileType: string): string {
    const lower = (fileType || '').toLowerCase();
    if (lower.includes('pdf')) {
      return 'PDF';
    }
    if (lower.includes('png') || lower.includes('jpg') || lower.includes('jpeg')) {
      return 'IMG';
    }
    return 'FILE';
  }
}