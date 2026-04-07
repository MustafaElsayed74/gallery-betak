import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { CartDto } from '../../services/api/cart.service';

export const CartActions = createActionGroup({
  source: 'Cart',
  events: {
    'Initialize Cart': emptyProps(),
    'Initialize Cart Success': props<{ cart: CartDto }>(),
    'Initialize Cart Failure': props<{ error: string }>(),

    'Add Item': props<{ productId: number; quantity: number }>(),
    'Add Item Success': props<{ cart: CartDto }>(),
    'Add Item Failure': props<{ error: string }>(),

    'Update Item Quantity': props<{ productId: number; quantity: number }>(),
    'Update Item Quantity Success': props<{ cart: CartDto }>(),
    'Update Item Quantity Failure': props<{ error: string }>(),

    'Remove Item': props<{ itemId: number }>(),
    'Remove Item Success': props<{ cart: CartDto }>(),
    'Remove Item Failure': props<{ error: string }>(),

    'Clear Cart': emptyProps()
  }
});
