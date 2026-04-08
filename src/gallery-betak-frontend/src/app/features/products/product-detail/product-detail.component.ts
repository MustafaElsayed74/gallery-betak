import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { CartActions } from '../../../core/store/cart/cart.actions';
import { WishlistService } from '../../../core/services/api/wishlist.service';
import { ProductDto, ProductService } from '../../../core/services/api/product.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthRedirectService } from '../../../core/services/auth-redirect.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnInit {
  private store = inject(Store);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private wishlistService = inject(WishlistService);
  private authRedirectService = inject(AuthRedirectService);
  private productService = inject(ProductService);
  private toastService = inject(ToastService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  product: ProductDto | null = null;
  uiMessages = this.uiTextService.getCurrentMessages();
  isLoading = false;
  errorMessage = '';

  quantity = 1;
  activeImage = 0;
  activeTab = 'description';

  get productImages(): string[] {
    if (!this.product) {
      return ['assets/placeholder.svg'];
    }

    const images = this.product.images?.filter(image => !!image) ?? [];

    if (images.length > 0) {
      return images;
    }

    if (this.product.primaryImage) {
      return [this.product.primaryImage];
    }

    return ['assets/placeholder.svg'];
  }

  get productHighlights(): string[] {
    const description = this.normalizedDescription;

    if (!description) {
      return [];
    }

    return description
      .split(/[\n\r]+|[\.،؛!?]+/)
      .map(item => item.trim())
      .filter(item => item.length >= 8)
      .slice(0, 6);
  }

  get normalizedDescription(): string {
    if (!this.product) {
      return '';
    }

    return (this.product.descriptionAr || this.product.descriptionEn || '').replace(/\s+/g, ' ').trim();
  }

  get technicalSpecs(): Array<{ label: string; value: string }> {
    if (!this.product) {
      return [];
    }

    const specs: Array<{ label: string; value: string }> = [];
    const material = this.product.material?.trim();
    const dimensions = this.product.dimensions?.trim();
    const origin = this.product.origin?.trim();

    if (material) {
      specs.push({
        label: this.uiMessages.products.specMaterialLabel,
        value: material
      });
    }

    if (dimensions) {
      specs.push({
        label: this.uiMessages.products.specDimensionsLabel,
        value: dimensions
      });
    }

    if (this.product.weight !== null && this.product.weight !== undefined) {
      specs.push({
        label: this.uiMessages.products.specWeightLabel,
        value: `${this.product.weight} ${this.uiMessages.products.specWeightUnit}`
      });
    }

    if (origin) {
      specs.push({
        label: this.uiMessages.products.specOriginLabel,
        value: origin
      });
    }

    return specs;
  }

  ngOnInit(): void {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.route.paramMap.subscribe(params => {
      const slug = params.get('slug');
      if (!slug) {
        this.errorMessage = this.uiMessages.products.invalidProductData;
        return;
      }

      this.loadProduct(slug);
    });
  }

  private loadProduct(slug: string) {
    this.isLoading = true;
    this.errorMessage = '';
    this.quantity = 1;
    this.activeImage = 0;

    this.productService.getProductBySlug(slug).subscribe({
      next: product => {
        this.product = product;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = this.uiMessages.products.loadProductFailed;
        this.isLoading = false;
      }
    });
  }

  increaseQuantity() {
    if (!this.product) {
      return;
    }

    if (this.quantity < this.product.stockQuantity) {
      this.quantity++;
    }
  }

  decreaseQuantity() {
    if (this.quantity > 1) this.quantity--;
  }

  setActiveImage(index: number) {
    this.activeImage = index;
  }

  addToCart() {
    if (!this.product) {
      return;
    }

    this.store.dispatch(CartActions.addItem({ productId: this.product.id, quantity: this.quantity }));
  }

  buyNow() {
    if (!this.product || this.product.stockQuantity <= 0) {
      return;
    }

    this.store.dispatch(CartActions.addItem({ productId: this.product.id, quantity: this.quantity }));
    this.router.navigate(['/checkout']);
  }

  addToWishlist() {
    if (!this.product) {
      return;
    }

    if (!this.authRedirectService.ensureAuthenticated()) {
      return;
    }

    this.wishlistService.toggleWishlistItem(this.product.id).subscribe({
      next: () => {
        this.toastService.success(this.uiMessages.wishlist.updatedSuccess);
      },
      error: () => {
        this.toastService.error(this.uiMessages.wishlist.updateFailed);
      }
    });
  }

  async shareProduct(): Promise<void> {
    if (!this.product || typeof window === 'undefined') {
      return;
    }

    const shareUrl = this.buildShareUrl(this.product.slug);
    const shareTitle = this.product.nameAr || this.product.nameEn || this.uiMessages.products.listTitle;

    if (typeof navigator !== 'undefined' && typeof navigator.share === 'function') {
      try {
        await navigator.share({
          title: shareTitle,
          text: shareTitle,
          url: shareUrl
        });

        return;
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return;
        }
      }
    }

    await this.copyShareLink(shareUrl);
  }

  private buildShareUrl(slug: string): string {
    const encodedSlug = encodeURIComponent(slug);

    if (typeof window === 'undefined') {
      return `/products/${encodedSlug}`;
    }

    return `${window.location.origin}/products/${encodedSlug}`;
  }

  private async copyShareLink(url: string): Promise<void> {
    try {
      if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(url);
      } else {
        const copied = this.fallbackCopyToClipboard(url);

        if (!copied) {
          throw new Error('Clipboard API unavailable');
        }
      }

      this.toastService.success(this.uiMessages.products.shareLinkCopied);
    } catch {
      this.toastService.error(this.uiMessages.products.shareLinkCopyFailed);
    }
  }

  private fallbackCopyToClipboard(text: string): boolean {
    if (typeof document === 'undefined') {
      return false;
    }

    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.setAttribute('readonly', 'true');
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';

    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();
    textarea.setSelectionRange(0, textarea.value.length);

    const copied = document.execCommand('copy');
    document.body.removeChild(textarea);

    return copied;
  }
}
