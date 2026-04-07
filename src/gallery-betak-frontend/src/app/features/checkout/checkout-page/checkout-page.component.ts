import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { CartActions } from '../../../core/store/cart/cart.actions';
import { selectCart } from '../../../core/store/cart/cart.reducer';
import { CartDto } from '../../../core/services/api/cart.service';
import { AuthService } from '../../../core/services/api/auth.service';
import { CreateOrderRequest, OrderService } from '../../../core/services/api/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthRedirectService } from '../../../core/services/auth-redirect.service';
import { UiTextService } from '../../../core/services/ui-text.service';
import { UpsertAddressRequest, UserAddress } from '../../../core/services/api/auth.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-checkout-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './checkout-page.component.html',
  styleUrl: './checkout-page.component.css'
})
export class CheckoutPageComponent implements OnInit {
  private store = inject(Store);
  private authService = inject(AuthService);
  private authRedirectService = inject(AuthRedirectService);
  private orderService = inject(OrderService);
  private router = inject(Router);
  private toastService = inject(ToastService);
  private uiTextService = inject(UiTextService);
  private destroyRef = inject(DestroyRef);

  currentStep = 1;

  cart: CartDto | null = null;
  uiMessages = this.uiTextService.getCurrentMessages();

  get governorates(): readonly string[] {
    return this.uiMessages.checkout.governorates as readonly string[];
  }

  readonly baseDeliveryCost = 30;
  readonly codExtraFee = 10;
  isSubmitting = false;
  isLoadingProfileData = false;
  savedAddresses: UserAddress[] = [];
  selectedAddress: UserAddress | null = null;
  showAddressDrawer = false;

  checkoutData = {
    addressId: null as number | null,
    firstName: '',
    lastName: '',
    phone: '',
    email: '',
    governorate: '',
    city: '',
    street: '',
    paymentMethod: 'card'
  };

  get paymentMethodDisplayLabel(): string {
    switch (this.checkoutData.paymentMethod) {
      case 'card':
        return this.uiMessages.checkout.paymentMethodCardLabel;
      case 'wallet':
        return this.uiMessages.checkout.paymentMethodWalletLabel;
      case 'fawry':
        return this.uiMessages.checkout.paymentMethodFawryLabel;
      case 'cod':
        return this.uiMessages.checkout.paymentMethodCodLabel;
      default:
        return this.uiMessages.checkout.paymentMethodUnknownLabel;
    }
  }

  ngOnInit(): void {
    this.uiTextService.messages$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(messages => {
        this.uiMessages = messages;
      });

    this.store.dispatch(CartActions.initializeCart());

    this.store.select(selectCart)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(cart => {
        this.cart = cart;
      });

    const currentUser = this.authService.currentUserValue;
    if (currentUser) {
      this.checkoutData.firstName = currentUser.firstName;
      this.checkoutData.lastName = currentUser.lastName;
      this.checkoutData.email = currentUser.email;
      this.checkoutData.phone = currentUser.phoneNumber ?? '';

      this.loadCheckoutPrefillData();
    }
  }

  get cartItems() {
    return this.cart?.items ?? [];
  }

  get subtotal() {
    return this.cart?.subTotal ?? 0;
  }

  get shippingCharge() {
    return this.checkoutData.paymentMethod === 'cod'
      ? this.baseDeliveryCost + this.codExtraFee
      : this.baseDeliveryCost;
  }

  get total() {
    return this.subtotal + this.shippingCharge;
  }

  nextStep() {
    if (this.currentStep === 1 && !this.validateShippingStep()) {
      return;
    }

    if (this.currentStep < 3) this.currentStep++;
  }

  prevStep() {
    if (this.currentStep > 1) this.currentStep--;
  }

  submitOrder() {
    if (!this.authRedirectService.ensureAuthenticated()) {
      return;
    }

    if (!this.cart || this.cart.items.length === 0) {
      this.toastService.error(this.uiMessages.checkout.emptyCart);
      return;
    }

    if (!this.checkoutData.addressId || this.checkoutData.addressId < 1) {
      this.toastService.error(this.uiMessages.checkout.invalidAddressId);
      return;
    }

    if (this.selectedAddress && this.selectedAddress.id === this.checkoutData.addressId && this.hasAddressChanges()) {
      this.syncSelectedAddressWithFormAndSubmit();
      return;
    }

    this.isSubmitting = true;

    this.createOrder();
  }

  selectAddress(address: UserAddress) {
    this.checkoutData.addressId = address.id;
    this.selectedAddress = address;
    this.applyAddressToCheckout(address);
  }

  get hasRequiredShippingFields(): boolean {
    return Boolean(
      this.checkoutData.addressId
      && this.checkoutData.firstName.trim()
      && this.checkoutData.lastName.trim()
      && this.checkoutData.phone.trim()
      && this.checkoutData.governorate.trim()
      && this.checkoutData.city.trim()
      && this.checkoutData.street.trim()
    );
  }

  get selectedAddressSummary(): string {
    if (!this.selectedAddress) {
      return '';
    }

    return `${this.selectedAddress.streetAddress}، ${this.selectedAddress.city}، ${this.selectedAddress.governorate}`;
  }

