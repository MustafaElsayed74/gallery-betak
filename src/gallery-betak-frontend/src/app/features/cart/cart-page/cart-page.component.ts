import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { CartActions } from '../../../core/store/cart/cart.actions';
import { selectCart, selectError, selectLoading } from '../../../core/store/cart/cart.reducer';
import { CartDto, CartItemDto } from '../../../core/services/api/cart.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-cart-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.css'
})
export class CartPageComponent implements OnInit {
  private store = inject(Store);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();

  cart$ = this.store.select(selectCart);
  loading$ = this.store.select(selectLoading);
  error$ = this.store.select(selectError);

  shippingCost = 0;
  couponCode = '';
  discount = 0;

  ngOnInit(): void {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.store.dispatch(CartActions.initializeCart());
  }

  getSubtotal(cart: CartDto | null): number {
    return cart?.subTotal ?? 0;
  }

  getTotal(cart: CartDto | null): number {
    return this.getSubtotal(cart) + this.shippingCost - this.discount;
  }

  increaseQuantity(item: CartItemDto) {
    const nextQuantity = Math.min(item.quantity + 1, 99);
    this.store.dispatch(CartActions.updateItemQuantity({ productId: item.productId, quantity: nextQuantity }));
  }

  decreaseQuantity(item: CartItemDto) {
    if (item.quantity <= 1) {
      return;
    }

    this.store.dispatch(CartActions.updateItemQuantity({ productId: item.productId, quantity: item.quantity - 1 }));
  }

  removeItem(productId: number) {
    this.store.dispatch(CartActions.removeItem({ itemId: productId }));
  }
}
