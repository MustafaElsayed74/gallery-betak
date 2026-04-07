import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './api/auth.service';

@Injectable({
    providedIn: 'root'
})
export class AuthRedirectService {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    ensureAuthenticated(returnUrl?: string): boolean {
        if (this.authService.getToken()) {
            return true;
        }

        this.router.navigate(['/auth/login'], {
            queryParams: { returnUrl: returnUrl ?? this.router.url }
        });

        return false;
    }
}
