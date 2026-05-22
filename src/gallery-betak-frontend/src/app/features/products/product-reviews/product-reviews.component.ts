import { Component, Input, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReviewDto, ReviewService } from '../../../core/services/api/review.service';
import { AuthService } from '../../../core/services/api/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-product-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-reviews.component.html',
  styleUrl: './product-reviews.component.css'
})
export class ProductReviewsComponent implements OnInit {
  @Input() productId!: number;
  
  private destroyRef = inject(DestroyRef);
  
  reviews: ReviewDto[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  
  isLoggedIn = false;
  
  // Form
  rating = 0;
  comment = '';
  
  uiMessages: any;

  private reviewService = inject(ReviewService);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private uiTextService = inject(UiTextService);

  constructor() {
    this.uiMessages = this.uiTextService.getCurrentMessages();
  }

  ngOnInit() {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((messages: any) => {
        this.uiMessages = messages;
      });

    this.authService.currentUser$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((user: any) => {
      this.isLoggedIn = !!user;
    });

    this.loadReviews();
  }

  loadReviews() {
    this.isLoading = true;
    this.reviewService.getProductReviews(this.productId).subscribe({
      next: (reviews: ReviewDto[]) => {
        this.reviews = reviews;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load reviews';
        this.isLoading = false;
      }
    });
  }

  setRating(value: number) {
    this.rating = value;
  }

  submitReview() {
    if (this.rating === 0) {
      this.toastService.error('Please select a rating');
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.reviewService.createReview({
      productId: this.productId,
      rating: this.rating,
      comment: this.comment
    }).subscribe({
      next: (review: ReviewDto) => {
        this.reviews.unshift(review);
        this.rating = 0;
        this.comment = '';
        this.isSubmitting = false;
        this.toastService.success('Review submitted successfully!');
      },
      error: (err: any) => {
        // Backend handles "Already reviewed" and "Not purchased" validation
        this.errorMessage = err.error?.message || err.message || 'Failed to submit review';
        this.isSubmitting = false;
      }
    });
  }
}
