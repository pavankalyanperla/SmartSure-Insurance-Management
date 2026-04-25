import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, SendOtpRequest, UserProfile, VerifyOtpRequest } from '../models/auth.models';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);
  private readonly baseUrl = 'http://localhost:5000/gateway/auth';

  sendOtp(data: { fullName: string; email: string; password: string }): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/send-otp`, data);
  }

  verifyOtpAndRegister(data: { email: string; otpCode: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/verify-register`, data).pipe(
      tap((response: AuthResponse) => {
        if (response?.token) {
          this.tokenService.setToken(response.token);
        }
      })
    );
  }

  resendOtp(email: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/resend-otp`, JSON.stringify(email), {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    });
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, data).pipe(
      tap((response) => this.tokenService.setToken(response.token))
    );
  }

  logout(): void {
    this.tokenService.removeToken();
    void this.router.navigate(['/auth/login']);
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/profile`);
  }

  isLoggedIn(): boolean {
    return this.tokenService.isLoggedIn();
  }

  isAdmin(): boolean {
    return this.tokenService.isAdmin();
  }

  getCurrentUserName(): string {
    return this.tokenService.getUserName() || '';
  }

  getCurrentUserId(): number {
    return this.tokenService.getUserId() || 0;
  }
}