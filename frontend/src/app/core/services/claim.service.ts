import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Claim, ClaimDocument, CreateClaimRequest } from '../models/claim.models';

@Injectable({ providedIn: 'root' })
export class ClaimService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5000/gateway/claims';

  createClaim(data: CreateClaimRequest): Observable<Claim> {
    return this.http.post<Claim>(this.baseUrl, data);
  }

  submitClaim(id: number | string): Observable<Claim> {
    return this.http.post<Claim>(`${this.baseUrl}/${id}/submit`, {});
  }

  uploadDocument(id: number | string, file: File): Observable<ClaimDocument> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ClaimDocument>(`${this.baseUrl}/${id}/documents`, formData);
  }

  deleteDocument(claimId: number | string, documentId: number | string): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${claimId}/documents/${documentId}`);
  }

  getMyClaims(): Observable<Claim[]> {
    return this.http.get<Claim[]>(`${this.baseUrl}/my`);
  }

  getClaimById(id: number | string): Observable<Claim> {
    return this.http.get<Claim>(`${this.baseUrl}/${id}`);
  }

  getAllClaims(): Observable<Claim[]> {
    return this.http.get<Claim[]>(this.baseUrl);
  }

  updateClaimStatus(claimId: number | string, status: string, adminNote: string): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/${claimId}/status`, { claimId, status, adminNote });
  }

  getAdminStats(): Observable<unknown> {
    return this.http.get(`${this.baseUrl}/admin/stats`);
  }
}