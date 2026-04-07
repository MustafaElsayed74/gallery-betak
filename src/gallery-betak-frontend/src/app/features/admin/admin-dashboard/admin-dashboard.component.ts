import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    ApplyOfferDiscountRequest,
    AdminService,
    CouponAdminDto,
    CouponDiscountType,
    CreateCouponRequest,
    AuditLogDto,
    CategoryAdminDto,
    CreateAdminUserRequest,
    CreateCategoryRequest,
    CreateProductRequest,
    CustomerServiceMessageDto,
    DetailedAnalyticsDto,
    DashboardMetricsDto,
    OfferAdminDto,
    OrderSummaryDto,
    ProductImportPreviewDto,
    ProductAdminDetailDto,
    ProductAdminListDto,
    UpdateCouponRequest,
    UpdateAdminUserRequest,
    UpdateCategoryRequest,
    UpdateProductRequest,
    UserManagementDto
} from '../../../core/services/api/admin.service';
import { AuthService } from '../../../core/services/api/auth.service';
import { LanguageService } from '../../../core/services/language.service';

type AdminTab = 'overview' | 'users' | 'categories' | 'products' | 'orders' | 'support' | 'coupons' | 'offers' | 'analytics';
type UiOrderStatus = 'Pending' | 'Confirmed' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled' | 'Refunded';
type UiSupportStatus = 'New' | 'InProgress' | 'Resolved' | 'Closed';

