import { CommonModule, CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChartModule } from 'primeng/chart';
import { forkJoin } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { DashboardSummary } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, ChartModule],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  errorMessage: string | null = null;
  dashboard: DashboardSummary = {
    totalUsers: 0, activeUsers: 0, inactiveUsers: 0,
    totalPolicies: 0, totalClaims: 0,
    pendingClaims: 0, approvedClaims: 0, rejectedClaims: 0,
    totalRevenue: 0, closedClaims: 0
  };

  claimStatusChart: any = null;
  usersChart: any = null;
  overviewChart: any = null;
  chartOptions: any = null;
  barChartOptions: any = null;

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = null;

    forkJoin([
      this.adminService.getDashboard(),
      this.adminService.getAllUsers()
    ]).subscribe({
      next: ([dashboard, users]) => {
        const active   = users.filter(u => u.isActive).length;
        const inactive = users.length - active;
        this.dashboard = {
          ...dashboard,
          totalUsers:   users.length,
          activeUsers:  active,
          inactiveUsers: inactive
        };
        this.buildCharts();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err?.name === 'TimeoutError'
          ? 'Request timed out. Make sure AdminService is running, then click Retry.'
          : 'Could not load dashboard. Ensure all backend services are running.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildCharts(): void {
    this.claimStatusChart = {
      labels: ['Pending', 'Approved', 'Rejected', 'Closed'],
      datasets: [{
        data: [
          this.dashboard.pendingClaims,
          this.dashboard.approvedClaims,
          this.dashboard.rejectedClaims,
          this.dashboard.closedClaims
        ],
        backgroundColor: ['#f59e0b', '#10b981', '#ef4444', '#6b7280'],
        hoverBackgroundColor: ['#d97706', '#059669', '#dc2626', '#4b5563'],
        borderWidth: 0
      }]
    };

    this.usersChart = {
      labels: ['Total Users', 'Active Users', 'Inactive Users'],
      datasets: [{
        label: 'Users',
        data: [
          this.dashboard.totalUsers,
          this.dashboard.activeUsers,
          this.dashboard.inactiveUsers
        ],
        backgroundColor: ['#3b82f6', '#10b981', '#ef4444'],
        hoverBackgroundColor: ['#2563eb', '#059669', '#dc2626'],
        borderRadius: 8,
        borderSkipped: false
      }]
    };

    this.overviewChart = {
      labels: ['Total Users', 'Active Users', 'Policies', 'Total Claims', 'Pending', 'Approved', 'Rejected'],
      datasets: [{
        label: 'Count',
        data: [
          this.dashboard.totalUsers,
          this.dashboard.activeUsers,
          this.dashboard.totalPolicies,
          this.dashboard.totalClaims,
          this.dashboard.pendingClaims,
          this.dashboard.approvedClaims,
          this.dashboard.rejectedClaims
        ],
        backgroundColor: ['#3b82f6', '#10b981', '#8b5cf6', '#f97316', '#f59e0b', '#14b8a6', '#ef4444'],
        borderRadius: 8,
        borderSkipped: false
      }]
    };

    this.chartOptions = {
      plugins: {
        legend: { position: 'bottom', labels: { usePointStyle: true, padding: 20, font: { size: 12 } } }
      },
      cutout: '68%'
    };

    this.barChartOptions = {
      plugins: { legend: { display: false } },
      scales: {
        y: { beginAtZero: true, grid: { color: 'rgba(0,0,0,.05)' }, ticks: { precision: 0, font: { size: 11 } } },
        x: { grid: { display: false }, ticks: { font: { size: 11 } } }
      }
    };
  }
}
