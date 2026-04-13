import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  messageEn: string;
  data: T | null;
}

interface BackendUserProfile {
  id: string;
  email: string;
  phoneNumber: string | null;
  fullName: string;
  firstName: string;
  lastName: string;
  profileImageUrl: string | null;
  roles: string[];
}

interface BackendAuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: BackendUserProfile;
}

// Interfaces matching DTOs
export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  email: string;
  phoneNumber?: string | null;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface UserProfile {
  id: string;
  email: string;
  phoneNumber: string | null;
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl: string | null;
  roles: string[];
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface UserAddress {
  id: number;
  label: string;
  recipientName: string;
  phone: string;
  governorate: string;
  city: string;
  district?: string | null;
  streetAddress: string;
  buildingNo?: string | null;
  apartmentNo?: string | null;
  postalCode?: string | null;
  isDefault: boolean;
}

export interface UpsertAddressRequest {
  label: string;
  recipientName: string;
  phone: string;
  governorate: string;
  city: string;
  district?: string | null;
  streetAddress: string;
  buildingNo?: string | null;
  apartmentNo?: string | null;
  postalCode?: string | null;
  isDefault: boolean;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password?: string;
  confirmPassword?: string;
  phoneNumber?: string;
}

export interface LoginRequest {
  email: string;
  password?: string;
}

export interface GoogleLoginRequest {
  idToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly AUTH_URL = `${environment.apiUrl}/Auth`;
  private readonly CART_URL = `${environment.apiUrl}/Carts`;
  private currentUserSubject = new BehaviorSubject<AuthResponse | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUserFromStorage();
  }

  public get currentUserValue(): AuthResponse | null {
    return this.currentUserSubject.value;
  }

  login(model: LoginRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<BackendAuthResponse>>(`${this.AUTH_URL}/login`, model)
      .pipe(
        map(response => this.normalizeAuthResponse(response.data)),
        tap(response => {
          this.setAuthData(response);
          this.currentUserSubject.next(response);
        }),
        switchMap(response =>
          this.mergeGuestCartIfNeeded().pipe(
            map(() => response),
            catchError(() => of(response))
          )
        )
      );
  }

  googleLogin(model: GoogleLoginRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<BackendAuthResponse>>(`${this.AUTH_URL}/google/login`, model)
      .pipe(
        map(response => this.normalizeAuthResponse(response.data)),
        tap(response => {
          this.setAuthData(response);
          this.currentUserSubject.next(response);
        }),
        switchMap(response =>
          this.mergeGuestCartIfNeeded().pipe(
            map(() => response),
            catchError(() => of(response))
          )
        )
      );
  }

  register(model: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<BackendAuthResponse>>(`${this.AUTH_URL}/register`, model)
      .pipe(
        map(response => this.normalizeAuthResponse(response.data)),
        tap(response => {
          this.setAuthData(response);
          this.currentUserSubject.next(response);
        }),
        switchMap(response =>
          this.mergeGuestCartIfNeeded().pipe(
            map(() => response),
            catchError(() => of(response))
          )
        )
      );
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  public getToken(): string | null {
    return localStorage.getItem('token');
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<ApiResponse<UserProfile>>(`${this.AUTH_URL}/profile`).pipe(
      map(response => this.requirePayload(response, 'Profile payload missing.'))
    );
  }

  updateProfile(model: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<ApiResponse<UserProfile>>(`${this.AUTH_URL}/profile`, model).pipe(
      map(response => this.requirePayload(response, 'Update profile payload missing.')),
      tap(profile => this.syncUserFromProfile(profile))
    );
  }

  changePassword(model: ChangePasswordRequest): Observable<boolean> {
    return this.http.post<ApiResponse<boolean>>(`${this.AUTH_URL}/change-password`, model).pipe(
      map(response => response.data ?? false)
    );
  }

  getAddresses(): Observable<UserAddress[]> {
    return this.http.get<ApiResponse<UserAddress[]>>(`${this.AUTH_URL}/profile/addresses`).pipe(
      map(response => response.data ?? [])
    );
  }

  createAddress(model: UpsertAddressRequest): Observable<UserAddress> {
    return this.http.post<ApiResponse<UserAddress>>(`${this.AUTH_URL}/profile/addresses`, model).pipe(
      map(response => this.requirePayload(response, 'Create address payload missing.'))
    );
  }

  updateAddress(addressId: number, model: UpsertAddressRequest): Observable<UserAddress> {
    return this.http.put<ApiResponse<UserAddress>>(`${this.AUTH_URL}/profile/addresses/${addressId}`, model).pipe(
      map(response => this.requirePayload(response, 'Update address payload missing.'))
    );
  }

  setDefaultAddress(addressId: number): Observable<boolean> {
    return this.http.patch<ApiResponse<boolean>>(`${this.AUTH_URL}/profile/addresses/${addressId}/default`, {}).pipe(
      map(response => response.data ?? false)
    );
  }

  deleteAddress(addressId: number): Observable<boolean> {
    return this.http.delete<ApiResponse<boolean>>(`${this.AUTH_URL}/profile/addresses/${addressId}`).pipe(
      map(response => response.data ?? false)
    );
  }

  private setAuthData(response: AuthResponse) {
    localStorage.setItem('token', response.token);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('user', JSON.stringify({
      email: response.email,
      phoneNumber: response.phoneNumber ?? null,
      firstName: response.firstName,
      lastName: response.lastName,
      roles: response.roles
    }));
  }

  private syncUserFromProfile(profile: UserProfile) {
    const current = this.currentUserSubject.value;
    if (!current) {
      return;
    }

    const updated: AuthResponse = {
      ...current,
      email: profile.email,
      phoneNumber: profile.phoneNumber,
      firstName: profile.firstName,
      lastName: profile.lastName,
      roles: profile.roles
    };

    this.setAuthData(updated);
    this.currentUserSubject.next(updated);
  }

  private loadUserFromStorage() {
    const userStr = localStorage.getItem('user');
    const token = localStorage.getItem('token');

    if (userStr && token) {
      try {
        const user = JSON.parse(userStr);
        // We simulate AuthResponse structure for state
        this.currentUserSubject.next({
          ...user,
          token,
          refreshToken: localStorage.getItem('refreshToken') || '',
          expiration: ''
        });
      } catch (e) {
        this.logout();
      }
    }
  }

  private normalizeAuthResponse(data: BackendAuthResponse | null): AuthResponse {
    if (!data) {
      throw new Error('Authentication response payload missing.');
    }

    return {
      token: data.accessToken,
      refreshToken: data.refreshToken,
      expiration: data.expiresAt,
      email: data.user.email,
      phoneNumber: data.user.phoneNumber,
      firstName: data.user.firstName,
      lastName: data.user.lastName,
      roles: data.user.roles
    };
  }

  private requirePayload<T>(response: ApiResponse<T>, message: string): T {
    if (response.data == null) {
      throw new Error(message);
    }

    return response.data;
  }

  private mergeGuestCartIfNeeded(): Observable<boolean> {
    const sessionId = localStorage.getItem('cart_session_id');
    if (!sessionId) {
      return of(true);
    }

    return this.http.post<ApiResponse<boolean>>(
      `${this.CART_URL}/merge`,
      {},
      { headers: { 'X-Guest-Session-Id': sessionId } }
    ).pipe(
      map(response => response.data ?? true)
    );
  }
}
