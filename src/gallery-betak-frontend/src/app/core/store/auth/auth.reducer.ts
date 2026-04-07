import { createFeature, createReducer, on } from '@ngrx/store';
import { AuthActions } from './auth.actions';
import { AuthResponse } from '../../services/api/auth.service';

export interface AuthState {
  user: AuthResponse | null;
  loading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  loading: false,
  error: null
};

export const authFeature = createFeature({
  name: 'auth',
  reducer: createReducer(
    initialState,
    
    // Login
    on(AuthActions.login, (state) => ({
      ...state,
      loading: true,
      error: null
    })),
    on(AuthActions.loginSuccess, (state, { response }) => ({
      ...state,
      user: response,
      loading: false,
      error: null
    })),
    on(AuthActions.loginFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error
    })),
    
    // Register
    on(AuthActions.register, (state) => ({
      ...state,
      loading: true,
      error: null
    })),
    on(AuthActions.registerSuccess, (state, { response }) => ({
      ...state,
      user: response,
      loading: false,
      error: null
    })),
    on(AuthActions.registerFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error
    })),
    
    // Logout
    on(AuthActions.logout, (state) => ({
      ...state,
      user: null,
      error: null
    })),
    
    // Load existing via effect injection
    on(AuthActions.loadUserFromStorage, (state) => {
      const userStr = localStorage.getItem('user');
      const token = localStorage.getItem('token');
      if (userStr && token) {
        try {
          const user = JSON.parse(userStr);
          return {
            ...state,
            user: { ...user, token, refreshToken: localStorage.getItem('refreshToken') || '', expiration: '' }
          };
        } catch {
          return state;
        }
      }
      return state;
    })
  )
});

export const {
  selectAuthState,
  selectUser,
  selectLoading,
  selectError
} = authFeature;
