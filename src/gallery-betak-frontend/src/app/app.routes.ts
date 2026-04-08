import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent)
  },
  {
    path: 'categories/:id',
    loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent)
  },
  {
    path: 'products/:slug',
    loadComponent: () => import('./features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
  },
  {
    path: 'p/:id',
    loadComponent: () => import('./features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
  },
  {
    path: 'categories',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart-page/cart-page.component').then(m => m.CartPageComponent)
  },
  {
    path: 'wishlist',
    loadComponent: () => import('./features/wishlist/wishlist-page/wishlist-page.component').then(m => m.WishlistPageComponent)
  },
  {
    path: 'offers',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout-page/checkout-page.component').then(m => m.CheckoutPageComponent)
  },
  {
    path: 'account',
    loadComponent: () => import('./features/account/account-page/account-page.component').then(m => m.AccountPageComponent)
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
