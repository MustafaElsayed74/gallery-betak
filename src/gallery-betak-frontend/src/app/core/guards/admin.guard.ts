import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectUser } from '../store/auth/auth.reducer';

export const adminGuard: CanActivateFn = () => {
    const store = inject(Store);
    const router = inject(Router);

    return store.select(selectUser).pipe(
        take(1),
        map(user => {
            const isAdmin = (user?.roles ?? []).some(role => ['Admin', 'SuperAdmin'].includes(role));
            return isAdmin ? true : router.createUrlTree(['/account']);
        })
    );
};
