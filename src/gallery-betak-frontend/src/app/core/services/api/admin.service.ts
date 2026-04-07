import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
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

export interface PagedResult<T> {
    pageIndex: number;
    pageSize: number;
    count: number;
    data: T[];
}

export interface DashboardMetricsDto {
    totalUsers: number;
    totalOrders: number;
    totalRevenue: number;
    activeProducts: number;
    pendingOrders: number;
    cancelledOrders: number;
    lowStockProducts: number;
    revenueLast30Days: number;
    openSupportMessages: number;
    resolvedSupportMessages: number;
}

export interface AnalyticsDailyPointDto {
    date: string;
    revenue: number;
    ordersCount: number;
    discountAmount: number;
}

export interface AnalyticsOrderStatusBreakdownDto {
    status: string;
    count: number;
    revenue: number;
}

export interface CouponPerformanceDto {
    couponCode: string;
    ordersCount: number;
    totalDiscountAmount: number;
    totalRevenue: number;
    averageDiscountAmount: number;
}

export interface TopProductPerformanceDto {
    productId: number;
    productNameAr: string;
    productNameEn: string;
    quantitySold: number;
    revenue: number;
}

export interface DetailedAnalyticsDto {
    periodDays: number;
    revenueInPeriod: number;
    ordersInPeriod: number;
    averageOrderValue: number;
    discountsInPeriod: number;
    couponOrdersInPeriod: number;
    couponRevenueInPeriod: number;
    dailyTrend: AnalyticsDailyPointDto[];
    orderStatusBreakdown: AnalyticsOrderStatusBreakdownDto[];
    couponPerformance: CouponPerformanceDto[];
    topProducts: TopProductPerformanceDto[];
}

export type CouponDiscountType = 'Percentage' | 'FixedAmount';

export interface CouponAdminDto {
    id: number;
    code: string;
    descriptionAr: string | null;
    descriptionEn: string | null;
    discountType: CouponDiscountType;
    discountValue: number;
    minOrderAmount: number;
    maxDiscountAmount: number | null;
    usageLimit: number;
    usedCount: number;
    startsAt: string;
    expiresAt: string;
    isActive: boolean;
    isValid: boolean;
    createdAt: string;
}

export interface CreateCouponRequest {
    code: string;
    descriptionAr?: string;
    descriptionEn?: string;
    discountType: CouponDiscountType;
    discountValue: number;
    minOrderAmount: number;
    maxDiscountAmount?: number;
    usageLimit: number;
    startsAt: string;
    expiresAt: string;
    isActive: boolean;
}

export interface UpdateCouponRequest extends CreateCouponRequest { }

export interface OfferAdminDto {
    productId: number;
    productNameAr: string;
    productNameEn: string;
    slug: string;
    price: number;
    originalPrice: number | null;
    discountPercentage: number;
    stockQuantity: number;
    isActive: boolean;
    categoryNameAr: string | null;
    categoryNameEn: string | null;
}

export interface ApplyOfferDiscountRequest {
    discountedPrice?: number;
    discountPercentage?: number;
}

export interface AuditLogDto {
    id: number;
    userEmail: string | null;
    action: string;
    entityType: string;
    entityId: string | null;
    ipAddress: string | null;
    timestamp: string;
}

export interface UserManagementDto {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    isActive: boolean;
    createdAt: string;
    lastLoginAt: string | null;
    roles: string[];
}

export interface CreateAdminUserRequest {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    phoneNumber?: string;
    isActive: boolean;
    roles: string[];
}

export interface UpdateAdminUserRequest {
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber?: string;
    isActive: boolean;
    roles?: string[];
}

export interface OrderSummaryDto {
    id: number;
    orderNumber: string;
    status: string;
    totalAmount: number;
    createdAt: string;
    itemCount: number;
}

export interface UpdateOrderStatusRequest {
    status: number;
    trackingNumber?: string;
    reason?: string;
}

export interface CategoryAdminDto {
    id: number;
    nameAr: string;
    nameEn: string;
    slug: string;
    imageUrl: string | null;
    displayOrder: number;
    parentId?: number;
}