  formatAddressSummary(address: UserAddress): string {
    return `${address.streetAddress}، ${address.city}، ${address.governorate}`;
  }

  toggleAddressDrawer() {
    this.showAddressDrawer = !this.showAddressDrawer;
  }

  commitAddressChange(address: UserAddress) {
    this.selectAddress(address);
    this.showAddressDrawer = false;
  }

  private loadCheckoutPrefillData() {
    this.isLoadingProfileData = true;

    this.authService.getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: profile => {
          this.checkoutData.firstName = profile.firstName;
          this.checkoutData.lastName = profile.lastName;
          this.checkoutData.email = profile.email;
          this.checkoutData.phone = profile.phoneNumber ?? this.checkoutData.phone;
        },
        error: () => {
          this.isLoadingProfileData = false;
        }
      });

    this.authService.getAddresses()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: addresses => {
          this.savedAddresses = addresses;

          const preferredAddress =
            addresses.find(address => address.isDefault) ??
            addresses[0] ??
            null;

          if (preferredAddress) {
            this.checkoutData.addressId = preferredAddress.id;
            this.selectedAddress = preferredAddress;
            this.applyAddressToCheckout(preferredAddress);
          }

          this.isLoadingProfileData = false;
        },
        error: () => {
          this.savedAddresses = [];
          this.isLoadingProfileData = false;
        }
      });
  }

  private applyAddressToCheckout(address: UserAddress) {
    this.checkoutData.phone = address.phone || this.checkoutData.phone;
    this.checkoutData.governorate = address.governorate;
    this.checkoutData.city = address.city;
    this.checkoutData.street = address.streetAddress;

    const [firstName, ...lastNameParts] = address.recipientName.trim().split(' ').filter(Boolean);
    if (firstName) {
      this.checkoutData.firstName = firstName;
    }

    if (lastNameParts.length > 0) {
      this.checkoutData.lastName = lastNameParts.join(' ');
    }
  }

  private hasAddressChanges(): boolean {
    if (!this.selectedAddress) {
      return false;
    }

    const normalizedRecipientName = `${this.checkoutData.firstName} ${this.checkoutData.lastName}`.trim();

    return this.selectedAddress.recipientName.trim() !== normalizedRecipientName
      || this.selectedAddress.phone.trim() !== this.checkoutData.phone.trim()
      || this.selectedAddress.governorate.trim() !== this.checkoutData.governorate.trim()
      || this.selectedAddress.city.trim() !== this.checkoutData.city.trim()
      || this.selectedAddress.streetAddress.trim() !== this.checkoutData.street.trim();
  }

  private syncSelectedAddressWithFormAndSubmit() {
    if (!this.selectedAddress || !this.checkoutData.addressId) {
      return;
    }

    this.isSubmitting = true;

    const request: UpsertAddressRequest = {
      label: this.selectedAddress.label,
      recipientName: `${this.checkoutData.firstName} ${this.checkoutData.lastName}`.trim(),
      phone: this.checkoutData.phone,
      governorate: this.checkoutData.governorate,
      city: this.checkoutData.city,
      district: this.selectedAddress.district,
      streetAddress: this.checkoutData.street,
      buildingNo: this.selectedAddress.buildingNo,
      apartmentNo: this.selectedAddress.apartmentNo,
      postalCode: this.selectedAddress.postalCode,
      isDefault: this.selectedAddress.isDefault
    };

    this.authService.updateAddress(this.checkoutData.addressId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: updatedAddress => {
          this.selectedAddress = updatedAddress;
          this.savedAddresses = this.savedAddresses.map(address =>
            address.id === updatedAddress.id ? updatedAddress : address
          );

          this.createOrder();
        },
        error: () => {
          this.isSubmitting = false;
          this.toastService.error(this.uiMessages.checkout.addressSyncFailed);
        }
      });
  }

  private createOrder() {
    if (!this.checkoutData.addressId) {
      this.isSubmitting = false;
      return;
    }

    const request: CreateOrderRequest = {
      addressId: this.checkoutData.addressId,
      paymentMethod: this.mapPaymentMethod(this.checkoutData.paymentMethod),
      notes: `${this.checkoutData.street} - ${this.checkoutData.city} - ${this.checkoutData.governorate}`
    };

    this.orderService.createOrder(request).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toastService.success(this.uiMessages.checkout.orderCreated);
        this.store.dispatch(CartActions.initializeCart());
        this.router.navigate(['/'], { replaceUrl: true });
      },
      error: () => {
        this.isSubmitting = false;
        this.toastService.error(this.uiMessages.checkout.orderFailed);
      }
    });
  }

  private validateShippingStep(): boolean {
    if (!this.hasRequiredShippingFields) {
      this.toastService.error(this.uiMessages.checkout.shippingDetailsMissing);
      return false;
    }

    if (this.checkoutData.email && !this.isValidEmail(this.checkoutData.email)) {
      this.toastService.error(this.uiMessages.checkout.invalidEmail);
      return false;
    }

    return true;
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  private mapPaymentMethod(method: string): number {
    switch (method) {
      case 'wallet':
        return 0;
      case 'fawry':
        return 3;
      case 'cod':
        return 4;
      case 'card':
      default:
        return 5;
    }
  }
}
