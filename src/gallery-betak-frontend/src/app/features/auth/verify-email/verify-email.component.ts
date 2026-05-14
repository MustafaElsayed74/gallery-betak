import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/api/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center p-4"
      style="background: linear-gradient(135deg, #0b1329 0%, #0f2d40 50%, #0b1329 100%);">

      <div class="w-full max-w-md">
        <!-- Logo -->
        <div class="text-center mb-8">
          <div class="inline-flex items-center gap-3 mb-2">
            <span class="flex h-12 w-12 items-center justify-center rounded-2xl"
              style="background: linear-gradient(135deg, #0ea5e9, #14b8a6);">
              <svg class="h-7 w-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 32 32">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.3" d="M5 3.5v8.7M8.1 3.5v8.7M11.2 3.5v8.7M5 12.2c0 1.7 1.4 3.1 3.1 3.1s3.1-1.4 3.1-3.1M8.1 15.3v13.2M22.8 3.5v11.8M22.8 15.3c-2.9 0-5.3 2.5-5.3 5.6s2.4 5.6 5.3 5.6 5.3-2.5 5.3-5.6-2.4-5.6-5.3-5.6z"/>
              </svg>
            </span>
            <span class="text-white text-2xl font-black font-cairo">جاليري بيتك</span>
          </div>
        </div>

        <!-- Card -->
        <div class="rounded-3xl overflow-hidden shadow-2xl" style="background: rgba(255,255,255,0.97);">
          <div class="h-1.5" style="background: linear-gradient(90deg, #0ea5e9, #14b8a6);"></div>

          <div class="p-8">

            <!-- Success State -->
            <div *ngIf="done" class="text-center py-6">
              <div class="mx-auto w-16 h-16 rounded-full flex items-center justify-center mb-4"
                style="background: linear-gradient(135deg, rgba(14,165,233,0.12), rgba(20,184,166,0.12));">
                <svg class="w-8 h-8 text-teal-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <h2 class="text-xl font-bold text-slate-900 font-cairo mb-3">تم تأكيد البريد!</h2>
              <p class="text-slate-500 text-sm mb-6">شكراً لك، تم تأكيد بريدك الإلكتروني بنجاح.</p>
              <a routerLink="/account"
                class="inline-flex items-center justify-center px-6 py-3 rounded-xl text-white font-bold text-sm cta-gradient">
                الانتقال للحساب
              </a>
            </div>

            <!-- Form State -->
            <form *ngIf="!done" [formGroup]="form" (ngSubmit)="submit()" dir="rtl">
              <h1 class="text-2xl font-black text-slate-900 font-cairo text-right mb-2">تأكيد البريد الإلكتروني</h1>
              <p class="text-slate-500 text-sm text-right mb-8 leading-relaxed">
                أدخل رمز التحقق الذي أرسلناه إلى بريدك الإلكتروني <strong>{{ userEmail }}</strong>.
              </p>

              <!-- OTP Code -->
              <div class="mb-6 text-center">
                <input type="text" formControlName="code" id="verify-code" maxlength="6"
                  class="w-3/4 mx-auto text-center px-4 py-4 rounded-xl border-2 border-slate-200 text-slate-900 text-2xl font-bold tracking-[0.5em] focus:outline-none focus:border-cyan-500 transition font-mono"
                  placeholder="------" dir="ltr">
                <p *ngIf="form.get('code')?.touched && form.get('code')?.invalid"
                  class="text-rose-500 text-xs mt-2">يرجى إدخال الرمز بشكل صحيح</p>
              </div>

              <p *ngIf="errorMsg" class="text-rose-600 text-sm bg-rose-50 rounded-xl px-4 py-3 mb-4 border border-rose-100">
                {{ errorMsg }}
              </p>
              <p *ngIf="successMsg" class="text-teal-600 text-sm bg-teal-50 rounded-xl px-4 py-3 mb-4 border border-teal-100">
                {{ successMsg }}
              </p>

              <button type="submit" id="verify-submit"
                [disabled]="loading || form.invalid"
                class="w-full py-3.5 rounded-xl text-white font-bold text-base cta-gradient shadow-md disabled:opacity-60 disabled:cursor-not-allowed transition mb-4">
                {{ loading ? 'جاري التحقق...' : 'تأكيد الرمز' }}
              </button>

              <div class="text-center">
                <button type="button" (click)="resendCode()" [disabled]="resending"
                  class="text-sky-600 hover:text-sky-700 text-sm font-semibold transition disabled:opacity-50">
                  {{ resending ? 'جاري الإرسال...' : 'إرسال الرمز مرة أخرى' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  `
})
export class VerifyEmailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(4)]]
  });

  userEmail = '';
  loading = false;
  resending = false;
  done = false;
  errorMsg = '';
  successMsg = '';

  ngOnInit() {
    const user = this.authService.currentUserValue;
    if (!user) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.userEmail = user.email;
  }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.errorMsg = '';
    this.successMsg = '';

    this.http.post<{ success: boolean; message: string }>(
      `${environment.apiUrl}/Auth/email/verify`,
      { email: this.userEmail, code: this.form.value.code }
    ).subscribe({
      next: (res) => {
        if (res.success) {
          this.done = true;
        } else {
          this.errorMsg = res.message || 'رمز التحقق غير صالح.';
        }
        this.loading = false;
      },
      error: (err) => {
        this.errorMsg = err?.error?.message || 'رمز التحقق غير صالح أو منتهي الصلاحية.';
        this.loading = false;
      }
    });
  }

  resendCode() {
    this.resending = true;
    this.errorMsg = '';
    this.successMsg = '';

    this.http.post<{ success: boolean; message: string }>(
      `${environment.apiUrl}/Auth/email/send-verification`,
      {}
    ).subscribe({
      next: () => {
        this.successMsg = 'تم إرسال رمز جديد إلى بريدك الإلكتروني.';
        this.resending = false;
      },
      error: () => {
        this.errorMsg = 'فشل إرسال الرمز. حاول مرة أخرى.';
        this.resending = false;
      }
    });
  }
}
