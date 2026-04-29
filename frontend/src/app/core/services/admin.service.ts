import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap, timeout } from 'rxjs';
import { AdminUser, ClaimReview, DashboardSummary } from '../models/admin.models';

const REQUEST_TIMEOUT_MS = 15000;

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5000/gateway/admin';

  private _dashboardCache?: DashboardSummary;
  private _claimsCache?: ClaimReview[];
  private _usersCache?: AdminUser[];

  getDashboard(): Observable<DashboardSummary> {
    if (this._dashboardCache !== undefined) return of(this._dashboardCache);
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard`)
      .pipe(timeout(REQUEST_TIMEOUT_MS), tap(d => this._dashboardCache = d));
  }

  getAllClaims(): Observable<ClaimReview[]> {
    if (this._claimsCache !== undefined) return of(this._claimsCache);
    return this.http.get<ClaimReview[]>(`${this.baseUrl}/claims`)
      .pipe(timeout(REQUEST_TIMEOUT_MS), tap(d => this._claimsCache = d));
  }

  getPendingClaims(): Observable<ClaimReview[]> {
    return this.http.get<ClaimReview[]>(`${this.baseUrl}/claims/pending`).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  updateClaimStatus(claimId: number | string, status: string, adminNote: string): Observable<unknown> {
    this._claimsCache = undefined;
    this._dashboardCache = undefined;
    return this.http.put(`${this.baseUrl}/claims/status`, { claimId, status, adminNote });
  }

  getAllUsers(): Observable<AdminUser[]> {
    if (this._usersCache !== undefined) return of(this._usersCache);
    return this.http.get<AdminUser[]>(`${this.baseUrl}/users`)
      .pipe(timeout(REQUEST_TIMEOUT_MS), tap(d => this._usersCache = d));
  }

  updateUserStatus(userId: number | string, isActive: boolean): Observable<unknown> {
    this._usersCache = undefined;
    return this.http.put(`${this.baseUrl}/users/${userId}/status`, { isActive });
  }

  generateReport(reportType: string): Observable<unknown> {
    return this.http.get(`${this.baseUrl}/reports/generate`, {
      params: { reportType }
    }).pipe(timeout(REQUEST_TIMEOUT_MS));
  }

  getAdminLogs(): Observable<unknown[]> {
    return this.http.get<unknown[]>(`${this.baseUrl}/logs`).pipe(timeout(REQUEST_TIMEOUT_MS));
  }
}
