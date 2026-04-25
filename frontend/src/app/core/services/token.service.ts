import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { DecodedToken } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly TOKEN_KEY = 'smartsure_token';

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  removeToken(): void {
    localStorage.removeItem(this.TOKEN_KEY);
  }

  getDecodedToken(): DecodedToken | null {
    const token = this.getToken();
    if (!token) {
      return null;
    }

    try {
      return jwtDecode<DecodedToken>(token);
    } catch {
      return null;
    }
  }

  getUserId(): number | null {
    const decoded = this.getDecodedToken();
    return decoded?.sub ? Number(decoded.sub) : null;
  }

  getUserRole(): string | null {
    return this.getDecodedToken()?.role ?? null;
  }

  getUserName(): string | null {
    return this.getDecodedToken()?.fullName ?? null;
  }

  isTokenExpired(): boolean {
    const decoded = this.getDecodedToken();
    return !decoded ? true : decoded.exp * 1000 < Date.now();
  }

  isLoggedIn(): boolean {
    return !!this.getToken() && !this.isTokenExpired();
  }

  isAdmin(): boolean {
    return this.getUserRole() === 'ADMIN';
  }
}