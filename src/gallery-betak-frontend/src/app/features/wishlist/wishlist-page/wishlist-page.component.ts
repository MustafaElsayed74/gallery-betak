import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { WishlistDto, WishlistService } from '../../../core/services/api/wishlist.service';
import { Store } from '@ngrx/store';
import { CartActions } from '../../../core/store/cart/cart.actions';
import { AuthRedirectService } from '../../../core/services/auth-redirect.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-wishlist-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './wishlist-page.component.html',
  styleUrl: './wishlist-page.component.css'
})
export class WishlistPageComponent implements OnInit {
  private wishlistService = inject(WishlistService);
  private authRedirectService = inject(AuthRedirectService);
  private store = inject(Store);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();

  wishlist: WishlistDto | null = null;
  loading = false;
  isProcessing = false;
  errorMessage = '';

  constructor() {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });
  }

  ngOnInit(): void {
    if (!this.authRedirectService.ensureAuthenticated()) {
      return;
    }

    this.loadWishlist();
  }

  get wishlistItems() {
    return this.wishlist?.items ?? [];
  }

  loadWishlist() {
    this.loading = true;
    this.errorMessage = '';

    this.wishlistService.getWishlist().subscribe({
      next: wishlist => {
        this.wishlist = wishlist;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = this.uiMessages.wishlist.loadFailed;
      }
    });
  }

  removeItem(productId: number) {
    this.isProcessing = true;
    this.wishlistService.toggleWishlistItem(productId).subscribe({
      next: wishlist => {
        this.wishlist = wishlist;
        this.isProcessing = false;
      },
      error: () => {
        this.errorMessage = this.uiMessages.wishlist.removeFailed;
        this.isProcessing = false;
      }
    });
  }

  moveToCart(productId: number) {
    this.isProcessing = true;
    this.wishlistService.moveToCart(productId).subscribe({
      next: wishlist => {
        this.wishlist = wishlist;
        this.isProcessing = false;
        this.store.dispatch(CartActions.initializeCart());
      },
      error: () => {
        this.errorMessage = this.uiMessages.wishlist.moveToCartFailed;
        this.isProcessing = false;
      }
    });
  }

  clearWishlist() {
    this.isProcessing = true;
    this.wishlistService.clearWishlist().subscribe({
      next: () => {
        this.wishlist = { id: this.wishlist?.id ?? 0, items: [] };
        this.isProcessing = false;
      },
      error: () => {
        this.errorMessage = this.uiMessages.wishlist.clearFailed;
        this.isProcessing = false;
      }
    });
  }
}
