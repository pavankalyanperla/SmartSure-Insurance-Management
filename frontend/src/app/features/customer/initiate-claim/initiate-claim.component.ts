import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
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
  private readonly cdr = inject(ChangeDetectorRef);

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

  get minIncidentDate(): string {
    const policy = this.policies.find(p => p.id === this.selectedPolicyId);
    return policy?.startDate ? policy.startDate.split('T')[0] : '';
  }

  ngOnInit(): void {
    this.policyService.getMyPolicies().subscribe({
      next: (policies) => {
        this.policies = policies.filter(p => (p.status || '').toLowerCase() === 'active');
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  createDraft(): void {
    if (!this.selectedPolicyId || !this.incidentDate || this.description.trim().length < 10) {
      this.messageService.add({ severity: 'warn', summary: 'Incomplete form', detail: 'Provide policy, incident date and at least 10 characters of description.' });
      return;
    }
    if (this.minIncidentDate && this.incidentDate < this.minIncidentDate) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid date', detail: 'Incident date cannot be before the policy start date.' });
      return;
    }
    if (this.incidentDate > this.todayDate) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid date', detail: 'Incident date cannot be in the future.' });
      return;
    }

    this.isSubmitting = true;
    this.claimService.createClaim({
      policyId: this.selectedPolicyId,
      incidentDate: this.incidentDate,
      description: this.description.trim()
    }).subscribe({
      next: (claim) => {
        this.draftClaim = claim;
        this.step = 2;
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.isSubmitting = false;
        this.messageService.add({ severity: 'error', summary: 'Draft creation failed', detail: error?.error?.message || 'Could not create claim draft.' });
        this.cdr.detectChanges();
      }
    });
  }

  handleFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const allowed = ['application/pdf', 'image/jpeg', 'image/png'];
    const file = Array.from(input.files || []).find(f => allowed.includes(f.type) && f.size <= 5 * 1024 * 1024);
    if (!file) {
      this.selectedFiles = [];
      if ((input.files?.length ?? 0) > 0) {
        this.messageService.add({ severity: 'warn', summary: 'Invalid file', detail: 'Only PDF, JPG, JPEG, or PNG files up to 5 MB are allowed.' });
      }
      return;
    }
    this.selectedFiles = [file];
  }

  uploadDocuments(): void {
    if (!this.draftClaim) return;
    if (this.selectedFiles.length === 0) {
      this.messageService.add({ severity: 'warn', summary: 'Documents required', detail: 'Please upload at least one supporting document (PDF, JPG, or PNG) before proceeding.' });
      return;
    }

    this.isUploading = true;
    const fileToUpload = this.selectedFiles[0];
    const docsToDelete = [...this.uploadedDocs];
    this.uploadedDocs = [];

    const doUpload = (): void => {
      this.claimService.uploadDocument(this.draftClaim!.id, fileToUpload).subscribe({
        next: (doc) => {
          this.uploadedDocs = [doc];
          this.isUploading = false;
          this.step = 3;
          this.cdr.detectChanges();
        },
        error: (error) => {
          this.isUploading = false;
          this.cdr.detectChanges();
          this.messageService.add({ severity: 'error', summary: 'Upload failed', detail: error?.error?.message || `Failed to upload ${fileToUpload.name}` });
        }
      });
    };

    const deleteOld = (index: number): void => {
      if (index >= docsToDelete.length) { doUpload(); return; }
      const doc = docsToDelete[index];
      const proceed = () => deleteOld(index + 1);
      if (doc.id) {
        this.claimService.deleteDocument(this.draftClaim!.id, doc.id).subscribe({ next: proceed, error: proceed });
      } else {
        proceed();
      }
    };

    deleteOld(0);
  }

  submitClaim(): void {
    if (!this.draftClaim) return;

    this.isSubmitting = true;
    this.claimService.submitClaim(this.draftClaim.id).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.messageService.add({ severity: 'success', summary: 'Claim submitted', detail: 'Your claim has been submitted successfully.' });
        void this.router.navigate(['/customer/claims']);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.cdr.detectChanges();
        this.messageService.add({ severity: 'error', summary: 'Submit failed', detail: error?.error?.message || 'Could not submit claim.' });
      }
    });
  }

  previousStep(): void {
    if (this.step === 3) {
      this.selectedFiles = [];
    }
    if (this.step > 1) this.step -= 1;
  }

  formatFileSize(size: number): string {
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }
}
