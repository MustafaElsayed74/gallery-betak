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
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private store = inject(Store);
  private route = inject(ActivatedRoute);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();

  registerData = {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: ''
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

    const firstName = this.registerData.firstName.trim();
    const lastName = this.registerData.lastName.trim();
    const email = this.registerData.email.trim();
    const phone = this.normalizePhoneNumber(this.registerData.phone);
    const password = this.registerData.password.trim();
    const confirmPassword = this.registerData.confirmPassword.trim();

    if (firstName.length < 2 || lastName.length < 2) {
      this.errorMessage = this.uiMessages.auth.nameValidation;
      return;
    }

    if (!email) {
      this.errorMessage = this.uiMessages.auth.emailRequired;
      return;
    }

    if (!this.isStrongPassword(password)) {
      this.errorMessage = this.uiMessages.auth.passwordPolicy;
      return;
    }

    if (password !== confirmPassword) {
      this.errorMessage = this.uiMessages.auth.passwordMismatch;
      return;
    }

    if (phone && !this.isValidEgyptPhone(phone)) {
      this.errorMessage = this.uiMessages.auth.phoneValidation;
      return;
    }

    this.store.dispatch(AuthActions.register({
      request: {
        firstName,
        lastName,
        email,
        password,
        confirmPassword,
        phoneNumber: phone ? phone : undefined
      },
      returnUrl: this.returnUrl
    }));
  }

  private normalizePhoneNumber(rawPhone: string): string {
    const arabicIndicDigits = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩'];
    const easternArabicDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];

    const normalizedDigits = rawPhone
      .trim()
      .split('')
      .map(char => {
        const arabicIndex = arabicIndicDigits.indexOf(char);
        if (arabicIndex >= 0) {
          return arabicIndex.toString();
        }

        const easternArabicIndex = easternArabicDigits.indexOf(char);
        if (easternArabicIndex >= 0) {
          return easternArabicIndex.toString();
        }

        return char;
      })
      .join('');

    let digitsOnly = normalizedDigits.replace(/\D+/g, '');

    // Accept common Egyptian international format variants: +20 / 0020 / 20XXXXXXXXXX
    if (digitsOnly.startsWith('0020')) {
      digitsOnly = `0${digitsOnly.slice(4)}`;
    } else if (digitsOnly.startsWith('20') && digitsOnly.length === 12) {
      digitsOnly = `0${digitsOnly.slice(2)}`;
    }

    return digitsOnly;
  }

  private isValidEgyptPhone(phone: string): boolean {
    return /^01[0125]\d{8}$/.test(phone);
  }

  private isStrongPassword(password: string): boolean {
    return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$/.test(password);
  }
}
