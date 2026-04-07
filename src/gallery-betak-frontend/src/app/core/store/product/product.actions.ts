import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { PagedResult, ProductDto, ProductSpecParams } from '../../services/api/product.service';
import { CategoryDto } from '../../services/api/category.service';

export const ProductActions = createActionGroup({
  source: 'Product',
  events: {
    'Load Products': props<{ params: ProductSpecParams }>(),
    'Load Products Success': props<{ result: PagedResult<ProductDto> }>(),
    'Load Products Failure': props<{ error: string }>(),
    
    'Load Single Product': props<{ slug: string }>(),
    'Load Single Product Success': props<{ product: ProductDto }>(),
    'Load Single Product Failure': props<{ error: string }>(),

    'Load Categories': emptyProps(),
    'Load Categories Success': props<{ categories: CategoryDto[] }>(),
    'Load Categories Failure': props<{ error: string }>()
  }
});
