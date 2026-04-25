import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../core/services/policy.service';
import { ClaimService } from '../../../core/services/claim.service';
import { Policy } from '../../../core/models/policy.models';
import { Claim, ClaimDocument } from '../../../core/models/claim.models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-initiate-claim',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './initiate-claim.component.html'
})
export class InitiateClaimComponent implements OnInit {
  private readonly policyService = inject(PolicyService);
  private readonly claimService = inject(ClaimService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  step = 1;
  selectedPolicyId: number | null = null;
  incidentDate = '';
  description = '';
  draftClaim: Claim | null = null;
  uploadedDocs: ClaimDocument[] = [];
  selectedFiles: File[] = [];
  isSubmitting = false;
  isUploading = false;
  policies: Policy[] = [];
  todayDate = new Date().toISOString().split('T')[0];

  ngOnInit(): void {
    this.policyService.getMyPolicies().subscribe((policies) => {
      this.policies = policies.filter((policy) => (policy.status || '').toLowerCase() === 'active');
    });
  }

  createDraft(): void {
    if (!this.selectedPolicyId || !this.incidentDate || this.description.trim().length < 10) {
      this.messageService.add({ severity: 'warn', summary: 'Incomplete form', detail: 'Provide policy, incident date and at least 10 characters of description.' });
      return;
    }

    this.isSubmitting = true;
    this.claimService
      .createClaim({
        policyId: this.selectedPolicyId,
        incidentDate: this.incidentDate,
        description: this.description.trim()
      })
      .subscribe({
        next: (claim) => {
          this.draftClaim = claim;
          this.step = 2;
        },
        complete: () => {
          this.isSubmitting = false;
        },
        error: (error) => {
          this.isSubmitting = false;
          this.messageService.add({ severity: 'error', summary: 'Draft creation failed', detail: error?.error?.message || 'Could not create claim draft.' });
        }
      });
  }

  handleFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);
    const allowed = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];

    this.selectedFiles = files.filter((file) => {
      const isAllowed = allowed.includes(file.type);
      const isSizeOk = file.size <= 5 * 1024 * 1024;
      return isAllowed && isSizeOk;
    });
  }

  uploadDocuments(): void {
    if (!this.draftClaim || this.selectedFiles.length === 0) {
      this.step = 3;
      return;
    }

    this.isUploading = true;
    const pending = [...this.selectedFiles];

    const uploadNext = (): void => {
      const file = pending.shift();
      if (!file) {
        this.isUploading = false;
        this.step = 3;
        return;
      }

      this.claimService.uploadDocument(this.draftClaim!.id, file).subscribe({
        next: () => {
          this.uploadedDocs.push({
            id: Date.now() + pending.length,
            fileName: file.name,
            fileType: file.type,
            fileSize: file.size,
            uploadedAt: new Date().toISOString()
          });
          uploadNext();
        },
        error: (error) => {
          this.isUploading = false;
          this.messageService.add({ severity: 'error', summary: 'Upload failed', detail: error?.error?.message || `Failed to upload ${file.name}` });
        }
      });
    };

    uploadNext();
  }

  submitClaim(): void {
    if (!this.draftClaim) {
      return;
    }

    this.isSubmitting = true;
    this.claimService.submitClaim(this.draftClaim.id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Claim submitted', detail: 'Your claim has been submitted successfully.' });
        void this.router.navigate(['/customer/claims']);
      },
      complete: () => {
        this.isSubmitting = false;
      },
      error: (error) => {
        this.isSubmitting = false;
        this.messageService.add({ severity: 'error', summary: 'Submit failed', detail: error?.error?.message || 'Could not submit claim.' });
      }
    });
  }

  previousStep(): void {
    if (this.step > 1) {
      this.step -= 1;
    }
  }

  formatFileSize(size: number): string {
    if (size < 1024) {
      return `${size} B`;
    }
    if (size < 1024 * 1024) {
      return `${(size / 1024).toFixed(1)} KB`;
    }
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }
}