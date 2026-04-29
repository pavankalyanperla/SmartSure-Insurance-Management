import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';

interface ReportField {
  key: string;
  label: string;
  color: string;
  isCurrency?: boolean;
}

const REPORT_FIELDS: Record<string, ReportField[]> = {
  ClaimsSummary: [
    { key: 'totalClaims',    label: 'Total Claims',    color: 'orange' },
    { key: 'pendingClaims',  label: 'Pending Claims',  color: 'orange' },
    { key: 'approvedClaims', label: 'Approved Claims', color: 'green'  },
    { key: 'rejectedClaims', label: 'Rejected Claims', color: 'red'    },
    { key: 'closedClaims',   label: 'Closed Claims',   color: 'purple' }
  ],
  PolicySummary: [
    { key: 'totalPolicies', label: 'Total Policies', color: 'green'  },
    { key: 'totalUsers',    label: 'Total Users',    color: 'blue'   },
    { key: 'totalRevenue',  label: 'Total Revenue',  color: 'teal', isCurrency: true }
  ],
  RevenueSummary: [
    { key: 'totalRevenue',  label: 'Total Revenue',  color: 'teal', isCurrency: true },
    { key: 'totalPolicies', label: 'Policies Sold',  color: 'green'  },
    { key: 'totalClaims',   label: 'Claims Filed',   color: 'orange' }
  ]
};

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe],
  templateUrl: './admin-reports.component.html'
})
export class AdminReportsComponent {
  private readonly adminService = inject(AdminService);
  private readonly cdr = inject(ChangeDetectorRef);

  selectedReportType = 'ClaimsSummary';
  reportData: Record<string, unknown> | null = null;
  isGenerating = false;
  generatedAt: Date | null = null;
  generatedForType = '';

  reportTypes = [
    { label: 'Claims Summary',  value: 'ClaimsSummary'  },
    { label: 'Policy Summary',  value: 'PolicySummary'  },
    { label: 'Revenue Summary', value: 'RevenueSummary' }
  ];

  get currentFields(): ReportField[] {
    return REPORT_FIELDS[this.generatedForType] ?? [];
  }

  get generatedForLabel(): string {
    return this.reportTypes.find(t => t.value === this.generatedForType)?.label ?? '';
  }

  onTypeChange(): void {
    this.reportData = null;
    this.generatedAt = null;
    this.generatedForType = '';
  }

  generateReport(): void {
    this.isGenerating = true;
    this.reportData = null;

    this.adminService.generateReport(this.selectedReportType).subscribe({
      next: (data) => {
        this.reportData = data as Record<string, unknown>;
        this.generatedAt = new Date();
        this.generatedForType = this.selectedReportType;
        this.isGenerating = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isGenerating = false;
        this.cdr.detectChanges();
      }
    });
  }

  getValue(key: string): unknown {
    return this.reportData?.[key];
  }

  exportReport(): void {
    const blob = new Blob([JSON.stringify(this.reportData, null, 2)], { type: 'application/json' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `smartsure-${this.generatedForType.toLowerCase()}-report.json`;
    link.click();
    URL.revokeObjectURL(link.href);
  }
}
