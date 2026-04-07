import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type AppLanguage = 'ar' | 'en';

@Injectable({
    providedIn: 'root'
})
export class LanguageService {
    private static readonly STORAGE_KEY = 'app_language';

    private readonly languageSubject = new BehaviorSubject<AppLanguage>(this.readInitialLanguage());
    readonly language$ = this.languageSubject.asObservable();

    get currentLanguage(): AppLanguage {
        return this.languageSubject.value;
    }

    get currentDirection(): 'rtl' | 'ltr' {
        return this.currentLanguage === 'ar' ? 'rtl' : 'ltr';
    }

    setLanguage(language: AppLanguage): void {
        if (this.languageSubject.value === language) {
            return;
        }

        this.languageSubject.next(language);
        localStorage.setItem(LanguageService.STORAGE_KEY, language);
    }

    toggleLanguage(): void {
        this.setLanguage(this.currentLanguage === 'ar' ? 'en' : 'ar');
    }

    private readInitialLanguage(): AppLanguage {
        const stored = localStorage.getItem(LanguageService.STORAGE_KEY);
        return stored === 'en' ? 'en' : 'ar';
    }
}
