import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/api/auth.service';
import { SessionBannerService } from '../services/session-banner.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const sessionBannerService = inject(SessionBannerService);

  const extractErrorMessage = (error: HttpErrorResponse): string => {
    const payload = error.error as {
      message?: string;
      title?: string;
      errors?: Array<{ message?: string }> | Record<string, unknown>;
    } | null;

    if (Array.isArray(payload?.errors)) {
      const firstArrayError = payload.errors.find(
        (entry): entry is { message?: string } =>
          typeof entry === 'object' && entry !== null && 'message' in entry
      );

      if (firstArrayError?.message) {
        return firstArrayError.message;
      }
    }

    if (payload?.errors && typeof payload.errors === 'object') {
      for (const value of Object.values(payload.errors)) {
        if (Array.isArray(value)) {
          const firstText = value.find((item): item is string => typeof item === 'string' && item.length > 0);

          if (firstText) {
            return firstText;
          }
        }
      }
    }

    return payload?.message || payload?.title || error.statusText || 'Unknown Error';
  };

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthRequest = /\/api\/v1\/Auth\/(login|register|refresh)(\?|$)/i.test(req.url);

      if (error.status === 401) {
        // Auto logout on unauthorized responses from protected endpoints only.
        if (!isAuthRequest) {
          sessionBannerService.showSessionExpired();
          authService.logout();

          if (!router.url.startsWith('/auth/login')) {
            router.navigate(['/auth/login'], { queryParams: { returnUrl: router.routerState.snapshot.url } });
          }
        }
      } else if (error.status === 403) {
        router.navigate(['/']); // Redirect to home on forbidden
      }

      const errorMessage = extractErrorMessage(error);
      console.error('API Error:', errorMessage);
      return throwError(() => error);
    })
  );
};
