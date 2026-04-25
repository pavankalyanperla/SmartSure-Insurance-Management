import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminUser, ClaimReview, DashboardSummary } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5000/gateway/admin';

  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard`);
  }

  getAllClaims(): Observable<ClaimReview[]> {
    return this.http.get<ClaimReview[]>(`${this.baseUrl}/claims`);
  }

  getPendingClaims(): Observable<ClaimReview[]> {
    return this.http.get<ClaimReview[]>(`${this.baseUrl}/claims/pending`);
  }

  updateClaimStatus(claimId: number | string, status: string, adminNote: string): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/claims/status`, { claimId, status, adminNote });
  }

  getAllUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.baseUrl}/users`);
  }

  updateUserStatus(userId: number | string, isActive: boolean): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/users/${userId}/status`, { isActive });
  }

  generateReport(reportType: string): Observable<unknown> {
    return this.http.get(`${this.baseUrl}/reports/generate`, {
      params: { reportType }
    });
  }

  getAdminLogs(): Observable<unknown[]> {
    return this.http.get<unknown[]>(`${this.baseUrl}/logs`);
  }
}