import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): ReturnType<HttpInterceptorFn> => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const token = tokenService.getToken();
  const isPublicAuthRequest = ['/send-otp', '/verify-register', '/resend-otp', '/login'].some((segment) => req.url.includes(segment));

  const request = !token || isPublicAuthRequest
    ? req
    : req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });

  return next(request as HttpRequest<unknown>).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        authService.logout();
      }
      return throwError(() => error);
    })
  );
};