import { Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectUser } from '../../../core/store/auth/auth.reducer';
import { AuthActions } from '../../../core/store/auth/auth.actions';
import { LanguageService } from '../../../core/services/language.service';
import {
    AuthService,
    ChangePasswordRequest,
    UpsertAddressRequest,
    UserAddress,
    UserProfile
} from '../../../core/services/api/auth.service';

@Component({
    selector: 'app-account-page',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule],
    templateUrl: './account-page.component.html',
    styleUrl: './account-page.component.css'
})
export class AccountPageComponent {
    private readonly TAB_KEYS = ['profile', 'password', 'addresses'] as const;

    private store = inject(Store);
    private authService = inject(AuthService);
    private languageService = inject(LanguageService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private destroyRef = inject(DestroyRef);

    user$ = this.store.select(selectUser);

    profile: UserProfile | null = null;
    addresses: UserAddress[] = [];

    loading = false;
    savingProfile = false;
    changingPassword = false;
    savingAddress = false;

    profileForm = {
        firstName: '',
        lastName: '',
        email: '',
        phoneNumber: ''
    };

    passwordForm: ChangePasswordRequest = {
        currentPassword: '',
        newPassword: '',
        confirmNewPassword: ''
    };

    showAddressModal = false;
    editingAddressId: number | null = null;
    addressForm: UpsertAddressRequest = this.createEmptyAddressForm();
    activeTab: 'profile' | 'password' | 'addresses' = 'profile';

    profileMessage: string | null = null;
    passwordMessage: string | null = null;
    addressMessage: string | null = null;
    errorMessage: string | null = null;

    ngOnInit() {
        this.route.queryParamMap
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(params => {
                const requestedTab = params.get('tab');
                if (this.isTabKey(requestedTab) && requestedTab !== this.activeTab) {
                    this.activeTab = requestedTab;
                }
            });

        this.loadSettings();
    }

    t(en: string, ar: string): string {
        return this.languageService.currentLanguage === 'ar' ? ar : en;
    }

    get isArabic(): boolean {
        return this.languageService.currentLanguage === 'ar';
    }

    setActiveTab(tab: 'profile' | 'password' | 'addresses') {
        if (this.activeTab === tab) {
            return;
        }

        this.activeTab = tab;

        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { tab },
            queryParamsHandling: 'merge',
            replaceUrl: true
        });
    }

    isAdmin(roles: string[] | null | undefined): boolean {
        return (roles ?? []).some(role => ['Admin', 'SuperAdmin'].includes(role));
    }

    loadSettings() {
        this.loading = true;
        this.errorMessage = null;

        this.authService.getProfile().subscribe({
            next: profile => {
                this.profile = profile;
                this.profileForm = {
                    firstName: profile.firstName,
                    lastName: profile.lastName,
                    email: profile.email,
                    phoneNumber: profile.phoneNumber ?? ''
                };

                this.authService.getAddresses().subscribe({
                    next: addresses => {
                        this.addresses = addresses;
                        this.loading = false;
                    },
                    error: error => {
                        this.errorMessage = this.getErrorMessage(error, this.t('Failed to load your addresses.', 'تعذر تحميل عناوينك.'));
                        this.loading = false;
                    }
                });
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to load profile settings.', 'تعذر تحميل إعدادات الملف الشخصي.'));
                this.loading = false;
            }
        });
    }

    saveProfile() {
        this.savingProfile = true;
        this.profileMessage = null;
        this.errorMessage = null;

        this.authService.updateProfile({
            firstName: this.profileForm.firstName.trim(),
            lastName: this.profileForm.lastName.trim(),
            email: this.profileForm.email.trim(),
            phoneNumber: this.profileForm.phoneNumber.trim() || null
        }).subscribe({
            next: profile => {
                this.profile = profile;
                this.profileMessage = this.t('Profile updated successfully.', 'تم تحديث الملف الشخصي بنجاح.');
                this.savingProfile = false;
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to update profile.', 'تعذر تحديث الملف الشخصي.'));
                this.savingProfile = false;
            }
        });
    }

    changePassword() {
        if (this.passwordForm.newPassword !== this.passwordForm.confirmNewPassword) {
            this.errorMessage = this.t('New password and confirmation do not match.', 'كلمة المرور الجديدة وتأكيدها غير متطابقين.');
            return;
        }

        this.changingPassword = true;
        this.passwordMessage = null;
        this.errorMessage = null;

        this.authService.changePassword(this.passwordForm).subscribe({
            next: () => {
                this.passwordMessage = this.t('Password changed successfully.', 'تم تغيير كلمة المرور بنجاح.');
                this.passwordForm = {
                    currentPassword: '',
                    newPassword: '',
                    confirmNewPassword: ''
                };
                this.changingPassword = false;
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to change password.', 'تعذر تغيير كلمة المرور.'));
                this.changingPassword = false;
            }
        });
    }

    openAddAddressModal() {
        this.editingAddressId = null;
        this.addressForm = this.createEmptyAddressForm();
        this.showAddressModal = true;
    }

    openEditAddressModal(address: UserAddress) {
        this.editingAddressId = address.id;
        this.addressForm = {
            label: address.label,
            recipientName: address.recipientName,
            phone: address.phone,
            governorate: address.governorate,
            city: address.city,
            district: address.district ?? '',
            streetAddress: address.streetAddress,
            buildingNo: address.buildingNo ?? '',
            apartmentNo: address.apartmentNo ?? '',
            postalCode: address.postalCode ?? '',
            isDefault: address.isDefault
        };
        this.showAddressModal = true;
    }

    closeAddressModal() {
        this.showAddressModal = false;
        this.editingAddressId = null;
        this.addressForm = this.createEmptyAddressForm();
    }

    saveAddress() {
        this.savingAddress = true;
        this.addressMessage = null;
        this.errorMessage = null;

        const payload: UpsertAddressRequest = {
            ...this.addressForm,
            label: this.addressForm.label.trim(),
            recipientName: this.addressForm.recipientName.trim(),
            phone: this.addressForm.phone.trim(),
            governorate: this.addressForm.governorate.trim(),
            city: this.addressForm.city.trim(),
            streetAddress: this.addressForm.streetAddress.trim(),
            district: this.addressForm.district?.trim() || null,
            buildingNo: this.addressForm.buildingNo?.trim() || null,
            apartmentNo: this.addressForm.apartmentNo?.trim() || null,
            postalCode: this.addressForm.postalCode?.trim() || null
        };

        const request$ = this.editingAddressId
            ? this.authService.updateAddress(this.editingAddressId, payload)
            : this.authService.createAddress(payload);

        request$.subscribe({
            next: () => {
                this.addressMessage = this.editingAddressId
                    ? this.t('Address updated successfully.', 'تم تحديث العنوان بنجاح.')
                    : this.t('Address added successfully.', 'تمت إضافة العنوان بنجاح.');
                this.savingAddress = false;
                this.closeAddressModal();
                this.reloadAddresses();
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to save address.', 'تعذر حفظ العنوان.'));
                this.savingAddress = false;
            }
        });
    }

    makeAddressPriority(address: UserAddress) {
        this.errorMessage = null;
        this.authService.setDefaultAddress(address.id).subscribe({
            next: () => {
                this.addressMessage = this.t('Priority address updated.', 'تم تحديث أولوية العنوان.');
                this.reloadAddresses();
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to update address priority.', 'تعذر تحديث أولوية العنوان.'));
            }
        });
    }

    deleteAddress(address: UserAddress) {
        this.errorMessage = null;
        this.authService.deleteAddress(address.id).subscribe({
            next: () => {
                this.addressMessage = this.t('Address deleted.', 'تم حذف العنوان.');
                this.reloadAddresses();
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to delete address.', 'تعذر حذف العنوان.'));
            }
        });
    }

    private reloadAddresses() {
        this.authService.getAddresses().subscribe({
            next: addresses => {
                this.addresses = addresses;
            },
            error: error => {
                this.errorMessage = this.getErrorMessage(error, this.t('Failed to load your addresses.', 'تعذر تحميل عناوينك.'));
            }
        });
    }

    private createEmptyAddressForm(): UpsertAddressRequest {
        return {
            label: '',
            recipientName: '',
            phone: '',
            governorate: '',
            city: '',
            district: '',
            streetAddress: '',
            buildingNo: '',
            apartmentNo: '',
            postalCode: '',
            isDefault: false
        };
    }

    private getErrorMessage(error: unknown, fallback: string): string {
        const response = error as {
            error?: {
                message?: string;
                messageEn?: string;
                title?: string;
                errors?: Array<{ message?: string }> | Record<string, unknown>;
            };
            message?: string;
        };

        const firstValidation = this.extractValidationErrorMessage(response?.error?.errors);

        return firstValidation
            || response?.error?.messageEn
            || response?.error?.message
            || response?.error?.title
            || response?.message
            || fallback;
    }

    private extractValidationErrorMessage(rawErrors: unknown): string | undefined {
        if (Array.isArray(rawErrors)) {
            const firstArrayError = rawErrors.find(
                (entry): entry is { message?: string } =>
                    typeof entry === 'object' && entry !== null && 'message' in entry
            );

            return firstArrayError?.message;
        }

        if (rawErrors && typeof rawErrors === 'object') {
            const errorValues = Object.values(rawErrors as Record<string, unknown>);

            for (const value of errorValues) {
                if (Array.isArray(value)) {
                    const firstMessage = value.find((item): item is string => typeof item === 'string' && item.length > 0);

                    if (firstMessage) {
                        return firstMessage;
                    }
                }
            }
        }

        return undefined;
    }

    private isTabKey(tab: string | null): tab is 'profile' | 'password' | 'addresses' {
        return !!tab && this.TAB_KEYS.includes(tab as (typeof this.TAB_KEYS)[number]);
    }

    logout() {
        this.store.dispatch(AuthActions.logout());
    }
}
