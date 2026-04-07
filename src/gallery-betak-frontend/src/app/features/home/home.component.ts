import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProductCardComponent } from '../../shared/components/product-card/product-card.component';
import { Store } from '@ngrx/store';
import { ProductActions } from '../../core/store/product/product.actions';
import { selectProducts, selectCategories, selectLoading } from '../../core/store/product/product.reducer';
import { CartActions } from '../../core/store/cart/cart.actions';
import { WishlistService } from '../../core/services/api/wishlist.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthRedirectService } from '../../core/services/auth-redirect.service';
import { UiTextService } from '../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, ProductCardComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  private store = inject(Store);
  private wishlistService = inject(WishlistService);
  private authRedirectService = inject(AuthRedirectService);
  private toastService = inject(ToastService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();

  products$ = this.store.select(selectProducts);
  categories$ = this.store.select(selectCategories);
  loading$ = this.store.select(selectLoading);

  constructor() { }

  ngOnInit(): void {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.store.dispatch(ProductActions.loadCategories());
    this.store.dispatch(ProductActions.loadProducts({
      params: { pageSize: 4 }
    }));
  }

  handleAddToCart(productId: number) {
    this.store.dispatch(CartActions.addItem({ productId, quantity: 1 }));
    this.toastService.success(this.uiMessages.cart.addItemSuccess);
  }

  handleAddToWishlist(productId: number) {
    if (!this.authRedirectService.ensureAuthenticated()) {
      return;
    }

    this.wishlistService.toggleWishlistItem(productId).subscribe({
      next: () => {
        this.toastService.success(this.uiMessages.wishlist.updatedSuccess);
      },
      error: () => {
        this.toastService.error(this.uiMessages.wishlist.updateFailed);
      }
    });
  }
}
