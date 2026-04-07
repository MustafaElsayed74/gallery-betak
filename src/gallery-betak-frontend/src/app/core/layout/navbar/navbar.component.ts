import { Component, DestroyRef, ElementRef, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { selectCart } from '../../store/cart/cart.reducer';
import { selectUser } from '../../store/auth/auth.reducer';
import { AuthActions } from '../../store/auth/auth.actions';
import { map } from 'rxjs';
import { LanguageService } from '../../services/language.service';
import { UiTextService } from '../../services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  isMenuOpen = false;
  searchTerm = '';
  isUserMenuOpen = false;
  private store = inject(Store);
  private router = inject(Router);
  private languageService = inject(LanguageService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);
  private hostElement = inject(ElementRef<HTMLElement>);

  uiMessages = this.uiTextService.getCurrentMessages();

  cartItemsCount$ = this.store.select(selectCart).pipe(
    // Use the API-provided total so the badge stays in sync with the cart summary.
    map(cart => cart?.totalItems ?? (cart?.items?.reduce((sum: number, item: any) => sum + item.quantity, 0) ?? 0))
  );

  user$ = this.store.select(selectUser);
  isAdmin$ = this.user$.pipe(
    map(user => (user?.roles ?? []).some(role => ['Admin', 'SuperAdmin'].includes(role)))
  );
  userInitials$ = this.user$.pipe(
    map(user => {
      const first = user?.firstName?.trim()?.[0] ?? '';
      const last = user?.lastName?.trim()?.[0] ?? '';
      return (first + last).toUpperCase() || 'U';
    })
  );

  language$ = this.languageService.language$;

  constructor() {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  toggleLanguage() {
    this.languageService.toggleLanguage();
  }

  toggleUserMenu() {
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.isUserMenuOpen) {
      return;
    }

    const target = event.target as HTMLElement | null;
    const menuShell = this.hostElement.nativeElement.querySelector('.user-menu-shell');
    if (target && menuShell && !menuShell.contains(target)) {
      this.closeUserMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscapePressed() {
    this.closeUserMenu();
  }

  closeUserMenu() {
    this.isUserMenuOpen = false;
  }

  onSearchSubmit() {
    const normalizedSearch = this.searchTerm.trim();

    this.router.navigate(['/products'], {
      queryParams: {
        search: normalizedSearch || null,
        page: 1
      }
    });

    if (window.matchMedia('(max-width: 767px)').matches) {
      this.isMenuOpen = false;
    }
  }

  logout() {
    this.closeUserMenu();
    this.store.dispatch(AuthActions.logout());
  }
}
