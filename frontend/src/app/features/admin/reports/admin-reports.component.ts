import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe],
  templateUrl: './admin-reports.component.html'
})
export class AdminReportsComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  selectedReportType = 'Summary';
  reportData: Record<string, unknown> | null = null;
  isGenerating = false;
  generatedAt: Date | null = null;

  reportTypes = [
    { label: 'Summary Report', value: 'Summary' },
    { label: 'Claims Report', value: 'ClaimsSummary' },
    { label: 'Revenue Report', value: 'RevenueSummary' }
  ];

  ngOnInit(): void {
    setTimeout(() => {
      this.isGenerating = false;
    }, 0);
  }

  generateReport(): void {
    this.isGenerating = true;
    this.reportData = null;

    this.adminService.generateReport(this.selectedReportType).subscribe({
      next: (data) => {
        const payload = data as Record<string, unknown>;
        this.reportData = (payload?.['data'] as Record<string, unknown>) || payload;
        this.generatedAt = new Date();
        this.isGenerating = false;
      },
      error: (err) => {
        console.error('Report generation error:', err);
        this.isGenerating = false;
        alert('Failed to generate report. Please try again.');
      }
    });
  }

  exportReport(): void {
    const blob = new Blob([JSON.stringify(this.reportData, null, 2)], { type: 'application/json' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `smartsure-${this.selectedReportType.toLowerCase().replace(/\s+/g, '-')}-report.json`;
    link.click();
    URL.revokeObjectURL(link.href);
  }
}