export interface CreateCategoryRequest {
    nameAr: string;
    nameEn: string;
    descriptionAr?: string;
    descriptionEn?: string;
    parentId?: number;
    imageUrl?: string;
    displayOrder: number;
}

export interface UpdateCategoryRequest extends CreateCategoryRequest {
    isActive: boolean;
}

interface BackendCategoryTreeDto {
    id: number;
    nameAr: string;
    nameEn: string;
    slug: string;
    imageUrl: string | null;
    displayOrder: number;
    subCategories: BackendCategoryTreeDto[];
}

export interface ProductAdminListDto {
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

export interface ProductAdminDetailDto {
    id: number;
    nameAr: string;
    nameEn: string;
    slug: string;
    descriptionAr: string | null;
    descriptionEn: string | null;
    price: number;
    originalPrice: number | null;
    sku: string;
    stockQuantity: number;
    category: { id: number; nameAr: string; nameEn: string; slug: string } | null;
    weight: number | null;
    dimensions: string | null;
    material: string | null;
    origin: string | null;
    isFeatured: boolean;
}

export interface CreateProductRequest {
    nameAr: string;
    nameEn: string;
    descriptionAr?: string;
    descriptionEn?: string;
    price: number;
    originalPrice?: number;
    sku: string;
    stockQuantity: number;
    categoryId: number;
    weight?: number;
    dimensions?: string;
    material?: string;
    origin?: string;
    isFeatured: boolean;
    tagIds: number[];
    imageUrls?: string[];
    sourceUrl?: string;
    importedAt?: string;
}

export interface ProductImportRequest {
    url: string;
    preferredCategoryId?: number;
}

export interface ProductImportPreviewDto {
    sourceUrl: string;
    sourceHost: string;
    nameAr: string;
    nameEn: string;
    descriptionAr: string | null;
    descriptionEn: string | null;
    price: number;
    originalPrice: number | null;
    suggestedSku: string;
    suggestedCategoryId: number | null;
    weight: number | null;
    dimensions: string | null;
    material: string | null;
    origin: string | null;
    currency: string | null;
    imageUrls: string[];
    warnings: string[];
}

export interface UpdateProductRequest {
    nameAr: string;
    nameEn: string;
    descriptionAr?: string;
    descriptionEn?: string;
    price: number;
    originalPrice?: number;
    stockQuantity: number;
    categoryId: number;
    weight?: number;
    dimensions?: string;
    material?: string;
    origin?: string;
    isFeatured: boolean;
    isActive: boolean;
    tagIds: number[];
}

export interface CustomerServiceMessageDto {
    id: number;
    name: string;
    email: string;
    phoneNumber: string | null;
    subject: string;
    message: string;
    status: string;
    adminNotes: string | null;
    handledByUserId: string | null;
    createdAt: string;
    updatedAt: string | null;
    resolvedAt: string | null;
}

export interface UpdateSupportMessageRequest {
    status: number;
    adminNotes?: string;
}

export interface CreateSupportMessageRequest {
    name: string;
    email: string;
    phoneNumber?: string;
    subject: string;
    message: string;
}

@Injectable({
    providedIn: 'root'
})
export class AdminService {
    private readonly ADMIN_URL = `${environment.apiUrl}/admin`;
    private readonly PRODUCTS_URL = `${environment.apiUrl}/Products`;
    private readonly CATEGORIES_URL = `${environment.apiUrl}/Categories`;
    private readonly SUPPORT_URL = `${environment.apiUrl}/support`;

    constructor(private http: HttpClient) { }

    getMetrics(): Observable<DashboardMetricsDto> {
        return this.http.get<ApiResponse<DashboardMetricsDto>>(`${this.ADMIN_URL}/metrics`).pipe(
            map(response => this.requirePayload(response, 'Metrics payload missing.'))
        );
    }

