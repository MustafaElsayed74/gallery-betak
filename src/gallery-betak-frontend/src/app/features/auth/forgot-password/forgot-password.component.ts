import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
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
          <!-- Top accent -->
          <div class="h-1.5" style="background: linear-gradient(90deg, #0ea5e9, #14b8a6);"></div>

          <div class="p-8">
            <h1 class="text-2xl font-black text-slate-900 font-cairo text-right mb-2">نسيت كلمة المرور؟</h1>
            <p class="text-slate-500 text-sm text-right mb-8 leading-relaxed">
              أدخل بريدك الإلكتروني وسنرسل لك رابطاً لإعادة تعيين كلمة المرور.
            </p>

            <!-- Success State -->
            <div *ngIf="sent" class="text-center py-6">
              <div class="mx-auto w-16 h-16 rounded-full flex items-center justify-center mb-4"
                style="background: linear-gradient(135deg, rgba(14,165,233,0.12), rgba(20,184,166,0.12));">
                <svg class="w-8 h-8 text-teal-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                </svg>
              </div>
              <h2 class="text-xl font-bold text-slate-900 font-cairo mb-3">تم إرسال الرابط!</h2>
              <p class="text-slate-500 text-sm leading-relaxed mb-6">
                تفقد بريدك الإلكتروني وانقر على الرابط لإعادة تعيين كلمة المرور. قد يستغرق الأمر بضع دقائق.
              </p>
              <a routerLink="/auth/login"
                class="inline-flex items-center justify-center px-6 py-3 rounded-xl text-white font-bold text-sm cta-gradient shadow-md">
                العودة لتسجيل الدخول
              </a>
            </div>

            <!-- Form State -->
            <form *ngIf="!sent" [formGroup]="form" (ngSubmit)="submit()" dir="rtl">
              <div class="mb-6">
                <label class="block text-sm font-bold text-slate-700 mb-2">البريد الإلكتروني</label>
                <input type="email" formControlName="email" id="forgot-email"
                  class="w-full px-4 py-3 rounded-xl border border-slate-200 text-slate-900 text-sm focus:outline-none focus:ring-2 focus:border-transparent transition"
                  style="focus-ring-color: #0ea5e9;"
                  placeholder="example@email.com"
                  dir="ltr">
                <p *ngIf="form.get('email')?.touched && form.get('email')?.invalid"
                  class="text-rose-500 text-xs mt-1.5">يرجى إدخال بريد إلكتروني صالح</p>
              </div>

              <p *ngIf="errorMsg" class="text-rose-600 text-sm text-right bg-rose-50 rounded-xl px-4 py-3 mb-4 border border-rose-100">
                {{ errorMsg }}
              </p>

              <button type="submit" id="forgot-submit"
                [disabled]="loading || form.invalid"
                class="w-full py-3.5 rounded-xl text-white font-bold text-base cta-gradient shadow-md disabled:opacity-60 disabled:cursor-not-allowed transition">
                {{ loading ? 'جاري الإرسال...' : 'إرسال رابط إعادة التعيين' }}
              </button>

              <div class="text-center mt-6">
                <a routerLink="/auth/login"
                  class="text-sky-600 hover:text-sky-700 text-sm font-semibold transition">
                  ← العودة لتسجيل الدخول
                </a>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  loading = false;
  sent = false;
  errorMsg = '';

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.errorMsg = '';

    this.http.post<{ success: boolean; message: string }>(
      `${environment.apiUrl}/Auth/forgot-password`,
      { email: this.form.value.email }
    ).subscribe({
      next: () => {
        this.sent = true;
        this.loading = false;
      },
      error: () => {
        this.errorMsg = 'حدث خطأ غير متوقع. حاول مرة أخرى.';
        this.loading = false;
      }
    });
  }
}
