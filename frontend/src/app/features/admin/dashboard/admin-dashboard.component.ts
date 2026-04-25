import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { DashboardSummary } from '../../../core/models/admin.models';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  isLoading = true;
  dashboard: DashboardSummary = {
    totalUsers: 0,
    totalPolicies: 0,
    totalClaims: 0,
    pendingClaims: 0,
    approvedClaims: 0,
    rejectedClaims: 0,
    totalRevenue: 0,
    closedClaims: 0
  };

  ngOnInit(): void {
    setTimeout(() => {
      this.loadDashboard();
    }, 0);
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.adminService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Dashboard load error:', err);
        this.dashboard = {
          totalUsers: 0,
          totalPolicies: 0,
          totalClaims: 0,
          pendingClaims: 0,
          approvedClaims: 0,
          rejectedClaims: 0,
          totalRevenue: 0,
          closedClaims: 0
        };
        this.isLoading = false;
      }
    });
  }
}