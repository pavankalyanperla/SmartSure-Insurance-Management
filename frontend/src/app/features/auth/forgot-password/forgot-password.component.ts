import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly ngZone = inject(NgZone);

  step: 1 | 2 | 3 = 1;

  email = '';
  otpCode = '';
  newPassword = '';
  confirmPassword = '';
  showPassword = false;
  showConfirmPassword = false;

  isSending = false;
  isResending = false;
  isResetting = false;
  errorMessage = '';

  timeLeft = 900;
  timerInterval: ReturnType<typeof setInterval> | null = null;

  sendOtp(): void {
    this.errorMessage = '';
    if (!this.email.trim()) {
      this.errorMessage = 'Please enter your email address.';
      return;
    }

    this.isSending = true;
    this.authService.forgotPasswordSendOtp(this.email.trim()).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.isSending = false;
          this.step = 2;
          this.startTimer();
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.ngZone.run(() => {
          this.isSending = false;
          this.errorMessage = err?.error?.message || 'Could not send OTP. Check the email and try again.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  resendOtp(): void {
    if (this.isResending) return;
    this.isResending = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authService.forgotPasswordSendOtp(this.email.trim()).subscribe({
      next: () => {
        this.isResending = false;
        this.timeLeft = 900;
        this.startTimer();
        this.messageService.add({ severity: 'success', summary: 'OTP resent', detail: 'A fresh OTP has been sent to your email.' });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isResending = false;
        this.messageService.add({ severity: 'error', summary: 'Resend failed', detail: err?.error?.message || 'Could not resend OTP.' });
        this.cdr.detectChanges();
      }
    });
  }

  resetPassword(): void {
    this.errorMessage = '';

    if (!this.otpCode || this.otpCode.length !== 6) {
      this.errorMessage = 'Please enter the 6-digit OTP.';
      return;
    }
    if (!this.newPassword || this.newPassword.length < 6) {
      this.errorMessage = 'New password must be at least 6 characters.';
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.isResetting = true;
    this.authService.forgotPasswordReset({
      email: this.email.trim(),
      otpCode: this.otpCode.trim(),
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.ngZone.run(() => {
          if (this.timerInterval) { clearInterval(this.timerInterval); this.timerInterval = null; }
          this.isResetting = false;
          this.step = 3;
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.ngZone.run(() => {
          this.isResetting = false;
          this.errorMessage = err?.error?.message || 'Failed to reset password. Please try again.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  goToLogin(): void {
    void this.router.navigate(['/auth/login']);
  }

  startTimer(): void {
    if (this.timerInterval) { clearInterval(this.timerInterval); this.timerInterval = null; }
    this.timeLeft = 900;
    this.timerInterval = setInterval(() => {
      this.timeLeft -= 1;
      if (this.timeLeft <= 0 && this.timerInterval) {
        clearInterval(this.timerInterval);
        this.timerInterval = null;
      }
      this.cdr.detectChanges();
    }, 1000);
  }

  formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s < 10 ? '0' + s : s}`;
  }

  ngOnDestroy(): void {
    if (this.timerInterval) { clearInterval(this.timerInterval); this.timerInterval = null; }
  }
}
