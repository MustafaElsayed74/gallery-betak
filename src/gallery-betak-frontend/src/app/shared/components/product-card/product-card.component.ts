import { Component, DestroyRef, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProductDto } from '../../../core/services/api/product.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { LanguageService } from '../../../core/services/language.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css'
})
export class ProductCardComponent {
  private uiTextService = inject(UiTextService);
  private languageService = inject(LanguageService);
  private destroyRef = inject(DestroyRef);

  uiMessages = this.uiTextService.getCurrentMessages();
  currentLanguage = this.languageService.currentLanguage;

  @Input() product!: ProductDto;
  @Output() addToCart = new EventEmitter<number>();
  @Output() addToWishlist = new EventEmitter<number>();

  constructor() {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.languageService.language$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(language => {
        this.currentLanguage = language;
      });
  }

  get productDisplayName(): string {
    return this.currentLanguage === 'en'
      ? (this.product.nameEn || this.product.nameAr)
      : (this.product.nameAr || this.product.nameEn);
  }

  get categoryDisplayName(): string {
    return this.currentLanguage === 'en'
      ? (this.product.categoryNameEn || this.product.categoryNameAr || this.product.categoryName || '')
      : (this.product.categoryNameAr || this.product.categoryNameEn || this.product.categoryName || '');
  }

  onAddToCart(event: Event) {
    event.stopPropagation();
    event.preventDefault();
    this.addToCart.emit(this.product.id);
  }

  onAddToWishlist(event: Event) {
    event.stopPropagation();
    event.preventDefault();
    this.addToWishlist.emit(this.product.id);
  }
}
