import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AuthResponse, LoginRequest, RegisterRequest } from '../../services/api/auth.service';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    'Login': props<{ request: LoginRequest; returnUrl?: string }>(),
    'Login Success': props<{ response: AuthResponse; returnUrl?: string }>(),
    'Login Failure': props<{ error: string }>(),

    'Register': props<{ request: RegisterRequest; returnUrl?: string }>(),
    'Register Success': props<{ response: AuthResponse; returnUrl?: string }>(),
    'Register Failure': props<{ error: string }>(),

    'Logout': emptyProps(),

    'Load User From Storage': emptyProps()
  }
});
