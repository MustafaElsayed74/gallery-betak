import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ReviewDto {
  id: number;
  productId: number;
  userId: string;
  userName: string;
  rating: number;
  comment: string;
  status: string;
  isVerifiedPurchase: boolean;
  createdAt: string;
}

interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  messageEn: string;
  data: T | null;
}

export interface CreateReviewRequest {
  productId: number;
  rating: number;
  comment?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private readonly URL = `${environment.apiUrl}/Reviews`;

  constructor(private http: HttpClient) { }

  getProductReviews(productId: number): Observable<ReviewDto[]> {
    return this.http.get<ApiResponse<ReviewDto[]>>(`${this.URL}/product/${productId}`).pipe(
      map(response => response.data ?? [])
    );
  }

  createReview(request: CreateReviewRequest): Observable<ReviewDto> {
    return this.http.post<ApiResponse<ReviewDto>>(this.URL, request).pipe(
      map(response => {
        if (!response.data) throw new Error(response.message);
        return response.data;
      })
    );
  }
}
