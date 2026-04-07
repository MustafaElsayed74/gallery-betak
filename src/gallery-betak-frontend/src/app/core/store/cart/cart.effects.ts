import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { CartActions } from './cart.actions';
import { CartService } from '../../services/api/cart.service';
import { catchError, map, mergeMap, of } from 'rxjs';

@Injectable()
export class CartEffects {
  private actions$ = inject(Actions);
  private cartService = inject(CartService);

  private createEmptyCart() {
    return {
      id: 0,
      items: [],
      subTotal: 0,
      shippingCost: 0,
      total: 0,
      totalItems: 0
    };
  }

  initializeCart$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.initializeCart),
      mergeMap(() => {
        const sessionId = localStorage.getItem('cart_session_id') || crypto.randomUUID();
        localStorage.setItem('cart_session_id', sessionId);
        return this.cartService.getCart(sessionId).pipe(
          map(cart => CartActions.initializeCartSuccess({ cart })),
          catchError(() => of(CartActions.initializeCartSuccess({ cart: this.createEmptyCart() })))
        );
      })
    )
  );

  addItem$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.addItem),
      mergeMap(({ productId, quantity }) =>
        this.cartService.addItemToCart(productId, quantity).pipe(
          map(cart => CartActions.addItemSuccess({ cart })),
          catchError(error => of(CartActions.addItemFailure({ error: 'Failed to add item to cart' })))
        )
      )
    )
  );

  removeItem$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.removeItem),
      mergeMap(({ itemId }) =>
        this.cartService.removeItemFromCart(itemId).pipe(
          map(cart => CartActions.removeItemSuccess({ cart })),
          catchError(error => of(CartActions.removeItemFailure({ error: 'Failed to remove item' })))
        )
      )
    )
  );

  updateItemQuantity$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.updateItemQuantity),
      mergeMap(({ productId, quantity }) =>
        this.cartService.updateItemQuantity(productId, quantity).pipe(
          map(cart => CartActions.updateItemQuantitySuccess({ cart })),
          catchError(() => of(CartActions.updateItemQuantityFailure({ error: 'Failed to update quantity' })))
        )
      )
    )
  );
}
