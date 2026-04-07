import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  messageEn: string;
  data: T | null;
}

interface BackendCategoryDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  imageUrl: string | null;
  displayOrder: number;
  subCategories: BackendCategoryDto[];
}

export interface CategoryDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  description: string;
  imageUrl: string;
  parentId?: number;
  subCategories: CategoryDto[];
}

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private readonly URL = `${environment.apiUrl}/Categories`;

  constructor(private http: HttpClient) { }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<ApiResponse<BackendCategoryDto[]>>(this.URL).pipe(
      map(response => this.mapCategories(response.data ?? []))
    );
  }

  getCategory(id: number): Observable<CategoryDto> {
    return this.http.get<ApiResponse<BackendCategoryDto>>(`${this.URL}/${id}`).pipe(
      map(response => this.mapCategory(response.data))
    );
  }

  getHierarchicalCategories(): Observable<CategoryDto[]> {
    return this.getCategories();
  }

  private mapCategories(categories: BackendCategoryDto[]): CategoryDto[] {
    return categories.map(category => this.mapCategory(category));
  }

  private mapCategory(category: BackendCategoryDto | null): CategoryDto {
    if (!category) {
      throw new Error('Category payload missing.');
    }

    return {
      id: category.id,
      nameAr: category.nameAr,
      nameEn: category.nameEn,
      slug: category.slug,
      description: '',
      imageUrl: category.imageUrl ?? '',
      parentId: undefined,
      subCategories: this.mapCategories(category.subCategories ?? [])
    };
  }
}
