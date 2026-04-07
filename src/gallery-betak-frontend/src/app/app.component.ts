import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './core/layout/navbar/navbar.component';
import { FooterComponent } from './core/layout/footer/footer.component';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container.component';
import { Store } from '@ngrx/store';
import { CartActions } from './core/store/cart/cart.actions';
import { AuthActions } from './core/store/auth/auth.actions';
import { LanguageService } from './core/services/language.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SessionBannerService } from './core/services/session-banner.service';
import { UiTextService } from './core/services/ui-text.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent, FooterComponent, ToastContainerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'gallery-betak-frontend';
  isSessionBannerVisible = false;

  private store = inject(Store);
  private languageService = inject(LanguageService);
  private sessionBannerService = inject(SessionBannerService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();

  ngOnInit() {
    this.store.dispatch(CartActions.initializeCart());
    this.store.dispatch(AuthActions.loadUserFromStorage());

    this.languageService.language$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(language => {
        document.documentElement.lang = language;
        document.documentElement.dir = this.languageService.currentDirection;
      });

    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.sessionBannerService.isVisible$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(isVisible => {
        this.isSessionBannerVisible = isVisible;
      });
  }

  dismissSessionBanner(): void {
    this.sessionBannerService.dismiss();
  }
}
