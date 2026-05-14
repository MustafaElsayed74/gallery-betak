import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const pw = control.get('newPassword')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return pw && confirm && pw !== confirm ? { mismatch: true } : null;
}

@Component({
  selector: 'app-reset-password',
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

            <!-- Invalid token state -->
            <div *ngIf="!token || !email" class="text-center py-6">
              <div class="mx-auto w-16 h-16 rounded-full bg-rose-50 flex items-center justify-center mb-4">
                <svg class="w-8 h-8 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                </svg>
              </div>
              <h2 class="text-xl font-bold text-slate-900 font-cairo mb-3">رابط غير صالح</h2>
              <p class="text-slate-500 text-sm mb-6">هذا الرابط غير صالح أو منتهي الصلاحية. اطلب رابطاً جديداً.</p>
              <a routerLink="/auth/forgot-password"
                class="inline-flex items-center justify-center px-6 py-3 rounded-xl text-white font-bold text-sm cta-gradient">
                طلب رابط جديد
              </a>
            </div>

            <!-- Success state -->
            <div *ngIf="token && email && done" class="text-center py-6">
              <div class="mx-auto w-16 h-16 rounded-full flex items-center justify-center mb-4"
                style="background: linear-gradient(135deg, rgba(14,165,233,0.12), rgba(20,184,166,0.12));">
                <svg class="w-8 h-8 text-teal-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <h2 class="text-xl font-bold text-slate-900 font-cairo mb-3">تم تغيير كلمة المرور!</h2>
              <p class="text-slate-500 text-sm mb-6">يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.</p>
              <a routerLink="/auth/login"
                class="inline-flex items-center justify-center px-6 py-3 rounded-xl text-white font-bold text-sm cta-gradient">
                تسجيل الدخول
              </a>
            </div>

            <!-- Form state -->
            <form *ngIf="token && email && !done" [formGroup]="form" (ngSubmit)="submit()" dir="rtl">
              <h1 class="text-2xl font-black text-slate-900 font-cairo text-right mb-2">إعادة تعيين كلمة المرور</h1>
              <p class="text-slate-500 text-sm text-right mb-8">أدخل كلمة المرور الجديدة لحسابك.</p>

              <!-- New Password -->
              <div class="mb-4">
                <label class="block text-sm font-bold text-slate-700 mb-2">كلمة المرور الجديدة</label>
                <input [type]="showPw ? 'text' : 'password'" formControlName="newPassword" id="reset-new-pw"
                  class="w-full px-4 py-3 rounded-xl border border-slate-200 text-slate-900 text-sm focus:outline-none focus:ring-2 transition"
                  placeholder="8 أحرف على الأقل">
                <p *ngIf="form.get('newPassword')?.touched && form.get('newPassword')?.errors?.['minlength']"
                  class="text-rose-500 text-xs mt-1.5">كلمة المرور يجب أن تكون 8 أحرف على الأقل</p>
              </div>

              <!-- Confirm Password -->
              <div class="mb-6">
                <label class="block text-sm font-bold text-slate-700 mb-2">تأكيد كلمة المرور</label>
                <input [type]="showPw ? 'text' : 'password'" formControlName="confirmPassword" id="reset-confirm-pw"
                  class="w-full px-4 py-3 rounded-xl border border-slate-200 text-slate-900 text-sm focus:outline-none focus:ring-2 transition"
                  placeholder="أعد إدخال كلمة المرور">
                <p *ngIf="form.hasError('mismatch') && form.get('confirmPassword')?.touched"
                  class="text-rose-500 text-xs mt-1.5">كلمتا المرور غير متطابقتين</p>
                <button type="button" (click)="showPw = !showPw"
                  class="text-sky-600 text-xs font-semibold mt-1.5 hover:text-sky-700 transition">
                  {{ showPw ? 'إخفاء' : 'إظهار' }} كلمة المرور
                </button>
              </div>

              <p *ngIf="errorMsg" class="text-rose-600 text-sm bg-rose-50 rounded-xl px-4 py-3 mb-4 border border-rose-100">
                {{ errorMsg }}
              </p>

              <button type="submit" id="reset-submit"
                [disabled]="loading || form.invalid"
                class="w-full py-3.5 rounded-xl text-white font-bold text-base cta-gradient shadow-md disabled:opacity-60 disabled:cursor-not-allowed transition">
                {{ loading ? 'جاري التغيير...' : 'تغيير كلمة المرور' }}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  token = '';
  email = '';
  loading = false;
  done = false;
  showPw = false;
  errorMsg = '';

  form = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordMatchValidator });

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
  }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.errorMsg = '';

    this.http.post<{ success: boolean; message: string }>(
      `${environment.apiUrl}/Auth/reset-password`,
      {
        email: this.email,
        token: this.token,
        newPassword: this.form.value.newPassword,
        confirmNewPassword: this.form.value.confirmPassword
      }
    ).subscribe({
      next: (res) => {
        if (res.success) {
          this.done = true;
        } else {
          this.errorMsg = res.message || 'حدث خطأ. حاول مرة أخرى.';
        }
        this.loading = false;
      },
      error: (err) => {
        this.errorMsg = err?.error?.message || 'رمز إعادة التعيين غير صالح أو منتهي الصلاحية. اطلب رابطاً جديداً.';
        this.loading = false;
      }
    });
  }
}