    getDetailedAnalytics(days = 30): Observable<DetailedAnalyticsDto> {
        const params = new HttpParams().set('days', days.toString());
        return this.http.get<ApiResponse<DetailedAnalyticsDto>>(`${this.ADMIN_URL}/analytics/detailed`, { params }).pipe(
            map(response => this.requirePayload(response, 'Detailed analytics payload missing.'))
        );
    }

    getUsers(page = 1, limit = 20, search = ''): Observable<PagedResult<UserManagementDto>> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('limit', limit.toString());

        if (search.trim()) {
            params = params.set('search', search.trim());
        }

        return this.http.get<ApiResponse<BackendPagedResult<UserManagementDto>>>(`${this.ADMIN_URL}/users`, { params }).pipe(
            map(response => this.mapPagedResult(this.requirePayload(response, 'Users payload missing.')))
        );
    }

    createUser(request: CreateAdminUserRequest): Observable<UserManagementDto> {
        return this.http.post<ApiResponse<UserManagementDto>>(`${this.ADMIN_URL}/users`, request).pipe(
            map(response => this.requirePayload(response, 'Create user payload missing.'))
        );
    }

    updateUser(userId: string, request: UpdateAdminUserRequest): Observable<UserManagementDto> {
        return this.http.put<ApiResponse<UserManagementDto>>(`${this.ADMIN_URL}/users/${userId}`, request).pipe(
            map(response => this.requirePayload(response, 'Update user payload missing.'))
        );
    }

    setUserStatus(userId: string, isActive: boolean): Observable<boolean> {
        return this.http.patch<ApiResponse<boolean>>(`${this.ADMIN_URL}/users/${userId}/status`, { isActive }).pipe(
            map(response => response.data ?? false)
        );
    }

    deleteUser(userId: string): Observable<boolean> {
        return this.http.delete<ApiResponse<boolean>>(`${this.ADMIN_URL}/users/${userId}`).pipe(
            map(response => response.data ?? false)
        );
    }

    assignRole(userId: string, role: string): Observable<boolean> {
        return this.http.post<ApiResponse<boolean>>(`${this.ADMIN_URL}/users/${userId}/roles`, { role }).pipe(
            map(response => response.data ?? false)
        );
    }

    getCoupons(page = 1, limit = 20, search = '', isActive?: boolean): Observable<PagedResult<CouponAdminDto>> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('limit', limit.toString());

        if (search.trim()) {
            params = params.set('search', search.trim());
        }

        if (typeof isActive === 'boolean') {
            params = params.set('isActive', isActive.toString());
        }

