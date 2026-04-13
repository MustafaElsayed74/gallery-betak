import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { AuthActions } from './auth.actions';
import { AuthService } from '../../services/api/auth.service';
import { catchError, map, mergeMap, of, tap } from 'rxjs';
import { Router } from '@angular/router';
import { SessionBannerService } from '../../services/session-banner.service';

@Injectable()
export class AuthEffects {
  private actions$ = inject(Actions);
  private authService = inject(AuthService);
  private sessionBannerService = inject(SessionBannerService);
  private router = inject(Router);

  private extractValidationErrorMessage(rawErrors: unknown): string | undefined {
    if (Array.isArray(rawErrors)) {
      const firstArrayError = rawErrors.find(
        (entry): entry is { message?: string } =>
          typeof entry === 'object' && entry !== null && 'message' in entry
      );

      return firstArrayError?.message;
    }

    if (rawErrors && typeof rawErrors === 'object') {
      const errorValues = Object.values(rawErrors as Record<string, unknown>);

      for (const value of errorValues) {
        if (Array.isArray(value)) {
          const firstMessage = value.find((item): item is string => typeof item === 'string' && item.length > 0);

          if (firstMessage) {
            return firstMessage;
          }
        }
      }
    }

    return undefined;
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    const response = error as {
      error?: {
        message?: string;
        messageEn?: string;
        title?: string;
        errors?: Array<{ message?: string }> | Record<string, unknown>;
      };
      message?: string;
    };

    const validationMessage = this.extractValidationErrorMessage(response?.error?.errors);

    return validationMessage
      || response?.error?.message
      || response?.error?.messageEn
      || response?.error?.title
      || response?.message
      || fallback;
  }

  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      mergeMap(({ request, returnUrl }) =>
        this.authService.login(request).pipe(
          map(response => AuthActions.loginSuccess({ response, returnUrl })),
          catchError(error => of(AuthActions.loginFailure({ error: this.getErrorMessage(error, 'Login failed') })))
        )
      )
    )
  );

  googleLogin$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.googleLogin),
      mergeMap(({ idToken, returnUrl }) =>
        this.authService.googleLogin({ idToken }).pipe(
          map(response => AuthActions.loginSuccess({ response, returnUrl })),
          catchError(error => of(AuthActions.loginFailure({ error: this.getErrorMessage(error, 'Google login failed') })))
        )
      )
    )
  );

  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.register),
      mergeMap(({ request, returnUrl }) =>
        this.authService.register(request).pipe(
          map(response => AuthActions.registerSuccess({ response, returnUrl })),
          catchError(error => of(AuthActions.registerFailure({ error: this.getErrorMessage(error, 'Registration failed') })))
        )
      )
    )
  );

  authSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loginSuccess, AuthActions.registerSuccess),
      tap(({ returnUrl }) => {
        this.sessionBannerService.dismiss();
        this.router.navigateByUrl(returnUrl ?? '/');
      })
    ),
    { dispatch: false }
  );

  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      tap(() => {
        this.authService.logout();
        this.router.navigate(['/auth/login']);
      })
    ),
    { dispatch: false }
  );
}
