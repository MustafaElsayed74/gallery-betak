import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  messageEn: string;
  data: T | null;
}

interface BackendCartItemDto {
  productId: number;
  productNameAr: string;
  productNameEn: string;
  imageUrl: string | null;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  stockQuantity: number;
}

interface BackendCartDto {
  id: number;
  subTotal: number;
  totalItems: number;
  items: BackendCartItemDto[];
}

export interface CartItemDto {
  productId: number;
  productNameAr: string;
  productNameEn: string;
  productSlug: string;
  productImage: string;
  unitPrice: number;
  quantity: number;
}

export interface CartDto {
  id: number;
  items: CartItemDto[];
  subTotal: number;
  shippingCost: number;
  total: number;
  totalItems: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly URL = `${environment.apiUrl}/Carts`;
  private cartSource = new BehaviorSubject<CartDto | null>(null);
  cart$ = this.cartSource.asObservable();

  constructor(private http: HttpClient) {
    this.initCart();
  }

  private initCart() {
    const sessionId = this.getSessionId();
    if (sessionId) {
      this.getCart(sessionId).subscribe({
        next: cart => this.cartSource.next(cart),
        error: () => console.log('Cart init failed')
      });
    }
  }

  private getHeaders(sessionId: string | null) {
    return sessionId ? { headers: { 'X-Guest-Session-Id': sessionId } } : {};
  }

  getCart(sessionId: string): Observable<CartDto> {
    return this.http.get<ApiResponse<BackendCartDto>>(this.URL, this.getHeaders(sessionId))
      .pipe(
        map(response => this.mapCart(response.data)),
        tap(cart => this.cartSource.next(cart))
      );
  }

  addItemToCart(productId: number, quantity: number = 1): Observable<CartDto> {
    const sessionId = this.getOrCreateSessionId();
    const payload = { productId, quantity };

    return this.http.post<ApiResponse<BackendCartDto>>(`${this.URL}/items`, payload, this.getHeaders(sessionId))
      .pipe(
        map(response => this.mapCart(response.data)),
        tap(cart => this.cartSource.next(cart))
      );
  }

  updateItemQuantity(productId: number, quantity: number): Observable<CartDto> {
    const sessionId = this.getOrCreateSessionId();
    const payload = { quantity };

    return this.http.put<ApiResponse<BackendCartDto>>(`${this.URL}/items/${productId}`, payload, this.getHeaders(sessionId))
      .pipe(
        map(response => this.mapCart(response.data)),
        tap(cart => this.cartSource.next(cart))
      );
  }

  removeItemFromCart(productId: number): Observable<CartDto> {
    const sessionId = this.getSessionId();
    return this.http.delete<ApiResponse<BackendCartDto>>(`${this.URL}/items/${productId}`, this.getHeaders(sessionId))
      .pipe(
        map(response => this.mapCart(response.data)),
        tap(cart => this.cartSource.next(cart))
      );
  }

  clearCart(): Observable<boolean> {
    const sessionId = this.getSessionId();
    return this.http.delete<ApiResponse<boolean>>(this.URL, this.getHeaders(sessionId))
      .pipe(
        map(response => response.data ?? false),
        tap(() => this.cartSource.next(null))
      );
  }

  private getOrCreateSessionId(): string {
    let sessionId = localStorage.getItem('cart_session_id');
    if (!sessionId) {
      sessionId = crypto.randomUUID();
      localStorage.setItem('cart_session_id', sessionId);
    }
    return sessionId;
  }

  private getSessionId(): string | null {
    return localStorage.getItem('cart_session_id');
  }

  private mapCart(cart: BackendCartDto | null): CartDto {
    if (!cart) {
      throw new Error('Cart payload missing.');
    }

    return {
      id: cart.id,
      items: cart.items.map(item => ({
        productId: item.productId,
        productNameAr: item.productNameAr,
        productNameEn: item.productNameEn,
        productSlug: '',
        productImage: item.imageUrl ?? 'assets/placeholder.svg',
        unitPrice: item.unitPrice,
        quantity: item.quantity
      })),
      subTotal: cart.subTotal,
      shippingCost: 0,
      total: cart.subTotal,
      totalItems: cart.totalItems
    };
  }
}
