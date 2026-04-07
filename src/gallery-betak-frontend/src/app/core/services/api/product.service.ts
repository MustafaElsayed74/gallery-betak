import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  messageEn: string;
  data: T | null;
}

interface BackendPagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

interface BackendProductListDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  price: number;
  originalPrice: number | null;
  discountPercentage: number | null;
  primaryImageUrl: string | null;
  averageRating: number;
  reviewCount: number;
  inStock: boolean;
  isFeatured: boolean;
  categoryNameAr: string | null;
  categoryNameEn: string | null;
}

interface BackendProductDetailDto extends BackendProductListDto {
  descriptionAr: string | null;
  descriptionEn: string | null;
  sku: string;
  stockQuantity: number;
  weight: number | null;
  dimensions: string | null;
  material: string | null;
  origin: string | null;
  viewCount: number;
  createdAt: string;
  images: Array<{ id: number; imageUrl: string; thumbnailUrl: string | null; altTextAr: string | null; altTextEn: string | null; isPrimary: boolean; displayOrder: number }>;
  category: { id: number; nameAr: string; nameEn: string; slug: string } | null;
}

export interface PagedResult<T> {
  pageIndex: number;
  pageSize: number;
  count: number;
  data: T[];
}

export interface ProductDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  descriptionAr: string;
  descriptionEn: string;
  price: number;
  originalPrice: number;
  discountPercentage: number;
  stockQuantity: number;
  weight: number | null;
  dimensions: string | null;
  material: string | null;
  origin: string | null;
  categoryId: number;
  categoryNameAr: string;
  categoryNameEn: string;
  categoryName?: string;
  primaryImage: string;
  imageUrl?: string;
  images: string[];
  attributes: Record<string, string>;
  isNew: boolean;
  isOnSale: boolean;
  averageRating: number;
  avgRating?: number;
  reviewsCount: number;
}

export interface ProductSpecParams {
  sort?: string;
  categoryId?: number;
  search?: string;
  pageIndex?: number;
  pageSize?: number;
  minPrice?: number;
  maxPrice?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly URL = `${environment.apiUrl}/Products`;

  constructor(private http: HttpClient) { }

  getProducts(specParams: ProductSpecParams): Observable<PagedResult<ProductDto>> {
    let params = new HttpParams();

    if (specParams.categoryId) params = params.append('categoryId', specParams.categoryId.toString());
    if (specParams.sort) params = params.append('sortBy', specParams.sort);
    if (specParams.search) params = params.append('search', specParams.search);
    if (specParams.pageIndex) params = params.append('pageNumber', specParams.pageIndex.toString());
    if (specParams.pageSize) params = params.append('pageSize', specParams.pageSize.toString());
    if (specParams.minPrice !== null && specParams.minPrice !== undefined) {
      params = params.append('minPrice', specParams.minPrice.toString());
    }
    if (specParams.maxPrice !== null && specParams.maxPrice !== undefined) {
      params = params.append('maxPrice', specParams.maxPrice.toString());
    }

    return this.http.get<ApiResponse<BackendPagedResult<BackendProductListDto>>>(this.URL, { params }).pipe(
      map(response => ({
        data: response.data?.items.map(item => this.mapListProduct(item)) ?? [],
        count: response.data?.totalCount ?? 0,
        pageIndex: response.data?.pageNumber ?? 1,
        pageSize: response.data?.pageSize ?? specParams.pageSize ?? 12
      }))
    );
  }

  getProduct(id: number): Observable<ProductDto> {
    return this.http.get<ApiResponse<BackendProductDetailDto>>(`${this.URL}/${id}`).pipe(
      map(response => this.mapDetailProduct(response.data))
    );
  }

  getProductBySlug(slug: string): Observable<ProductDto> {
    const encodedSlug = encodeURIComponent(slug);

    return this.http.get<ApiResponse<BackendProductDetailDto>>(`${this.URL}/by-slug/${encodedSlug}`).pipe(
      map(response => this.mapDetailProduct(response.data))
    );
  }

  private mapListProduct(item: BackendProductListDto | null): ProductDto {
    if (!item) {
      throw new Error('Product payload missing.');
    }

    return {
      id: item.id,
      nameAr: item.nameAr,
      nameEn: item.nameEn,
      slug: item.slug,
      descriptionAr: '',
      descriptionEn: '',
      price: item.price,
      originalPrice: item.originalPrice ?? item.price,
      discountPercentage: item.discountPercentage ?? 0,
      stockQuantity: item.inStock ? 1 : 0,
      weight: null,
      dimensions: null,
      material: null,
      origin: null,
      categoryId: 0,
      categoryNameAr: item.categoryNameAr ?? '',
      categoryNameEn: item.categoryNameEn ?? '',
      categoryName: item.categoryNameAr ?? item.categoryNameEn ?? '',
      primaryImage: item.primaryImageUrl ?? 'assets/placeholder.svg',
      imageUrl: item.primaryImageUrl ?? 'assets/placeholder.svg',
      images: item.primaryImageUrl ? [item.primaryImageUrl] : [],
      attributes: {},
      isNew: false,
      isOnSale: (item.discountPercentage ?? 0) > 0,
      averageRating: item.averageRating,
      avgRating: item.averageRating,
      reviewsCount: item.reviewCount
    };
  }

  private mapDetailProduct(item: BackendProductDetailDto | null): ProductDto {
    if (!item) {
      throw new Error('Product payload missing.');
    }

    return {
      ...this.mapListProduct(item),
      descriptionAr: item.descriptionAr ?? '',
      descriptionEn: item.descriptionEn ?? '',
      stockQuantity: item.stockQuantity,
      weight: item.weight,
      dimensions: item.dimensions,
      material: item.material,
      origin: item.origin,
      categoryId: item.category?.id ?? 0,
      categoryNameAr: item.category?.nameAr ?? item.categoryNameAr ?? '',
      categoryNameEn: item.category?.nameEn ?? item.categoryNameEn ?? '',
      categoryName: item.category?.nameAr ?? item.categoryNameAr ?? item.categoryNameEn ?? '',
      imageUrl: item.images[0]?.thumbnailUrl || item.images[0]?.imageUrl || item.primaryImageUrl || 'assets/placeholder.svg',
      images: item.images.map(image => image.thumbnailUrl || image.imageUrl),
      isNew: false
    };
  }
}
