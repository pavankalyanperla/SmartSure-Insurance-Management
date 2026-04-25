export interface PolicyType {
  id: number;
  name: string;
  description: string;
  baseAmount: number;
  isActive: boolean;
  coverageDetails?: string;
  durationMonths?: number;
  minAge?: number;
  maxAge?: number;
  claimLimit?: number;
  exclusions?: string;
  waitingPeriod?: number;
  autoRenewal?: boolean;
  gracePeriodDays?: number;
  riskCategory?: string;
  enrolledCount?: number;
}

export interface Policy {
  id: number;
  policyNumber: string;
  policyTypeName: string;
  status: string;
  startDate: string;
  endDate: string;
  premiumAmount: number;
  createdAt: string;
  policyTypeId?: number;
}

export interface PremiumCalculation {
  policyTypeId: number;
  age: number;
  startDate: string;
  endDate: string;
}

export interface PremiumResponse {
  baseAmount: number;
  ageFactor: number;
  durationFactor: number;
  finalAmount: number;
}

export interface CreatePolicyRequest {
  policyTypeId: number;
  startDate: string;
  endDate: string;
  age: number;
}

export interface Payment {
  id: number;
  policyId: number;
  amount: number;
  paymentMethod: string;
  status: string;
  transactionId: string;
  paymentDate: string;
}