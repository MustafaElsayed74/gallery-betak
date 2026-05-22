import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { OrderService, OrderSummaryDto } from '../../../core/services/api/order.service';
import { UiTextService } from '../../../core/services/ui-text.service';

@Component({
  selector: 'app-my-orders',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-orders.component.html',
  styleUrl: './my-orders.component.css'
})
export class MyOrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private uiTextService = inject(UiTextService);

  orders: OrderSummaryDto[] = [];
  isLoading = true;
  errorMessage = '';
  uiMessages: any;

  constructor() {
    this.uiMessages = this.uiTextService.getCurrentMessages();
  }

  ngOnInit() {
    this.uiTextService.messages$.subscribe((messages: any) => {
      this.uiMessages = messages;
    });
    this.loadOrders();
  }

  loadOrders() {
    this.isLoading = true;
    this.orderService.getOrdersForUser().subscribe({
      next: (orders: OrderSummaryDto[]) => {
        this.orders = orders;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load orders.';
        this.isLoading = false;
      }
    });
  }
}