const ORDER_STATUSES: UiOrderStatus[] = ['Pending', 'Confirmed', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Refunded'];
const ORDER_STATUS_TO_ENUM: Record<UiOrderStatus, number> = {
    Pending: 0,
    Confirmed: 1,
    Processing: 2,
    Shipped: 3,
    Delivered: 4,
    Cancelled: 5,
    Refunded: 6
};

const SUPPORT_STATUSES: UiSupportStatus[] = ['New', 'InProgress', 'Resolved', 'Closed'];
const SUPPORT_STATUS_TO_ENUM: Record<UiSupportStatus, number> = {
    New: 0,
    InProgress: 1,
    Resolved: 2,
    Closed: 3
};

const COUPON_DISCOUNT_TYPES: CouponDiscountType[] = ['Percentage', 'FixedAmount'];

@Component({
    selector: 'app-admin-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule],
    templateUrl: './admin-dashboard.component.html',
    styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
    private readonly adminService = inject(AdminService);
    private readonly authService = inject(AuthService);
    private readonly languageService = inject(LanguageService);
    private readonly destroyRef = inject(DestroyRef);

    currentUser = this.authService.currentUserValue;
    activeTab: AdminTab = 'overview';

    loadingOverview = false;
    loadingUsers = false;
    loadingCategories = false;
    loadingProducts = false;
    loadingOrders = false;
    loadingSupport = false;
    loadingCoupons = false;
    loadingOffers = false;
    loadingAnalytics = false;

    errorMessage = '';
    successMessage = '';

    metrics: DashboardMetricsDto | null = null;
    recentOrders: OrderSummaryDto[] = [];
    recentLogs: AuditLogDto[] = [];
    lowStockProducts: ProductAdminListDto[] = [];

    users: UserManagementDto[] = [];
    userPage = 1;
    userLimit = 10;
    userTotal = 0;
    userSearch = '';

    roleOptions = ['Customer', 'Admin', 'SuperAdmin'];

    showUserModal = false;
    isEditingUser = false;
    selectedUserId: string | null = null;
    userPassword = '';
    userForm: CreateAdminUserRequest = this.defaultCreateUserForm();

    showRoleModal = false;
    selectedRoleUser: UserManagementDto | null = null;
    selectedRole = '';
    isAssigningRole = false;
    roleAssignmentError = '';

    categories: CategoryAdminDto[] = [];
    showCategoryModal = false;
    selectedCategoryId: number | null = null;
    isEditingCategory = false;
    categoryForm: UpdateCategoryRequest = this.defaultCategoryForm();

    products: ProductAdminListDto[] = [];
    productPage = 1;
    productLimit = 10;
    productTotal = 0;
    productSearch = '';
    productCategoryFilter?: number;

    showProductModal = false;
    selectedProductId: number | null = null;
    isEditingProduct = false;
    productSku = '';
    productForm: UpdateProductRequest = this.defaultProductForm();
    productImportUrl = '';
    importingProduct = false;
    importWarnings: string[] = [];
    importedImageUrls: string[] = [];
    importedSourceUrl: string | undefined;

    orders: OrderSummaryDto[] = [];
    orderPage = 1;
    orderLimit = 10;
    orderTotal = 0;
    orderStatusFilter: UiOrderStatus = 'Pending';
    orderStatuses = ORDER_STATUSES;
    orderDrafts: Record<number, {
        status: UiOrderStatus
        trackingNumber: string
        reason: string
        isSubmitting: boolean
        success: boolean
        error: string | null
    }> = {};

    supportMessages: CustomerServiceMessageDto[] = [];
    supportPage = 1;
    supportLimit = 10;
    supportTotal = 0;
    supportSearch = '';
    supportStatusFilter = '';
    supportStatuses = SUPPORT_STATUSES;
    supportDrafts: Record<number, { status: UiSupportStatus; notes: string }> = {};

    coupons: CouponAdminDto[] = [];
    couponPage = 1;
    couponLimit = 10;
    couponTotal = 0;
    couponSearch = '';
    couponActiveFilter = '';
    couponDiscountTypes = COUPON_DISCOUNT_TYPES;
    showCouponModal = false;
    isEditingCoupon = false;
    selectedCouponId: number | null = null;
    couponForm: UpdateCouponRequest = this.defaultCouponForm();

    offers: OfferAdminDto[] = [];
    offerPage = 1;
    offerLimit = 10;
    offerTotal = 0;
    offerSearch = '';
    offerActiveOnly = false;
    selectedOfferProductId: number | null = null;
    showOfferModal = false;
    offerForm: ApplyOfferDiscountRequest = this.defaultOfferForm();
    offerMode: 'percentage' | 'price' = 'percentage';

    analyticsPeriodDays = 30;
    analytics: DetailedAnalyticsDto | null = null;
    analyticsRevenuePeak = 0;
    analyticsOrdersPeak = 0;
    analyticsDiscountPeak = 0;
    analyticsStatusPeak = 0;
    analyticsCouponOrdersPeak = 0;
    analyticsTopProductQtyPeak = 0;
    analyticsCouponOrderRate = 0;
    analyticsDiscountPressureRate = 0;

    showConfirmModal = false;
    confirmMessage = '';
    private confirmAction: (() => void) | null = null;

    ngOnInit(): void {
        this.loadOverview();
        this.loadUsers();
        this.loadCategories();
        this.loadProducts();
        this.loadOrders();
        this.loadSupportMessages();
        this.loadCoupons();
        this.loadOffers();
        this.loadAnalytics();
    }

    get isArabic(): boolean {
        return this.languageService.currentLanguage === 'ar';
    }

    t(en: string, ar: string): string {
        return this.isArabic ? ar : en;
    }

    displayName(nameEn?: string | null, nameAr?: string | null): string {
        if (this.isArabic) {
            return nameAr?.trim() || nameEn?.trim() || this.t('N/A', 'غير متاح');
        }

        return nameEn?.trim() || nameAr?.trim() || this.t('N/A', 'غير متاح');
    }

    displayOrderStatus(status: string): string {
        const statusMap: Record<string, string> = {
            Pending: 'قيد الانتظار',
            Confirmed: 'مؤكد',
            Processing: 'قيد المعالجة',
            Shipped: 'تم الشحن',
            Delivered: 'تم التسليم',
            Cancelled: 'ملغي',
            Refunded: 'مسترجع'
        };

        return this.isArabic ? (statusMap[status] || status) : status;
    }

    displaySupportStatus(status: string): string {
        const statusMap: Record<string, string> = {
            New: 'جديد',
            InProgress: 'قيد التنفيذ',
            Resolved: 'تم الحل',
            Closed: 'مغلق'
        };

        return this.isArabic ? (statusMap[status] || status) : status;
    }

    displayRole(role: string): string {
        const roleMap: Record<string, string> = {
            Customer: 'عميل',
            Admin: 'مشرف',
            SuperAdmin: 'مشرف عام'
        };

        return this.isArabic ? (roleMap[role] || role) : role;
    }

    setTab(tab: AdminTab): void {
        this.activeTab = tab;
        this.clearFlash();
    }

    refreshAll(): void {
        this.loadOverview();
        this.loadUsers();
        this.loadCategories();
        this.loadProducts();
        this.loadOrders();
        this.loadSupportMessages();
        this.loadCoupons();
        this.loadOffers();
        this.loadAnalytics();
    }

    loadOverview(): void {
        this.loadingOverview = true;

        this.adminService.getMetrics()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: metrics => this.metrics = metrics,
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to load dashboard metrics.', 'تعذر تحميل مؤشرات لوحة التحكم.')))
            });

        this.adminService.getRecentOrders()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: orders => this.recentOrders = orders,
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to load recent orders.', 'تعذر تحميل أحدث الطلبات.')))
            });

        this.adminService.getAuditLogs()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: logs => this.recentLogs = logs,
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to load audit logs.', 'تعذر تحميل سجلات التدقيق.')))
            });

        this.adminService.getLowStockProducts(2)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: products => {
                    this.lowStockProducts = products;
                    this.loadingOverview = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load low stock products.', 'تعذر تحميل المنتجات منخفضة المخزون.')));
                    this.loadingOverview = false;
                }
            });
    }

    loadUsers(): void {
        this.loadingUsers = true;

        this.adminService.getUsers(this.userPage, this.userLimit, this.userSearch)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.users = result.data;
                    this.userTotal = result.count;
                    this.loadingUsers = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load users.', 'تعذر تحميل المستخدمين.')));
                    this.loadingUsers = false;
                }
            });
    }

    previousUsersPage(): void {
        if (this.userPage > 1) {
            this.userPage -= 1;
            this.loadUsers();
        }
    }

    nextUsersPage(): void {
        if (this.userPage * this.userLimit < this.userTotal) {
            this.userPage += 1;
            this.loadUsers();
        }
    }

    openCreateUserModal(): void {
        this.isEditingUser = false;
        this.selectedUserId = null;
        this.userPassword = '';
        this.userForm = this.defaultCreateUserForm();
        this.showUserModal = true;
    }

    openEditUserModal(user: UserManagementDto): void {
        this.isEditingUser = true;
        this.selectedUserId = user.id;
        this.userPassword = '';
        this.userForm = {
            firstName: user.firstName,
            lastName: user.lastName,
            email: user.email,
            password: '',
            phoneNumber: user.phoneNumber,
            isActive: user.isActive,
            roles: [...user.roles]
        };
        this.showUserModal = true;
    }

    closeUserModal(): void {
        this.showUserModal = false;
        this.selectedUserId = null;
        this.userPassword = '';
        this.userForm = this.defaultCreateUserForm();
    }

    isRoleSelected(role: string): boolean {
        return this.userForm.roles.includes(role);
    }

    toggleRoleSelection(role: string): void {
        if (this.userForm.roles.includes(role)) {
            this.userForm.roles = this.userForm.roles.filter(item => item !== role);
            if (this.userForm.roles.length === 0) {
                this.userForm.roles = ['Customer'];
            }
            return;
        }

        this.userForm.roles = [...this.userForm.roles, role];
    }

    saveUser(): void {
        this.clearFlash();

        if (!this.userForm.roles.length) {
            this.userForm.roles = ['Customer'];
        }

        if (this.isEditingUser && this.selectedUserId) {
            const updatePayload: UpdateAdminUserRequest = {
                firstName: this.userForm.firstName.trim(),
                lastName: this.userForm.lastName.trim(),
                email: this.userForm.email.trim(),
                phoneNumber: this.userForm.phoneNumber?.trim() || undefined,
                isActive: this.userForm.isActive,
                roles: this.userForm.roles
            };

            this.adminService.updateUser(this.selectedUserId, updatePayload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: () => {
                        this.closeUserModal();
                        this.loadUsers();
                        this.loadOverview();
                        this.showSuccess(this.t('User updated successfully.', 'تم تحديث المستخدم بنجاح.'));
                    },
                    error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update user.', 'تعذر تحديث المستخدم.')))
                });

            return;
        }

        if (!this.userPassword.trim()) {
            this.showError(this.t('Password is required when creating user.', 'كلمة المرور مطلوبة عند إنشاء المستخدم.'));
            return;
        }

        const createPayload: CreateAdminUserRequest = {
            firstName: this.userForm.firstName.trim(),
            lastName: this.userForm.lastName.trim(),
            email: this.userForm.email.trim(),
            password: this.userPassword,
            phoneNumber: this.userForm.phoneNumber?.trim() || undefined,
            isActive: this.userForm.isActive,
            roles: this.userForm.roles
        };

        this.adminService.createUser(createPayload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.closeUserModal();
                    this.loadUsers();
                    this.loadOverview();
                    this.showSuccess(this.t('User created successfully.', 'تم إنشاء المستخدم بنجاح.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to create user.', 'تعذر إنشاء المستخدم.')))
            });
    }

    toggleUserStatus(user: UserManagementDto): void {
        this.clearFlash();
        this.adminService.setUserStatus(user.id, !user.isActive)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.loadUsers();
                    this.showSuccess(this.t('User status updated.', 'تم تحديث حالة المستخدم.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update user status.', 'تعذر تحديث حالة المستخدم.')))
            });
    }

    deleteUser(user: UserManagementDto): void {
        this.clearFlash();

        this.openConfirmDialog(
            this.t(
                `Delete user ${user.firstName} ${user.lastName}?`,
                `هل تريد حذف المستخدم ${user.firstName} ${user.lastName}؟`
            ),
            () => {
                this.adminService.deleteUser(user.id)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                        next: () => {
                            this.loadUsers();
                            this.loadOverview();
                            this.showSuccess(this.t('User deleted successfully.', 'تم حذف المستخدم بنجاح.'));
                        },
                        error: error => this.showError(this.getErrorMessage(error, this.t('Unable to delete user.', 'تعذر حذف المستخدم.')))
                    });
            }
        );
    }

    openRoleModal(user: UserManagementDto): void {
        this.selectedRoleUser = user;
        this.selectedRole = '';
        this.roleAssignmentError = '';
        this.showRoleModal = true;
    }

    closeRoleModal(): void {
        this.selectedRoleUser = null;
        this.selectedRole = '';
        this.roleAssignmentError = '';
        this.isAssigningRole = false;
        this.showRoleModal = false;
    }

    assignRoleToUser(): void {
        if (!this.selectedRoleUser || !this.selectedRole) {
            this.roleAssignmentError = this.t('Please select role and user.', 'يرجى اختيار الدور والمستخدم.');
            return;
        }

        this.isAssigningRole = true;
        this.roleAssignmentError = '';

        this.adminService.assignRole(this.selectedRoleUser.id, this.selectedRole)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.closeRoleModal();
                    this.loadUsers();
                    this.loadOverview();
                    this.showSuccess(this.t('Role assigned successfully.', 'تم تعيين الدور بنجاح.'));
                },
                error: error => {
                    this.roleAssignmentError = this.getErrorMessage(error, this.t('Unable to assign role.', 'تعذر تعيين الدور.'));
                    this.isAssigningRole = false;
                }
            });
    }

    loadCategories(): void {
        this.loadingCategories = true;

        this.adminService.getCategories()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: categories => {
                    this.categories = categories;
                    this.loadingCategories = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load categories.', 'تعذر تحميل الفئات.')));
                    this.loadingCategories = false;
                }
            });
    }

    openCreateCategoryModal(): void {
        this.selectedCategoryId = null;
        this.isEditingCategory = false;
        this.categoryForm = this.defaultCategoryForm();
        this.showCategoryModal = true;
    }

    openEditCategoryModal(category: CategoryAdminDto): void {
        this.selectedCategoryId = category.id;
        this.isEditingCategory = true;
        this.categoryForm = {
            nameAr: category.nameAr,
            nameEn: category.nameEn,
            parentId: category.parentId,
            imageUrl: category.imageUrl ?? undefined,
            displayOrder: category.displayOrder,
            descriptionAr: undefined,
            descriptionEn: undefined,
            isActive: true
        };
        this.showCategoryModal = true;
    }

    closeCategoryModal(): void {
        this.showCategoryModal = false;
        this.selectedCategoryId = null;
        this.isEditingCategory = false;
        this.categoryForm = this.defaultCategoryForm();
    }

    saveCategory(): void {
        this.clearFlash();

        const payload = {
            ...this.categoryForm,
            nameAr: this.categoryForm.nameAr.trim(),
            nameEn: this.categoryForm.nameEn.trim(),
            parentId: this.categoryForm.parentId ? Number(this.categoryForm.parentId) : undefined,
            displayOrder: Number(this.categoryForm.displayOrder)
        };

        if (this.isEditingCategory && this.selectedCategoryId) {
            this.adminService.updateCategory(this.selectedCategoryId, payload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: () => {
                        this.closeCategoryModal();
                        this.loadCategories();
                        this.showSuccess(this.t('Category updated successfully.', 'تم تحديث الفئة بنجاح.'));
                    },
                    error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update category.', 'تعذر تحديث الفئة.')))
                });

            return;
        }

        const createPayload: CreateCategoryRequest = {
            nameAr: payload.nameAr,
            nameEn: payload.nameEn,
            descriptionAr: payload.descriptionAr,
            descriptionEn: payload.descriptionEn,
            parentId: payload.parentId,
            imageUrl: payload.imageUrl,
            displayOrder: payload.displayOrder
        };

        this.adminService.createCategory(createPayload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.closeCategoryModal();
                    this.loadCategories();
                    this.showSuccess(this.t('Category created successfully.', 'تم إنشاء الفئة بنجاح.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to create category.', 'تعذر إنشاء الفئة.')))
            });
    }

    deleteCategory(category: CategoryAdminDto): void {
        this.clearFlash();

        this.openConfirmDialog(
            this.t(
                `Delete category ${category.nameEn}?`,
                `هل تريد حذف الفئة ${category.nameAr || category.nameEn}؟`
            ),
            () => {
                this.adminService.deleteCategory(category.id)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                        next: () => {
                            this.loadCategories();
                            this.showSuccess(this.t('Category deleted successfully.', 'تم حذف الفئة بنجاح.'));
                        },
                        error: error => this.showError(this.getErrorMessage(error, this.t('Unable to delete category.', 'تعذر حذف الفئة.')))
                    });
            }
        );
    }

    loadProducts(): void {
        this.loadingProducts = true;

        this.adminService.getProducts(this.productPage, this.productLimit, this.productSearch, this.productCategoryFilter)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.products = result.data;
                    this.productTotal = result.count;
                    this.loadingProducts = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load products.', 'تعذر تحميل المنتجات.')));
                    this.loadingProducts = false;
                }
            });
    }

    previousProductsPage(): void {
        if (this.productPage > 1) {
            this.productPage -= 1;
            this.loadProducts();
        }
    }

    nextProductsPage(): void {
        if (this.productPage * this.productLimit < this.productTotal) {
            this.productPage += 1;
            this.loadProducts();
        }
    }

    openCreateProductModal(): void {
        this.selectedProductId = null;
        this.isEditingProduct = false;
        this.productSku = '';
        this.productForm = this.defaultProductForm();
        this.productImportUrl = '';
        this.importWarnings = [];
        this.importedImageUrls = [];
        this.importedSourceUrl = undefined;
        this.importingProduct = false;
        this.showProductModal = true;
    }

    openEditProductModal(product: ProductAdminListDto): void {
        this.clearFlash();

        this.adminService.getProduct(product.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: detail => {
                    this.setProductFormFromDetail(detail);
                    this.productSku = detail.sku;
                    this.productImportUrl = '';
                    this.importWarnings = [];
                    this.importedImageUrls = [];
                    this.importedSourceUrl = undefined;
                    this.selectedProductId = product.id;
                    this.isEditingProduct = true;
                    this.showProductModal = true;
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to load product details.', 'تعذر تحميل تفاصيل المنتج.')))
            });
    }

    closeProductModal(): void {
        this.showProductModal = false;
        this.selectedProductId = null;
        this.isEditingProduct = false;
        this.productSku = '';
        this.productForm = this.defaultProductForm();
        this.productImportUrl = '';
        this.importWarnings = [];
        this.importedImageUrls = [];
        this.importedSourceUrl = undefined;
        this.importingProduct = false;
    }

    importProductFromUrl(): void {
        this.clearFlash();

        const importUrl = this.productImportUrl.trim();
        if (!importUrl) {
            this.showError(this.t('Please provide a product URL to import.', 'يرجى إدخال رابط المنتج للاستيراد.'));
            return;
        }

        this.importingProduct = true;

        const preferredCategoryId = this.productForm.categoryId > 0
            ? this.productForm.categoryId
            : undefined;

        this.adminService.importProductFromUrl(importUrl, preferredCategoryId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: preview => {
                    this.applyImportedPreview(preview);
                    this.importWarnings = preview.warnings ?? [];
                    this.importedImageUrls = preview.imageUrls ?? [];
                    this.importedSourceUrl = preview.sourceUrl;
                    this.importingProduct = false;
                    this.showSuccess(this.t('Import preview loaded. Review and save.', 'تم تحميل معاينة الاستيراد. راجع البيانات ثم احفظ.'));
                },
                error: error => {
                    this.importingProduct = false;
                    this.showError(this.getErrorMessage(error, this.t('Unable to import from URL.', 'تعذر الاستيراد من الرابط.')));
                }
            });
    }

    saveProduct(): void {
        this.clearFlash();

        const payload = this.normalizeProductPayload(this.productForm);

        if (this.isEditingProduct && this.selectedProductId) {
            this.adminService.updateProduct(this.selectedProductId, payload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: () => {
                        this.closeProductModal();
                        this.loadProducts();
                        this.loadOverview();
                        this.showSuccess(this.t('Product updated successfully.', 'تم تحديث المنتج بنجاح.'));
                    },
                    error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update product.', 'تعذر تحديث المنتج.')))
                });

            return;
        }

        const createPayload: CreateProductRequest = {
            ...payload,
            sku: this.productSku.trim(),
            tagIds: payload.tagIds,
            imageUrls: this.importedImageUrls,
            sourceUrl: this.importedSourceUrl,
            importedAt: this.importedSourceUrl ? new Date().toISOString() : undefined
        };

        if (!createPayload.sku) {
            this.showError(this.t('SKU is required when creating product.', 'رمز المنتج (SKU) مطلوب عند إنشاء المنتج.'));
            return;
        }

        this.adminService.createProduct(createPayload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.closeProductModal();
                    this.loadProducts();
                    this.loadOverview();
                    this.showSuccess(this.t('Product created successfully.', 'تم إنشاء المنتج بنجاح.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to create product.', 'تعذر إنشاء المنتج.')))
            });
    }

    deleteProduct(product: ProductAdminListDto): void {
        this.clearFlash();

        this.openConfirmDialog(
            this.t(
                `Delete product ${product.nameEn}?`,
                `هل تريد حذف المنتج ${product.nameAr || product.nameEn}؟`
            ),
            () => {
                this.adminService.deleteProduct(product.id)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                        next: () => {
                            this.loadProducts();
                            this.loadOverview();
                            this.showSuccess(this.t('Product deleted successfully.', 'تم حذف المنتج بنجاح.'));
                        },
                        error: error => this.showError(this.getErrorMessage(error, this.t('Unable to delete product.', 'تعذر حذف المنتج.')))
                    });
            }
        );
    }

    openConfirmDialog(message: string, onConfirm: () => void): void {
        this.confirmMessage = message;
        this.confirmAction = onConfirm;
        this.showConfirmModal = true;
    }

    closeConfirmModal(): void {
        this.showConfirmModal = false;
        this.confirmMessage = '';
        this.confirmAction = null;
    }

    confirmModalAction(): void {
        const action = this.confirmAction;
        this.closeConfirmModal();
        action?.();
    }

    loadOrders(): void {
        this.loadingOrders = true;

        this.adminService.getOrders(this.orderPage, this.orderLimit, this.orderStatusFilter)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.orders = result.data;
                    this.orderTotal = result.count;
                    this.orders.forEach(order => {
                        if (!this.orderDrafts[order.id]) {
                            this.orderDrafts[order.id] = {
                                status: this.toOrderStatus(order.status),
                                trackingNumber: '',
                                reason: '',
                                isSubmitting: false,
                                success: false,
                                error: null
                            };
                        }
                    });
                    this.loadingOrders = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load orders.', 'تعذر تحميل الطلبات.')));
                    this.loadingOrders = false;
                }
            });
    }

    previousOrdersPage(): void {
        if (this.orderPage > 1) {
            this.orderPage -= 1;
            this.loadOrders();
        }
    }

    nextOrdersPage(): void {
        if (this.orderPage * this.orderLimit < this.orderTotal) {
            this.orderPage += 1;
            this.loadOrders();
        }
    }

    submitOrder(order: OrderSummaryDto): void {
        if (!this.orderDrafts[order.id]) {
            this.orderDrafts[order.id] = {
                status: 'Confirmed',
                trackingNumber: '',
                reason: '',
                isSubmitting: false,
                success: false,
                error: null
            };
        }

        this.orderDrafts[order.id].status = 'Confirmed';
        this.applyOrderStatus(order.id);
    }

    applyOrderStatus(orderId: number): void {
        this.clearFlash();
        const draft = this.orderDrafts[orderId];

        if (!draft) {
            this.orderDrafts[orderId] = {
                status: 'Confirmed',
                trackingNumber: '',
                reason: '',
                isSubmitting: false,
                success: false,
                error: null
            };
            return;
        }

        draft.isSubmitting = true;
        draft.success = false;
        draft.error = null;

        this.adminService.updateOrderStatus(orderId, {
            status: ORDER_STATUS_TO_ENUM[draft.status],
            trackingNumber: draft.trackingNumber?.trim() || undefined,
            reason: draft.reason?.trim() || undefined
        })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    draft.isSubmitting = false;
                    draft.success = true;
                    draft.error = null;

                    this.showSuccess(
                        draft.status === 'Confirmed'
                            ? this.t('Order confirmed and moved out of the pending queue.', 'تم تأكيد الطلب ونقله من قائمة الطلبات المعلقة.')
                            : this.t('Order status updated and pending queue refreshed.', 'تم تحديث حالة الطلب وتحديث قائمة الطلبات المعلقة.')
                    );

                    // Auto-hide success message after 3 seconds
                    setTimeout(() => {
                        draft.success = false;
                        this.successMessage = '';
                    }, 3000);

                    this.orderPage = 1;

                    // Reload data after short delay to make visual feedback noticeable.
                    setTimeout(() => {
                        this.loadOrders();
                        this.loadOverview();
                    }, 350);
                },
                error: error => {
                    draft.isSubmitting = false;
                    draft.success = false;
                    draft.error = this.getErrorMessage(error, this.t('Unable to update order status.', 'تعذر تحديث حالة الطلب.'));

                    // Auto-hide error message after 5 seconds
                    setTimeout(() => {
                        draft.error = null;
                    }, 5000);
                }
            });
    }

    loadSupportMessages(): void {
        this.loadingSupport = true;

        this.adminService.getSupportMessages(this.supportPage, this.supportLimit, this.supportStatusFilter || undefined, this.supportSearch)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.supportMessages = result.data;
                    this.supportTotal = result.count;

                    this.supportMessages.forEach(message => {
                        if (!this.supportDrafts[message.id]) {
                            this.supportDrafts[message.id] = {
                                status: this.toSupportStatus(message.status),
                                notes: message.adminNotes ?? ''
                            };
                        }
                    });

                    this.loadingSupport = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load support messages.', 'تعذر تحميل رسائل الدعم.')));
                    this.loadingSupport = false;
                }
            });
    }

    previousSupportPage(): void {
        if (this.supportPage > 1) {
            this.supportPage -= 1;
            this.loadSupportMessages();
        }
    }

    nextSupportPage(): void {
        if (this.supportPage * this.supportLimit < this.supportTotal) {
            this.supportPage += 1;
            this.loadSupportMessages();
        }
    }

    applySupportUpdate(messageId: number): void {
        this.clearFlash();

        const draft = this.supportDrafts[messageId];
        if (!draft) {
            this.showError(this.t('Support update data is missing.', 'بيانات تحديث الدعم غير موجودة.'));
            return;
        }

        this.adminService.updateSupportMessage(messageId, {
            status: SUPPORT_STATUS_TO_ENUM[draft.status],
            adminNotes: draft.notes?.trim() || undefined
        })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.loadSupportMessages();
                    this.loadOverview();
                    this.showSuccess(this.t('Support message updated successfully.', 'تم تحديث رسالة الدعم بنجاح.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update support message.', 'تعذر تحديث رسالة الدعم.')))
            });
    }

    loadCoupons(): void {
        this.loadingCoupons = true;

        const isActive = this.couponActiveFilter === ''
            ? undefined
            : this.couponActiveFilter === 'true';

        this.adminService.getCoupons(this.couponPage, this.couponLimit, this.couponSearch, isActive)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.coupons = result.data;
                    this.couponTotal = result.count;
                    this.loadingCoupons = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load coupons.', 'تعذر تحميل الكوبونات.')));
                    this.loadingCoupons = false;
                }
            });
    }

    previousCouponsPage(): void {
        if (this.couponPage > 1) {
            this.couponPage -= 1;
            this.loadCoupons();
        }
    }

    nextCouponsPage(): void {
        if (this.couponPage * this.couponLimit < this.couponTotal) {
            this.couponPage += 1;
            this.loadCoupons();
        }
    }

    openCreateCouponModal(): void {
        this.isEditingCoupon = false;
        this.selectedCouponId = null;
        this.couponForm = this.defaultCouponForm();
        this.showCouponModal = true;
    }

    openEditCouponModal(coupon: CouponAdminDto): void {
        this.isEditingCoupon = true;
        this.selectedCouponId = coupon.id;
        this.couponForm = {
            code: coupon.code,
            descriptionAr: coupon.descriptionAr ?? undefined,
            descriptionEn: coupon.descriptionEn ?? undefined,
            discountType: coupon.discountType,
            discountValue: coupon.discountValue,
            minOrderAmount: coupon.minOrderAmount,
            maxDiscountAmount: coupon.maxDiscountAmount ?? undefined,
            usageLimit: coupon.usageLimit,
            startsAt: this.toDateTimeLocal(coupon.startsAt),
            expiresAt: this.toDateTimeLocal(coupon.expiresAt),
            isActive: coupon.isActive
        };
        this.showCouponModal = true;
    }

    closeCouponModal(): void {
        this.showCouponModal = false;
        this.selectedCouponId = null;
        this.isEditingCoupon = false;
        this.couponForm = this.defaultCouponForm();
    }

    saveCoupon(): void {
        this.clearFlash();

        const startsAt = new Date(this.couponForm.startsAt);
        const expiresAt = new Date(this.couponForm.expiresAt);

        if (Number.isNaN(startsAt.getTime()) || Number.isNaN(expiresAt.getTime())) {
            this.showError(this.t('Please provide valid start and expiry dates.', 'يرجى إدخال تاريخ بداية وانتهاء صالحين.'));
            return;
        }

        if (expiresAt <= startsAt) {
            this.showError(this.t('Expiry date must be after start date.', 'تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.'));
            return;
        }

        const payload = this.normalizeCouponPayload(this.couponForm);

        if (this.isEditingCoupon && this.selectedCouponId) {
            this.adminService.updateCoupon(this.selectedCouponId, payload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: () => {
                        this.closeCouponModal();
                        this.loadCoupons();
                        this.loadAnalytics();
                        this.showSuccess(this.t('Coupon updated successfully.', 'تم تحديث الكوبون بنجاح.'));
                    },
                    error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update coupon.', 'تعذر تحديث الكوبون.')))
                });

            return;
        }

        const createPayload: CreateCouponRequest = { ...payload };

        this.adminService.createCoupon(createPayload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.closeCouponModal();
                    this.loadCoupons();
                    this.loadAnalytics();
                    this.showSuccess(this.t('Coupon created successfully.', 'تم إنشاء الكوبون بنجاح.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to create coupon.', 'تعذر إنشاء الكوبون.')))
            });
    }

    toggleCouponStatus(coupon: CouponAdminDto): void {
        this.clearFlash();

        this.adminService.setCouponStatus(coupon.id, !coupon.isActive)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.loadCoupons();
                    this.loadAnalytics();
                    this.showSuccess(this.t('Coupon status updated.', 'تم تحديث حالة الكوبون.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to update coupon status.', 'تعذر تحديث حالة الكوبون.')))
            });
    }

    deleteCoupon(coupon: CouponAdminDto): void {
        this.clearFlash();

        this.openConfirmDialog(
            this.t(`Delete coupon ${coupon.code}?`, `هل تريد حذف الكوبون ${coupon.code}؟`),
            () => {
                this.adminService.deleteCoupon(coupon.id)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                        next: () => {
                            this.loadCoupons();
                            this.loadAnalytics();
                            this.showSuccess(this.t('Coupon deleted successfully.', 'تم حذف الكوبون بنجاح.'));
                        },
                        error: error => this.showError(this.getErrorMessage(error, this.t('Unable to delete coupon.', 'تعذر حذف الكوبون.')))
                    });
            }
        );
    }

    loadOffers(): void {
        this.loadingOffers = true;

        this.adminService.getOffers(this.offerPage, this.offerLimit, this.offerSearch, this.offerActiveOnly)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.offers = result.data;
                    this.offerTotal = result.count;
                    this.loadingOffers = false;
                },
                error: error => {
                    this.showError(this.getErrorMessage(error, this.t('Unable to load offers.', 'تعذر تحميل العروض.')));
                    this.loadingOffers = false;
                }
            });
    }

    previousOffersPage(): void {
        if (this.offerPage > 1) {
            this.offerPage -= 1;
            this.loadOffers();
        }
    }

    nextOffersPage(): void {
        if (this.offerPage * this.offerLimit < this.offerTotal) {
            this.offerPage += 1;
            this.loadOffers();
        }
    }

    openOfferModal(product: ProductAdminListDto | OfferAdminDto): void {
        const productId = 'productId' in product ? product.productId : product.id;
        const currentPrice = 'productId' in product ? product.price : product.price;

        this.selectedOfferProductId = productId;
        this.offerMode = 'percentage';
        this.offerForm = {
            discountPercentage: 10,
            discountedPrice: undefined
        };

        if ('productId' in product && product.originalPrice) {
            this.offerMode = 'price';
            this.offerForm = {
                discountedPrice: product.price,
                discountPercentage: undefined
            };
        }

        if (!currentPrice || currentPrice <= 0) {
            this.offerForm.discountPercentage = 10;
        }

        this.showOfferModal = true;
    }

    closeOfferModal(): void {
        this.showOfferModal = false;
        this.selectedOfferProductId = null;
        this.offerForm = this.defaultOfferForm();
        this.offerMode = 'percentage';
    }

    saveOffer(): void {
        this.clearFlash();

        if (!this.selectedOfferProductId) {
            this.showError(this.t('Select product first.', 'يرجى اختيار المنتج أولاً.'));
            return;
        }

        const payload: ApplyOfferDiscountRequest = this.offerMode === 'percentage'
            ? {
                discountPercentage: this.offerForm.discountPercentage ? Number(this.offerForm.discountPercentage) : undefined,
                discountedPrice: undefined
            }
            : {
                discountedPrice: this.offerForm.discountedPrice ? Number(this.offerForm.discountedPrice) : undefined,
                discountPercentage: undefined
            };

        this.adminService.applyOfferDiscount(this.selectedOfferProductId, payload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.closeOfferModal();
                    this.loadOffers();
                    this.loadProducts();
                    this.loadAnalytics();
                    this.showSuccess(this.t('Offer saved successfully.', 'تم حفظ العرض بنجاح.'));
                },
                error: error => this.showError(this.getErrorMessage(error, this.t('Unable to save offer.', 'تعذر حفظ العرض.')))
            });
    }

    clearOffer(offer: OfferAdminDto): void {
        this.clearFlash();

        this.openConfirmDialog(
            this.t(`Clear discount for ${this.displayName(offer.productNameEn, offer.productNameAr)}?`, `هل تريد إزالة الخصم من ${this.displayName(offer.productNameEn, offer.productNameAr)}؟`),
            () => {
                this.adminService.clearOfferDiscount(offer.productId)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                        next: () => {
                            this.loadOffers();
                            this.loadProducts();
                            this.loadAnalytics();
                            this.showSuccess(this.t('Offer removed successfully.', 'تمت إزالة العرض بنجاح.'));
                        },
                        error: error => this.showError(this.getErrorMessage(error, this.t('Unable to remove offer.', 'تعذر إزالة العرض.')))
                    });
            }
        );
    }

    loadAnalytics(): void {
        this.loadingAnalytics = true;

        this.adminService.getDetailedAnalytics(this.analyticsPeriodDays)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: data => {
                    this.analytics = data;
                    this.recalculateAnalyticsScales();
                    this.loadingAnalytics = false;
                },
                error: error => {
                    this.analytics = null;
                    this.recalculateAnalyticsScales();
                    this.showError(this.getErrorMessage(error, this.t('Unable to load analytics.', 'تعذر تحميل التحليلات.')));
                    this.loadingAnalytics = false;
                }
            });
    }

    applyAnalyticsPeriod(days: number): void {
        this.analyticsPeriodDays = days;
        this.loadAnalytics();
    }

    dailyRevenueBarWidth(value: number): number {
        return this.toBarWidth(value, this.analyticsRevenuePeak, 5);
    }

    dailyOrdersBarWidth(value: number): number {
        return this.toBarWidth(value, this.analyticsOrdersPeak, 5);
    }

    dailyDiscountBarWidth(value: number): number {
        return this.toBarWidth(value, this.analyticsDiscountPeak, 5);
    }

    statusBarWidth(value: number): number {
        return this.toBarWidth(value, this.analyticsStatusPeak, 8);
    }

    couponOrdersBarWidth(value: number): number {
        return this.toBarWidth(value, this.analyticsCouponOrdersPeak, 8);
    }

    topProductQtyBarWidth(value: number): number {
        return this.toBarWidth(value, this.analyticsTopProductQtyPeak, 8);
    }

    toPercentLabel(value: number): string {
        return `${Math.round(value)}%`;
    }

    trackById(_: number, item: { id: number | string }): number | string {
        return item.id;
    }

    trackByOfferProductId(_: number, item: OfferAdminDto): number {
        return item.productId;
    }

    private applyImportedPreview(preview: ProductImportPreviewDto): void {
        const resolvedNameEn = preview.nameEn?.trim() || preview.nameAr?.trim() || this.productForm.nameEn;
        const resolvedNameAr = preview.nameAr?.trim() || preview.nameEn?.trim() || this.productForm.nameAr;
        const currentStockQuantity = this.productForm.stockQuantity;

        this.productForm = {
            ...this.productForm,
            nameAr: resolvedNameAr,
            nameEn: resolvedNameEn,
            descriptionAr: preview.descriptionAr ?? undefined,
            descriptionEn: preview.descriptionEn ?? undefined,
            price: preview.price ?? this.productForm.price,
            originalPrice: preview.originalPrice ?? undefined,
            stockQuantity: currentStockQuantity,
            categoryId: preview.suggestedCategoryId ?? this.productForm.categoryId,
            weight: preview.weight ?? undefined,
            dimensions: preview.dimensions ?? undefined,
            material: preview.material ?? undefined,
            origin: preview.origin ?? undefined,
            isActive: true,
            tagIds: this.productForm.tagIds ?? []
        };

        if (!this.isEditingProduct && preview.suggestedSku?.trim()) {
            this.productSku = preview.suggestedSku.trim();
        }

        this.productImportUrl = preview.sourceUrl;
    }

    private setProductFormFromDetail(detail: ProductAdminDetailDto): void {
        this.productForm = {
            nameAr: detail.nameAr,
            nameEn: detail.nameEn,
            descriptionAr: detail.descriptionAr ?? undefined,
            descriptionEn: detail.descriptionEn ?? undefined,
            price: detail.price,
            originalPrice: detail.originalPrice ?? undefined,
            stockQuantity: detail.stockQuantity,
            categoryId: detail.category?.id ?? 0,
            weight: detail.weight ?? undefined,
            dimensions: detail.dimensions ?? undefined,
            material: detail.material ?? undefined,
            origin: detail.origin ?? undefined,
            isFeatured: detail.isFeatured,
            isActive: true,
            tagIds: []
        };
    }

    private normalizeProductPayload(source: UpdateProductRequest): UpdateProductRequest {
        return {
            nameAr: source.nameAr.trim(),
            nameEn: source.nameEn.trim(),
            descriptionAr: source.descriptionAr?.trim() || undefined,
            descriptionEn: source.descriptionEn?.trim() || undefined,
            price: Number(source.price),
            originalPrice: source.originalPrice ? Number(source.originalPrice) : undefined,
            stockQuantity: Number(source.stockQuantity),
            categoryId: Number(source.categoryId),
            weight: source.weight ? Number(source.weight) : undefined,
            dimensions: source.dimensions?.trim() || undefined,
            material: source.material?.trim() || undefined,
            origin: source.origin?.trim() || undefined,
            isFeatured: source.isFeatured,
            isActive: source.isActive,
            tagIds: source.tagIds ?? []
        };
    }

    private toOrderStatus(value: string): UiOrderStatus {
        return ORDER_STATUSES.includes(value as UiOrderStatus) ? value as UiOrderStatus : 'Pending';
    }

    private toSupportStatus(value: string): UiSupportStatus {
        return SUPPORT_STATUSES.includes(value as UiSupportStatus) ? value as UiSupportStatus : 'New';
    }

    private defaultCreateUserForm(): CreateAdminUserRequest {
        return {
            firstName: '',
            lastName: '',
            email: '',
            password: '',
            phoneNumber: '',
            isActive: true,
            roles: ['Customer']
        };
    }

    private defaultCategoryForm(): UpdateCategoryRequest {
        return {
            nameAr: '',
            nameEn: '',
            descriptionAr: '',
            descriptionEn: '',
            parentId: undefined,
            imageUrl: '',
            displayOrder: 0,
            isActive: true
        };
    }

    private defaultProductForm(): UpdateProductRequest {
        return {
            nameAr: '',
            nameEn: '',
            descriptionAr: '',
            descriptionEn: '',
            price: 0,
            originalPrice: undefined,
            stockQuantity: 0,
            categoryId: 0,
            weight: undefined,
            dimensions: '',
            material: '',
            origin: '',
            isFeatured: false,
            isActive: true,
            tagIds: []
        };
    }

    private defaultCouponForm(): UpdateCouponRequest {
        const now = new Date();
        const end = new Date(now);
        end.setDate(end.getDate() + 30);

        return {
            code: '',
            descriptionAr: '',
            descriptionEn: '',
            discountType: 'Percentage',
            discountValue: 10,
            minOrderAmount: 0,
            maxDiscountAmount: undefined,
            usageLimit: 0,
            startsAt: this.toDateTimeLocal(now.toISOString()),
            expiresAt: this.toDateTimeLocal(end.toISOString()),
            isActive: true
        };
    }

    private defaultOfferForm(): ApplyOfferDiscountRequest {
        return {
            discountPercentage: 10,
            discountedPrice: undefined
        };
    }

    private normalizeCouponPayload(source: UpdateCouponRequest): UpdateCouponRequest {
        return {
            code: source.code.trim(),
            descriptionAr: source.descriptionAr?.trim() || undefined,
            descriptionEn: source.descriptionEn?.trim() || undefined,
            discountType: source.discountType,
            discountValue: Number(source.discountValue),
            minOrderAmount: Number(source.minOrderAmount),
            maxDiscountAmount: source.maxDiscountAmount ? Number(source.maxDiscountAmount) : undefined,
            usageLimit: Number(source.usageLimit),
            startsAt: new Date(source.startsAt).toISOString(),
            expiresAt: new Date(source.expiresAt).toISOString(),
            isActive: source.isActive
        };
    }

    private toDateTimeLocal(value: string): string {
        const date = new Date(value);
        const tzOffsetMs = date.getTimezoneOffset() * 60000;
        return new Date(date.getTime() - tzOffsetMs).toISOString().slice(0, 16);
    }

    private recalculateAnalyticsScales(): void {
        const trend = this.analytics?.dailyTrend ?? [];
        const statusBreakdown = this.analytics?.orderStatusBreakdown ?? [];
        const couponPerformance = this.analytics?.couponPerformance ?? [];
        const topProducts = this.analytics?.topProducts ?? [];

        this.analyticsRevenuePeak = trend.length > 0
            ? Math.max(...trend.map(item => item.revenue))
            : 0;

        this.analyticsOrdersPeak = trend.length > 0
            ? Math.max(...trend.map(item => item.ordersCount))
            : 0;

        this.analyticsDiscountPeak = trend.length > 0
            ? Math.max(...trend.map(item => item.discountAmount))
            : 0;

        this.analyticsStatusPeak = statusBreakdown.length > 0
            ? Math.max(...statusBreakdown.map(item => item.count))
            : 0;

        this.analyticsCouponOrdersPeak = couponPerformance.length > 0
            ? Math.max(...couponPerformance.map(item => item.ordersCount))
            : 0;

        this.analyticsTopProductQtyPeak = topProducts.length > 0
            ? Math.max(...topProducts.map(item => item.quantitySold))
            : 0;

        const ordersInPeriod = this.analytics?.ordersInPeriod ?? 0;
        const couponOrdersInPeriod = this.analytics?.couponOrdersInPeriod ?? 0;
        this.analyticsCouponOrderRate = ordersInPeriod > 0
            ? (couponOrdersInPeriod / ordersInPeriod) * 100
            : 0;

        const revenueInPeriod = this.analytics?.revenueInPeriod ?? 0;
        const discountsInPeriod = this.analytics?.discountsInPeriod ?? 0;
        this.analyticsDiscountPressureRate = revenueInPeriod > 0
            ? (discountsInPeriod / revenueInPeriod) * 100
            : 0;
    }

    private toBarWidth(value: number, max: number, minVisible = 0): number {
        if (max <= 0 || value <= 0) {
            return 0;
        }

        const width = (value / max) * 100;
        return Math.min(100, Math.max(minVisible, width));
    }

    private clearFlash(): void {
        this.errorMessage = '';
        this.successMessage = '';
    }

    private showError(message: string): void {
        this.errorMessage = message;
        this.successMessage = '';
    }

    private showSuccess(message: string): void {
        this.successMessage = message;
        this.errorMessage = '';
    }

    private getErrorMessage(error: unknown, fallback: string): string {
        const response = error as {
            error?: { message?: string; messageEn?: string };
            message?: string;
        };

        if (this.isArabic) {
            return response?.error?.message || response?.error?.messageEn || response?.message || fallback;
        }

        return response?.error?.messageEn || response?.error?.message || response?.message || fallback;
    }
}
