import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductDto, ProductService } from '../../../core/services/api/product.service';
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
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  product: ProductDto | null = null;
  uiMessages = this.uiTextService.getCurrentMessages();
  isLoading = false;
  errorMessage = '';
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
      const idParam = params.get('id');
      const slug = params.get('slug');

      if (idParam) {
        const parsedId = Number(idParam);

        if (Number.isFinite(parsedId) && parsedId > 0) {
          this.loadProductById(parsedId);
          return;
        }

        this.errorMessage = this.uiMessages.products.invalidProductData;
        return;
      }

      if (slug) {
        this.loadProductBySlug(slug);
        return;
      }

      if (!slug) {
        this.errorMessage = this.uiMessages.products.invalidProductData;
        return;
      }
    });
  }

  private resetLoadingState() {
    this.isLoading = true;
    this.errorMessage = '';
    this.activeImage = 0;
  }

  private loadProductBySlug(slug: string) {
    this.resetLoadingState();

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

  private loadProductById(id: number) {
    this.resetLoadingState();

    this.productService.getProduct(id).subscribe({
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

  setActiveImage(index: number) {
    this.activeImage = index;
  }
}
