import { Component, DestroyRef, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductCardComponent } from '../../../shared/components/product-card/product-card.component';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { CartActions } from '../../../core/store/cart/cart.actions';
import { WishlistService } from '../../../core/services/api/wishlist.service';
import { ProductDto, ProductService, ProductSpecParams } from '../../../core/services/api/product.service';
import { CategoryService } from '../../../core/services/api/category.service';
import { combineLatest } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { AuthRedirectService } from '../../../core/services/auth-redirect.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

interface CategoryFilterItem {
  id: number;
  name: string;
}

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ProductCardComponent, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {
  private static readonly FILTER_STATE_KEY = 'products_filters_open';

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private store = inject(Store);
  private wishlistService = inject(WishlistService);
  private authRedirectService = inject(AuthRedirectService);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private toastService = inject(ToastService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  products: ProductDto[] = [];
  categories: CategoryFilterItem[] = [];
  uiMessages = this.uiTextService.getCurrentMessages();

  isLoading = false;
  isCategoriesLoading = false;
  errorMessage = '';
  categoriesErrorMessage = '';
  totalCount = 0;
  pageIndex = 1;
  pageSize = 12;
  sortBy = 'default';
  searchTerm = '';
  minPrice: number | null = null;
  maxPrice: number | null = null;
  filterErrorMessage = '';

  isFilterOpen = false;
  selectedCategoryId: number | null = null;
  private searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  get displayedProducts() {
    return this.products;
  }

  get hasActiveFilters(): boolean {
    return this.selectedCategoryId !== null ||
      this.sortBy !== 'default' ||
      this.searchTerm.trim().length > 0 ||
      this.minPrice !== null ||
      this.maxPrice !== null ||
      this.pageIndex > 1;
  }

  ngOnInit(): void {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.restoreFilterPanelState();
    this.loadCategories();

    combineLatest([this.route.paramMap, this.route.queryParamMap]).subscribe(([params, query]) => {
      const routeCategoryId = params.get('id');
      this.selectedCategoryId = routeCategoryId ? Number(routeCategoryId) : null;

      const querySort = query.get('sort');
      this.sortBy = querySort ?? 'default';

      this.searchTerm = query.get('search') ?? '';

      const queryPage = Number(query.get('page'));
      this.pageIndex = Number.isFinite(queryPage) && queryPage > 0 ? queryPage : 1;

      const queryMinPrice = Number(query.get('minPrice'));
      this.minPrice = Number.isFinite(queryMinPrice) && queryMinPrice >= 0 ? queryMinPrice : null;

      const queryMaxPrice = Number(query.get('maxPrice'));
      this.maxPrice = Number.isFinite(queryMaxPrice) && queryMaxPrice >= 0 ? queryMaxPrice : null;

      this.loadProducts();
    });
  }

  ngOnDestroy(): void {
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }

    document.body.style.overflow = '';
  }

  private loadCategories() {
    this.isCategoriesLoading = true;
    this.categoriesErrorMessage = '';

    this.categoryService.getHierarchicalCategories().subscribe({
      next: categories => {
        this.categories = categories.map(category => ({
          id: category.id,
          name: category.nameAr || category.nameEn
        }));
        this.isCategoriesLoading = false;
      },
      error: () => {
        this.categoriesErrorMessage = this.uiMessages.products.loadCategoriesFailed;
        this.isCategoriesLoading = false;
      }
    });
  }

  private loadProducts() {
    this.isLoading = true;
    this.errorMessage = '';

    const params: ProductSpecParams = {
      pageIndex: this.pageIndex,
      pageSize: this.pageSize,
      sort: this.mapSort(this.sortBy)
    };

    if (this.selectedCategoryId) {
      params.categoryId = this.selectedCategoryId;
    }

    if (this.searchTerm.trim().length > 0) {
      params.search = this.searchTerm.trim();
    }

    if (this.minPrice !== null) {
      params.minPrice = this.minPrice;
    }

    if (this.maxPrice !== null) {
      params.maxPrice = this.maxPrice;
    }

    this.productService.getProducts(params).subscribe({
      next: result => {
        this.products = result.data;
        this.totalCount = result.count;
        this.pageIndex = result.pageIndex;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = this.uiMessages.products.loadProductsFailed;
        this.isLoading = false;
      }
    });
  }

  toggleFilters() {
    this.setFilterOpenState(!this.isFilterOpen);
  }

  @HostListener('window:resize')
  onWindowResize() {
    this.syncMobileBodyScrollLock();
  }

  @HostListener('document:keydown.escape')
  onEscapeKey() {
    if (this.isFilterOpen) {
      this.setFilterOpenState(false);
    }
  }

  selectCategory(categoryId: number | null) {
    this.closeFiltersPanel();

    if (categoryId === null) {
      this.router.navigate(['/products'], {
        queryParams: {
          search: this.searchTerm || null,
          sort: this.sortBy,
          page: 1,
          minPrice: this.minPrice,
          maxPrice: this.maxPrice
        }
      });
      return;
    }

    this.router.navigate(['/categories', categoryId], {
      queryParams: {
        search: this.searchTerm || null,
        sort: this.sortBy,
        page: 1,
        minPrice: this.minPrice,
        maxPrice: this.maxPrice
      }
    });
  }

  onSortChange(value: string) {
    this.updateQueryParams({ sort: value, page: 1 });
  }

  onSearchTermChanged(value: string) {
    this.searchTerm = value;

    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }

    this.searchDebounceTimer = setTimeout(() => {
      this.applySearch();
    }, 350);
  }

  applySearch() {
    const normalizedSearch = this.searchTerm.trim();
    const currentSearch = (this.route.snapshot.queryParamMap.get('search') ?? '').trim();

    if (normalizedSearch === currentSearch) {
      return;
    }

    this.updateQueryParams({
      search: normalizedSearch || null,
      page: 1
    });
  }

  clearSearch() {
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }

    this.searchTerm = '';
    this.updateQueryParams({ search: null, page: 1 });
  }

  clearAllFilters() {
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }

    this.searchTerm = '';
    this.sortBy = 'default';
    this.minPrice = null;
    this.maxPrice = null;
    this.filterErrorMessage = '';

    this.router.navigate(['/products']);
    this.closeFiltersPanel();
  }

  applyPriceFilter() {
    this.filterErrorMessage = '';

    if (this.minPrice !== null && this.maxPrice !== null && this.minPrice > this.maxPrice) {
      const temp = this.minPrice;
      this.minPrice = this.maxPrice;
      this.maxPrice = temp;
    }

    this.updateQueryParams({
      minPrice: this.minPrice,
      maxPrice: this.maxPrice,
      page: 1
    });

    this.closeFiltersPanel();
  }

  clearPriceFilter() {
    this.filterErrorMessage = '';
    this.minPrice = null;
    this.maxPrice = null;
    this.updateQueryParams({ minPrice: null, maxPrice: null, page: 1 });

    this.closeFiltersPanel();
  }

  goToPreviousPage() {
    if (this.pageIndex <= 1 || this.isLoading) {
      return;
    }

    this.updateQueryParams({ page: this.pageIndex - 1 });
  }

  goToNextPage() {
    if (!this.canGoNextPage || this.isLoading) {
      return;
    }

    this.updateQueryParams({ page: this.pageIndex + 1 });
  }

  get canGoNextPage(): boolean {
    return this.pageIndex * this.pageSize < this.totalCount;
  }

  private mapSort(sort: string): string {
    switch (sort) {
      case 'price_asc':
        return 'priceAsc';
      case 'price_desc':
        return 'priceDesc';
      case 'newest':
        return 'newest';
      default:
        return 'nameArAsc';
    }
  }

  private updateQueryParams(params: {
    search?: string | null;
    sort?: string;
    page?: number;
    minPrice?: number | null;
    maxPrice?: number | null;
  }) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      queryParamsHandling: 'merge'
    });
  }

  private restoreFilterPanelState() {
    const storedState = localStorage.getItem(ProductListComponent.FILTER_STATE_KEY);
    this.isFilterOpen = storedState === '1';
  }

  private setFilterOpenState(isOpen: boolean) {
    this.isFilterOpen = isOpen;
    localStorage.setItem(ProductListComponent.FILTER_STATE_KEY, isOpen ? '1' : '0');
    this.syncMobileBodyScrollLock();
  }

  private closeFiltersPanel() {
    this.setFilterOpenState(false);
  }

  private syncMobileBodyScrollLock() {
    const isMobileViewport = window.matchMedia('(max-width: 767px)').matches;
    document.body.style.overflow = isMobileViewport && this.isFilterOpen ? 'hidden' : '';
  }

  handleAddToCart(productId: number) {
    this.store.dispatch(CartActions.addItem({ productId, quantity: 1 }));
  }

  handleAddToWishlist(productId: number) {
    if (!this.authRedirectService.ensureAuthenticated(this.router.url)) {
      return;
    }

    this.wishlistService.toggleWishlistItem(productId).subscribe({
      next: () => {
        this.toastService.success(this.uiMessages.wishlist.updatedSuccess);
      },
      error: () => {
        this.toastService.error(this.uiMessages.wishlist.updateFailed);
      }
    });
  }
}
