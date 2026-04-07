// @ts-nocheck
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

interface BackendWishlistItem {
    productId: number;
    productNameAr: string;
    productNameEn: string;
    imageUrl: string | null;
    unitPrice: number;
    stockQuantity: number;
    isInStock: boolean;
    addedAt: string;
}

interface BackendWishlist {
    id: number;
    items: BackendWishlistItem[];
}

export interface WishlistItem {
    productId: number;
    productNameAr: string;
    productNameEn: string;
    imageUrl: string;
    unitPrice: number;
    stockQuantity: number;
    isInStock: boolean;
    addedAt: string;
}

export interface WishlistDto {
    id: number;
    items: WishlistItem[];
}

@Injectable({
    providedIn: 'root'
})
export class WishlistService {
    private readonly URL = `${environment.apiUrl}/Wishlists`;

    constructor(private http: HttpClient) { }

    getWishlist(): Observable<WishlistDto> {
        return this.http.get<ApiResponse<BackendWishlist>>(this.URL).pipe(
            map((response: ApiResponse<BackendWishlist>) => this.mapWishlist(response.data))
        );
    }

    toggleWishlistItem(productId: number): Observable<WishlistDto> {
        return this.http.post<ApiResponse<BackendWishlist>>(`${this.URL}/toggle/${productId}`, {}).pipe(
            map((response: ApiResponse<BackendWishlist>) => this.mapWishlist(response.data))
        );
    }

    clearWishlist(): Observable<boolean> {
        return this.http.delete<ApiResponse<boolean>>(this.URL).pipe(
            map((response: ApiResponse<boolean>) => response.data ?? false)
        );
    }

    moveToCart(productId: number): Observable<WishlistDto> {
        return this.http.post<ApiResponse<BackendWishlist>>(`${this.URL}/move-to-cart/${productId}`, {}).pipe(
            map((response: ApiResponse<BackendWishlist>) => this.mapWishlist(response.data))
        );
    }

    private mapWishlist(data: BackendWishlist | null): WishlistDto {
        if (!data) {
            return { id: 0, items: [] };
        }

        return {
            id: data.id,
            items: data.items.map(item => ({
                productId: item.productId,
                productNameAr: item.productNameAr,
                productNameEn: item.productNameEn,
                imageUrl: item.imageUrl ?? 'assets/placeholder.svg',
                unitPrice: item.unitPrice,
                stockQuantity: item.stockQuantity,
                isInStock: item.isInStock,
                addedAt: item.addedAt
            }))
        };
    }
}