        return this.http.get<ApiResponse<BackendPagedResult<CouponAdminDto>>>(`${this.ADMIN_URL}/coupons`, { params }).pipe(
            map(response => this.mapPagedResult(this.requirePayload(response, 'Coupons payload missing.')))
        );
    }

    createCoupon(request: CreateCouponRequest): Observable<CouponAdminDto> {
        return this.http.post<ApiResponse<CouponAdminDto>>(`${this.ADMIN_URL}/coupons`, request).pipe(
            map(response => this.requirePayload(response, 'Create coupon payload missing.'))
        );
    }

    updateCoupon(couponId: number, request: UpdateCouponRequest): Observable<CouponAdminDto> {
        return this.http.put<ApiResponse<CouponAdminDto>>(`${this.ADMIN_URL}/coupons/${couponId}`, request).pipe(
            map(response => this.requirePayload(response, 'Update coupon payload missing.'))
        );
    }

    setCouponStatus(couponId: number, isActive: boolean): Observable<boolean> {
        return this.http.patch<ApiResponse<boolean>>(`${this.ADMIN_URL}/coupons/${couponId}/status`, { isActive }).pipe(
            map(response => response.data ?? false)
        );
    }

    deleteCoupon(couponId: number): Observable<boolean> {
        return this.http.delete<ApiResponse<boolean>>(`${this.ADMIN_URL}/coupons/${couponId}`).pipe(
            map(response => response.data ?? false)
        );
    }

    getOffers(page = 1, limit = 20, search = '', activeOnly?: boolean): Observable<PagedResult<OfferAdminDto>> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('limit', limit.toString());

        if (search.trim()) {
            params = params.set('search', search.trim());
        }

        if (typeof activeOnly === 'boolean') {
            params = params.set('activeOnly', activeOnly.toString());
        }

        return this.http.get<ApiResponse<BackendPagedResult<OfferAdminDto>>>(`${this.ADMIN_URL}/offers`, { params }).pipe(
            map(response => this.mapPagedResult(this.requirePayload(response, 'Offers payload missing.')))
        );
    }

    applyOfferDiscount(productId: number, request: ApplyOfferDiscountRequest): Observable<OfferAdminDto> {
        return this.http.patch<ApiResponse<OfferAdminDto>>(`${this.ADMIN_URL}/offers/${productId}/discount`, request).pipe(
            map(response => this.requirePayload(response, 'Apply offer payload missing.'))
        );
    }

    clearOfferDiscount(productId: number): Observable<boolean> {
        return this.http.delete<ApiResponse<boolean>>(`${this.ADMIN_URL}/offers/${productId}/discount`).pipe(
            map(response => response.data ?? false)
        );
    }

    getOrders(page = 1, limit = 20, status?: string): Observable<PagedResult<OrderSummaryDto>> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('limit', limit.toString());

        if (status?.trim()) {
            params = params.set('status', status.trim());
        }

        return this.http.get<ApiResponse<BackendPagedResult<OrderSummaryDto>>>(`${this.ADMIN_URL}/orders`, { params }).pipe(
            map(response => this.mapPagedResult(this.requirePayload(response, 'Orders payload missing.')))
        );
    }

    updateOrderStatus(orderId: number, request: UpdateOrderStatusRequest): Observable<boolean> {
        return this.http.patch<ApiResponse<boolean>>(`${this.ADMIN_URL}/orders/${orderId}/status`, request).pipe(
            map(response => response.data ?? false)
        );
    }

    getRecentOrders(): Observable<OrderSummaryDto[]> {
        return this.http.get<ApiResponse<OrderSummaryDto[]>>(`${this.ADMIN_URL}/orders/recent`).pipe(
            map(response => response.data ?? [])
        );
    }

    getAuditLogs(): Observable<AuditLogDto[]> {
        return this.http.get<ApiResponse<AuditLogDto[]>>(`${this.ADMIN_URL}/logs`).pipe(
            map(response => response.data ?? [])
        );
    }

    getSupportMessages(page = 1, limit = 20, status?: string, search?: string): Observable<PagedResult<CustomerServiceMessageDto>> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('limit', limit.toString());

        if (status?.trim()) {
            params = params.set('status', status.trim());
        }

        if (search?.trim()) {
            params = params.set('search', search.trim());
        }

        return this.http.get<ApiResponse<BackendPagedResult<CustomerServiceMessageDto>>>(`${this.ADMIN_URL}/support/messages`, { params }).pipe(
            map(response => this.mapPagedResult(this.requirePayload(response, 'Support messages payload missing.')))
        );
    }

    updateSupportMessage(messageId: number, request: UpdateSupportMessageRequest): Observable<CustomerServiceMessageDto> {
        return this.http.patch<ApiResponse<CustomerServiceMessageDto>>(`${this.ADMIN_URL}/support/messages/${messageId}`, request).pipe(
            map(response => this.requirePayload(response, 'Update support message payload missing.'))
        );
    }

    submitSupportMessage(request: CreateSupportMessageRequest): Observable<CustomerServiceMessageDto> {
        return this.http.post<ApiResponse<CustomerServiceMessageDto>>(`${this.SUPPORT_URL}/messages`, request).pipe(
            map(response => this.requirePayload(response, 'Submit support message payload missing.'))
        );
    }

    getCategories(): Observable<CategoryAdminDto[]> {
        return this.http.get<ApiResponse<BackendCategoryTreeDto[]>>(this.CATEGORIES_URL).pipe(
            map(response => this.flattenCategories(this.requirePayload(response, 'Categories payload missing.')))
        );
    }

    createCategory(request: CreateCategoryRequest): Observable<boolean> {
        return this.http.post<ApiResponse<unknown>>(this.CATEGORIES_URL, request).pipe(
            map(response => response.success)
        );
    }

    updateCategory(categoryId: number, request: UpdateCategoryRequest): Observable<boolean> {
        return this.http.put<ApiResponse<unknown>>(`${this.CATEGORIES_URL}/${categoryId}`, request).pipe(
            map(response => response.success)
        );
    }

    deleteCategory(categoryId: number): Observable<boolean> {
        return this.http.delete<ApiResponse<boolean>>(`${this.CATEGORIES_URL}/${categoryId}`).pipe(
            map(response => response.data ?? false)
        );
    }

    getProducts(page = 1, pageSize = 20, search = '', categoryId?: number): Observable<PagedResult<ProductAdminListDto>> {
        let params = new HttpParams()
            .set('pageNumber', page.toString())
            .set('pageSize', pageSize.toString());

        if (search.trim()) {
            params = params.set('search', search.trim());
        }

        if (categoryId) {
            params = params.set('categoryId', categoryId.toString());
        }

        return this.http.get<ApiResponse<BackendPagedResult<ProductAdminListDto>>>(this.PRODUCTS_URL, { params }).pipe(
            map(response => this.mapPagedResult(this.requirePayload(response, 'Products payload missing.')))
        );
    }

    getProduct(productId: number): Observable<ProductAdminDetailDto> {
        return this.http.get<ApiResponse<ProductAdminDetailDto>>(`${this.PRODUCTS_URL}/${productId}`).pipe(
            map(response => this.requirePayload(response, 'Product payload missing.'))
        );
    }

    createProduct(request: CreateProductRequest): Observable<boolean> {
        return this.http.post<ApiResponse<unknown>>(this.PRODUCTS_URL, request).pipe(
            map(response => response.success)
        );
    }

    importProductFromUrl(url: string, preferredCategoryId?: number): Observable<ProductImportPreviewDto> {
        const payload: ProductImportRequest = {
            url: url.trim(),
            preferredCategoryId
        };

        return this.http.post<ApiResponse<ProductImportPreviewDto>>(`${this.ADMIN_URL}/products/import`, payload).pipe(
            map(response => this.requirePayload(response, 'Product import payload missing.'))
        );
    }

    updateProduct(productId: number, request: UpdateProductRequest): Observable<boolean> {
        return this.http.put<ApiResponse<unknown>>(`${this.PRODUCTS_URL}/${productId}`, request).pipe(
            map(response => response.success)
        );
    }

    deleteProduct(productId: number): Observable<boolean> {
        return this.http.delete<ApiResponse<boolean>>(`${this.PRODUCTS_URL}/${productId}`).pipe(
            map(response => response.data ?? false)
        );
    }

    getLowStockProducts(threshold = 5): Observable<ProductAdminListDto[]> {
        const params = new HttpParams().set('threshold', threshold.toString());
        return this.http.get<ApiResponse<ProductAdminListDto[]>>(`${this.PRODUCTS_URL}/low-stock`, { params }).pipe(
            map(response => response.data ?? [])
        );
    }

    private mapPagedResult<T>(backend: BackendPagedResult<T>): PagedResult<T> {
        return {
            pageIndex: backend.pageNumber,
            pageSize: backend.pageSize,
            count: backend.totalCount,
            data: backend.items ?? []
        };
    }

    private flattenCategories(categories: BackendCategoryTreeDto[], parentId?: number): CategoryAdminDto[] {
        const result: CategoryAdminDto[] = [];

        categories.forEach(category => {
            result.push({
                id: category.id,
                nameAr: category.nameAr,
                nameEn: category.nameEn,
                slug: category.slug,
                imageUrl: category.imageUrl,
                displayOrder: category.displayOrder,
                parentId
            });

            if (category.subCategories?.length) {
                result.push(...this.flattenCategories(category.subCategories, category.id));
            }
        });

        return result;
    }

    private requirePayload<T>(response: ApiResponse<T>, errorMessage: string): T {
        if (!response.data) {
            throw new Error(errorMessage);
        }

        return response.data;
    }
}
