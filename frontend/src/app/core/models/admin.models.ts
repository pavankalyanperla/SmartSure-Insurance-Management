export interface DashboardSummary {
  totalUsers: number;
  totalPolicies: number;
  totalClaims: number;
  pendingClaims: number;
  approvedClaims: number;
  rejectedClaims: number;
  totalRevenue: number;
  closedClaims: number;
}

export interface AdminUser {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface ClaimReview {
  claimId: number;
  claimNumber: string;
  customerName: string;
  policyId: number;
  incidentDate: string;
  description: string;
  status: string;
  createdAt: string;
  adminNote?: string;
  documents?: { id: number; fileName: string; fileType: string; fileSize: number; uploadedAt: string }[];
}

export interface ReportResult {
  reportType: string;
  generatedAt: string;
  data: unknown;
}