import { createFeature, createReducer, on } from '@ngrx/store';
import { ProductActions } from './product.actions';
import { ProductDto } from '../../services/api/product.service';
import { CategoryDto } from '../../services/api/category.service';

export interface ProductState {
  products: ProductDto[];
  totalCount: number;
  categories: CategoryDto[];
  selectedProduct: ProductDto | null;
  loading: boolean;
  error: string | null;
}

const initialState: ProductState = {
  products: [],
  totalCount: 0,
  categories: [],
  selectedProduct: null,
  loading: false,
  error: null
};

export const productFeature = createFeature({
  name: 'product',
  reducer: createReducer(
    initialState,
    
    on(ProductActions.loadProducts, (state) => ({ ...state, loading: true, error: null })),
    on(ProductActions.loadProductsSuccess, (state, { result }) => ({ 
      ...state, 
      products: result.data, 
      totalCount: result.count, 
      loading: false 
    })),
    on(ProductActions.loadProductsFailure, (state, { error }) => ({ ...state, loading: false, error })),

    on(ProductActions.loadSingleProduct, (state) => ({ ...state, loading: true, error: null })),
    on(ProductActions.loadSingleProductSuccess, (state, { product }) => ({ 
      ...state, 
      selectedProduct: product, 
      loading: false 
    })),
    on(ProductActions.loadSingleProductFailure, (state, { error }) => ({ ...state, loading: false, error })),

    on(ProductActions.loadCategories, (state) => ({ ...state, loading: true, error: null })),
    on(ProductActions.loadCategoriesSuccess, (state, { categories }) => ({ 
      ...state, 
      categories, 
      loading: false 
    })),
    on(ProductActions.loadCategoriesFailure, (state, { error }) => ({ ...state, loading: false, error }))
  )
});

export const {
  selectProductState,
  selectProducts,
  selectTotalCount,
  selectCategories,
  selectSelectedProduct,
  selectLoading,
  selectError
} = productFeature;
