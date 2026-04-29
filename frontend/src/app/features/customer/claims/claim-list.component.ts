import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimService } from '../../../core/services/claim.service';
import { Claim } from '../../../core/models/claim.models';

@Component({
  selector: 'app-claim-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './claim-list.component.html'
})
export class ClaimListComponent implements OnInit {
  private readonly claimService = inject(ClaimService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  claims: Claim[] = [];

  ngOnInit(): void {
    this.claimService.getMyClaims().subscribe({
      next: (claims) => {
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
    if (key === 'draft') return 'badge-draft';
    if (key === 'submitted') return 'badge-submitted';
    if (key === 'underreview') return 'badge-underreview';
    if (key === 'approved') return 'badge-approved';
    if (key === 'rejected') return 'badge-rejected';
    return 'badge-closed';
  }
}
