// @ts-nocheck
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
    id: number;
    message: string;
    type: ToastType;
}

@Injectable({
    providedIn: 'root'
})
export class ToastService {
    private readonly toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
    readonly toasts$ = this.toastsSubject.asObservable();

    private nextId = 1;

    success(message: string, durationMs = 3000): void {
        this.show('success', message, durationMs);
    }

    error(message: string, durationMs = 4000): void {
        this.show('error', message, durationMs);
    }

    info(message: string, durationMs = 3000): void {
        this.show('info', message, durationMs);
    }

    dismiss(id: number): void {
        const updated = this.toastsSubject.value.filter((toast: ToastMessage) => toast.id !== id);
        this.toastsSubject.next(updated);
    }

    private show(type: ToastType, message: string, durationMs: number): void {
        const toast: ToastMessage = {
            id: this.nextId++,
            type,
            message
        };

        this.toastsSubject.next([...this.toastsSubject.value, toast]);

        window.setTimeout(() => {
            this.dismiss(toast.id);
        }, durationMs);
    }
}
