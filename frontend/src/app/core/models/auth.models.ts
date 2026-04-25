export interface SendOtpRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface VerifyOtpRequest {
  email: string;
  otpCode: string;
}

export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
  role: string;
  expiresAt?: string;
}

export interface UserProfile {
  id: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface DecodedToken {
  sub: string;
  email: string;
  role: string;
  fullName: string;
  exp: number;
}