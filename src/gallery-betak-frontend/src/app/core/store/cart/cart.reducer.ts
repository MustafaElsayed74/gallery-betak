import { createFeature, createReducer, on } from '@ngrx/store';
import { CartActions } from './cart.actions';
import { CartDto } from '../../services/api/cart.service';

export interface CartState {
  cart: CartDto | null;
  loading: boolean;
  error: string | null;
}

const initialState: CartState = {
  cart: null,
  loading: false,
  error: null
};

export const cartFeature = createFeature({
  name: 'cart',
  reducer: createReducer(
    initialState,

    on(CartActions.initializeCart, (state) => ({ ...state, loading: true, error: null })),
    on(CartActions.initializeCartSuccess, (state, { cart }) => ({ ...state, cart, loading: false })),
    on(CartActions.initializeCartFailure, (state, { error }) => ({ ...state, loading: false, error })),

    on(CartActions.addItem, (state) => ({ ...state, loading: true, error: null })),
    on(CartActions.addItemSuccess, (state, { cart }) => ({ ...state, cart, loading: false })),
    on(CartActions.addItemFailure, (state, { error }) => ({ ...state, loading: false, error })),

    on(CartActions.updateItemQuantity, (state) => ({ ...state, loading: true, error: null })),
    on(CartActions.updateItemQuantitySuccess, (state, { cart }) => ({ ...state, cart, loading: false })),
    on(CartActions.updateItemQuantityFailure, (state, { error }) => ({ ...state, loading: false, error })),

    on(CartActions.removeItem, (state) => ({ ...state, loading: true, error: null })),
    on(CartActions.removeItemSuccess, (state, { cart }) => ({ ...state, cart, loading: false })),
    on(CartActions.removeItemFailure, (state, { error }) => ({ ...state, loading: false, error })),

    on(CartActions.clearCart, (state) => ({ ...state, cart: null, loading: false, error: null }))
  )
});

export const {
  selectCartState,
  selectCart,
  selectLoading,
  selectError
} = cartFeature;
