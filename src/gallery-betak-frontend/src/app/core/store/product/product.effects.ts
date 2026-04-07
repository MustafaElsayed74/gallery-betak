import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { ProductActions } from './product.actions';
import { ProductService } from '../../services/api/product.service';
import { CategoryService } from '../../services/api/category.service';
import { catchError, map, mergeMap, of } from 'rxjs';

@Injectable()
export class ProductEffects {
  private actions$ = inject(Actions);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);

  loadProducts$ = createEffect(() => 
    this.actions$.pipe(
      ofType(ProductActions.loadProducts),
      mergeMap(({ params }) => 
        this.productService.getProducts(params).pipe(
          map(result => ProductActions.loadProductsSuccess({ result })),
          catchError(error => of(ProductActions.loadProductsFailure({ error: 'Failed to load products' })))
        )
      )
    )
  );

  loadSingleProduct$ = createEffect(() => 
    this.actions$.pipe(
      ofType(ProductActions.loadSingleProduct),
      mergeMap(({ slug }) => 
        this.productService.getProductBySlug(slug).pipe(
          map(product => ProductActions.loadSingleProductSuccess({ product })),
          catchError(error => of(ProductActions.loadSingleProductFailure({ error: 'Failed to load product' })))
        )
      )
    )
  );

  loadCategories$ = createEffect(() => 
    this.actions$.pipe(
      ofType(ProductActions.loadCategories),
      mergeMap(() => 
        this.categoryService.getCategories().pipe(
          map(categories => ProductActions.loadCategoriesSuccess({ categories })),
          catchError(error => of(ProductActions.loadCategoriesFailure({ error: 'Failed to load categories' })))
        )
      )
    )
  );
}
