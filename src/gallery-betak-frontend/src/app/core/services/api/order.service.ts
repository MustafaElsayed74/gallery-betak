import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  messageEn: string;
  data: T | null;
}

export interface CreateOrderRequest {
  addressId: number;
  paymentMethod: number;
  notes?: string;
}

export interface OrderItem {
  productId: number;
  productNameAr: string;
  productNameEn: string;
  productSKU: string;
  productImageUrl: string | null;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface OrderDto {
  id: number;
  orderNumber: string;
  status: string;
  subTotal: number;
  shippingCost: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
  paymentMethod: string;
  paymentStatus: string;
  notes: string | null;
  createdAt: string;
  shippingRecipientName: string;
  shippingPhone: string;
  shippingGovernorate: string;
  shippingCity: string;
  shippingStreetAddress: string;
  trackingNumber: string | null;
  items: OrderItem[];
}

export interface OrderSummaryDto {
  id: number;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  itemCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly URL = `${environment.apiUrl}/Orders`;

  constructor(private http: HttpClient) { }

  createOrder(request: CreateOrderRequest): Observable<OrderDto> {
    return this.http.post<ApiResponse<OrderDto>>(this.URL, request).pipe(
      map(response => {
        if (!response.data) {
          throw new Error('Order payload missing.');
        }

        return response.data;
      })
    );
  }

  getOrdersForUser(): Observable<OrderSummaryDto[]> {
    return this.http.get<ApiResponse<OrderSummaryDto[]>>(this.URL).pipe(
      map(response => response.data ?? [])
    );
  }

  getOrderForUser(id: number): Observable<OrderDto> {
    return this.http.get<ApiResponse<OrderDto>>(`${this.URL}/${id}`).pipe(
      map(response => {
        if (!response.data) {
          throw new Error('Order payload missing.');
        }

        return response.data;
      })
    );
  }
}
