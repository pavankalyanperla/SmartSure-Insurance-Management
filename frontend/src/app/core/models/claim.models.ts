export interface ClaimDocument {
  id: number;
  fileName: string;
  fileType: string;
  fileSize: number;
  uploadedAt: string;
}

export interface Claim {
  id: number;
  claimNumber: string;
  policyId: number;
  customerId: number;
  incidentDate: string;
  description: string;
  status: string;
  adminNote?: string;
  createdAt: string;
  updatedAt?: string;
  documents?: ClaimDocument[];
}

export interface CreateClaimRequest {
  policyId: number;
  incidentDate: string;
  description: string;
}