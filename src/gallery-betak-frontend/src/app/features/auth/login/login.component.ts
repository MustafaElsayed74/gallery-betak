import { AfterViewInit, Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../../core/store/auth/auth.actions';
import { selectError, selectLoading } from '../../../core/store/auth/auth.reducer';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements AfterViewInit {
  private store = inject(Store);
  private route = inject(ActivatedRoute);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();

  loginData = {
    email: '',
    password: '',
    rememberMe: false
  };

  isSubmitting = false;
  errorMessage = '';
  returnUrl = '/';
  googleClientId = environment.googleClientId;
  googleUnavailable = false;

  constructor() {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.store.select(selectLoading)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(loading => {
        this.isSubmitting = loading;
      });

    this.store.select(selectError)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(error => {
        this.errorMessage = error ?? '';
      });

    const queryReturnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    this.returnUrl = queryReturnUrl && queryReturnUrl.startsWith('/') ? queryReturnUrl : '/';
  }

  ngAfterViewInit(): void {
    if (!this.googleClientId) {
      this.googleUnavailable = true;
      return;
    }

    this.initGoogleSignIn();
  }

  onSubmit() {
    this.errorMessage = '';

    const email = this.loginData.email.trim();
    const password = this.loginData.password.trim();

    this.store.dispatch(AuthActions.login({
      request: {
        email,
        password
      },
      returnUrl: this.returnUrl
    }));
  }

  onGoogleFallbackClick() {
    const googleApi = (window as any)?.google;
    if (!googleApi?.accounts?.id) {
      this.googleUnavailable = true;
      return;
    }

    googleApi.accounts.id.prompt();
  }

  private async initGoogleSignIn() {
    try {
      await this.ensureGoogleSdkLoaded();
      const googleApi = (window as any)?.google;

      if (!googleApi?.accounts?.id) {
        this.googleUnavailable = true;
        return;
      }

      googleApi.accounts.id.initialize({
        client_id: this.googleClientId,
        callback: (response: { credential?: string }) => {
          const idToken = response?.credential;
          if (!idToken) {
            this.errorMessage = 'Google login failed to return a valid token.';
            return;
          }

          this.store.dispatch(AuthActions.googleLogin({
            idToken,
            returnUrl: this.returnUrl
          }));
        }
      });

      const container = document.getElementById('google-signin-button');
      if (!container) {
        this.googleUnavailable = true;
        return;
      }

      googleApi.accounts.id.renderButton(container, {
        type: 'standard',
        theme: 'outline',
        size: 'large',
        shape: 'pill',
        text: 'continue_with',
        width: container.clientWidth || 320
      });
    } catch {
      this.googleUnavailable = true;
    }
  }

  private ensureGoogleSdkLoaded(): Promise<void> {
    const googleApi = (window as any)?.google;
    if (googleApi?.accounts?.id) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const existingScript = document.querySelector('script[data-google-identity="true"]') as HTMLScriptElement | null;
      if (existingScript) {
        existingScript.addEventListener('load', () => resolve(), { once: true });
        existingScript.addEventListener('error', () => reject(), { once: true });
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.setAttribute('data-google-identity', 'true');
      script.onload = () => resolve();
      script.onerror = () => reject();
      document.head.appendChild(script);
    });
  }
}
