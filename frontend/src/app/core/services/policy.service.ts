import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreatePolicyRequest, Payment, Policy, PolicyType, PremiumCalculation, PremiumResponse } from '../models/policy.models';

@Injectable({ providedIn: 'root' })
export class PolicyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5000/gateway/policies';

  getPolicyTypes(): Observable<PolicyType[]> {
    return this.http.get<PolicyType[]>(`${this.baseUrl}/types`);
  }

  getPolicyTypeById(id: number | string): Observable<PolicyType> {
    return this.http.get<PolicyType>(`${this.baseUrl}/types/${id}`);
  }

  calculatePremium(data: PremiumCalculation): Observable<PremiumResponse> {
    return this.http.post<PremiumResponse>(`${this.baseUrl}/calculate-premium`, data);
  }

  createPolicy(data: CreatePolicyRequest): Observable<Policy> {
    return this.http.post<Policy>(this.baseUrl, data);
  }

  getMyPolicies(): Observable<Policy[]> {
    return this.http.get<Policy[]>(`${this.baseUrl}/my`);
  }

  getPolicyById(id: number | string): Observable<Policy> {
    return this.http.get<Policy>(`${this.baseUrl}/${id}`);
  }

  getPaymentByPolicyId(id: number | string): Observable<Payment> {
    return this.http.get<Payment>(`${this.baseUrl}/${id}/payment`);
  }

  getMyPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.baseUrl}/my/payments`);
  }

  getAdminPolicyTypes(): Observable<PolicyType[]> {
    return this.http.get<PolicyType[]>(`${this.baseUrl}/admin/types`);
  }

  createPolicyType(data: Partial<PolicyType>): Observable<PolicyType> {
    return this.http.post<PolicyType>(`${this.baseUrl}/admin/types`, data);
  }

  updatePolicyType(id: number | string, data: Partial<PolicyType>): Observable<PolicyType> {
    return this.http.put<PolicyType>(`${this.baseUrl}/admin/types/${id}`, data);
  }

  deletePolicyType(id: number | string): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/admin/types/${id}`);
  }

  togglePolicyTypeStatus(id: number | string, isActive: boolean): Observable<unknown> {
    const params = new HttpParams().set('isActive', String(isActive));
    return this.http.put(`${this.baseUrl}/admin/types/${id}/toggle`, null, { params });
  }

  getPolicyTypeStats(id: number | string): Observable<unknown> {
    return this.http.get(`${this.baseUrl}/admin/types/${id}/stats`);
  }
}