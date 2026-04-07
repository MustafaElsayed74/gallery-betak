import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class SessionBannerService {
    private readonly isVisibleSubject = new BehaviorSubject<boolean>(false);
    readonly isVisible$ = this.isVisibleSubject.asObservable();

    showSessionExpired(): void {
        if (this.isVisibleSubject.value) {
            return;
        }

        this.isVisibleSubject.next(true);
    }

    dismiss(): void {
        this.isVisibleSubject.next(false);
    }
}
