import { Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../../core/store/auth/auth.actions';
import { selectError, selectLoading } from '../../../core/store/auth/auth.reducer';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
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
}
