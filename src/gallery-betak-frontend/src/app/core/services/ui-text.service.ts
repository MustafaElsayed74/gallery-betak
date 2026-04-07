import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { UI_MESSAGES } from '../constants/ui-messages';
import { UI_MESSAGES_EN } from '../constants/ui-messages.en';
import { AppLanguage, LanguageService } from './language.service';

@Injectable({
    providedIn: 'root'
})
export class UiTextService {
    private readonly languageService = inject(LanguageService);

    readonly messages$ = this.languageService.language$.pipe(
        map(language => this.getMessages(language))
    );

    getCurrentMessages() {
        return this.getMessages(this.languageService.currentLanguage);
    }

    private getMessages(language: AppLanguage) {
        if (language === 'en') {
            return {
                ...UI_MESSAGES,
                navbar: {
                    ...UI_MESSAGES.navbar,
                    ...UI_MESSAGES_EN.navbar
                },
                footer: {
                    ...UI_MESSAGES.footer,
                    ...UI_MESSAGES_EN.footer
                },
                auth: {
                    ...UI_MESSAGES.auth,
                    ...UI_MESSAGES_EN.auth
                },
                session: {
                    ...UI_MESSAGES.session,
                    ...UI_MESSAGES_EN.session
                },
                home: {
                    ...UI_MESSAGES.home,
                    ...UI_MESSAGES_EN.home
                },
                products: {
                    ...UI_MESSAGES.products,
                    ...UI_MESSAGES_EN.products
                },
                wishlist: {
                    ...UI_MESSAGES.wishlist,
                    ...UI_MESSAGES_EN.wishlist
                },
                cart: {
                    ...UI_MESSAGES.cart,
                    ...UI_MESSAGES_EN.cart
                },
                checkout: {
                    ...UI_MESSAGES.checkout,
                    ...UI_MESSAGES_EN.checkout
                }
            };
        }

        return UI_MESSAGES;
    }
}
