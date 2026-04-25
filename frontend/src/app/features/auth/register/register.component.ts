import { CommonModule } from '@angular/common';
import { Component, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);

  isStep1 = true;
  isStep2 = false;
  isSending = false;
  isVerifying = false;
  errorMessage = '';
  successMessage = '';
  otpCode = '';
  timeLeft = 900;
  timerInterval: ReturnType<typeof setInterval> | null = null;

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';

  sendOtp(): void {
    this.errorMessage = '';

    if (!this.fullName.trim()) {
      this.errorMessage = 'Full name is required';
      return;
    }
    if (!this.email.trim()) {
      this.errorMessage = 'Email is required';
      return;
    }
    if (!this.password) {
      this.errorMessage = 'Password is required';
      return;
    }
    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match';
      return;
    }

    this.isSending = true;

    this.authService
      .sendOtp({
        fullName: this.fullName,
        email: this.email,
        password: this.password
      })
      .subscribe({
      next: (response) => {
        console.log('OTP sent:', response);
        this.isSending = false;
        this.isStep1 = false;
        this.isStep2 = true;
        this.successMessage = 'OTP sent successfully. Please verify to continue.';
        this.startTimer();
      },
      error: (error) => {
        console.error('Send OTP error:', error);
        this.errorMessage = error?.error?.message || error?.error || 'Failed to send OTP. Please try again.';
        this.isSending = false;
        this.messageService.add({ severity: 'error', summary: 'OTP failed', detail: this.errorMessage });
      }
    });
  }

  verifyOtp(): void {
    this.errorMessage = '';
    if (!this.otpCode || this.otpCode.length !== 6) {
      this.errorMessage = 'Please enter the 6-digit OTP';
      return;
    }

    this.isVerifying = true;
    this.authService.verifyOtpAndRegister({
      email: this.email,
      otpCode: this.otpCode
    }).subscribe({
      next: (response) => {
        console.log('Verified:', response);
        if (this.timerInterval) {
          clearInterval(this.timerInterval);
          this.timerInterval = null;
        }
        this.isVerifying = false;
        void this.router.navigate(['/customer/dashboard']);
      },
      error: (error) => {
        console.error('Verify OTP error:', error);
        this.errorMessage = error?.error?.message || 'Invalid OTP. Please try again.';
        this.isVerifying = false;
        this.messageService.add({ severity: 'error', summary: 'Verification failed', detail: this.errorMessage });
      }
    });
  }

  resendOtp(): void {
    if (!this.email) {
      return;
    }

    this.authService.resendOtp(this.email).subscribe({
      next: () => {
        this.timeLeft = 900;
        this.startTimer();
        this.messageService.add({ severity: 'success', summary: 'OTP resent', detail: 'A fresh OTP has been sent to your email.' });
      },
      error: (error) => this.messageService.add({ severity: 'error', summary: 'Resend failed', detail: error?.error?.message || 'Could not resend OTP' })
    });
  }

  goBack(): void {
    this.isStep1 = true;
    this.isStep2 = false;
    this.otpCode = '';
    this.errorMessage = '';
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  startTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
    this.timeLeft = 900;
    this.timerInterval = setInterval(() => {
      this.timeLeft -= 1;
      if (this.timeLeft <= 0 && this.timerInterval) {
        clearInterval(this.timerInterval);
        this.timerInterval = null;
      }
    }, 1000);
  }

  formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s < 10 ? '0' + s : s}`;
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }
}