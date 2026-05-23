import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderService, OrderDto } from '../../../core/services/api/order.service';

@Component({
  selector: 'app-order-details',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './order-details.component.html',
  styleUrl: './order-details.component.css'
})
export class OrderDetailsComponent implements OnInit {
  order: OrderDto | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private orderService: OrderService
  ) {}

  ngOnInit(): void {
    const orderIdParam = this.route.snapshot.paramMap.get('id');
    if (orderIdParam) {
      const orderId = Number(orderIdParam);
      if (!isNaN(orderId)) {
        this.fetchOrderDetails(orderId);
      } else {
        this.errorMessage = 'Invalid order ID format.';
        this.isLoading = false;
      }
    } else {
      this.errorMessage = 'Order ID is missing in the URL.';
      this.isLoading = false;
    }
  }

  private fetchOrderDetails(id: number): void {
    this.orderService.getOrderForUser(id).subscribe({
      next: (data: OrderDto) => {
        this.order = data;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error fetching order details', error);
        this.errorMessage = 'Failed to load order details. It may not exist or you do not have permission.';
        this.isLoading = false;
      }
    });
  }
}
