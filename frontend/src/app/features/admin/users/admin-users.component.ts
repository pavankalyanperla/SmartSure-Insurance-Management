import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AdminUser } from '../../../core/models/admin.models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule],
  templateUrl: './admin-users.component.html'
})
export class AdminUsersComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly messageService = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  errorMessage: string | null = null;
  users: AdminUser[] = [];
  filteredUsers: AdminUser[] = [];
  searchText = '';

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.adminService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data || [];
        this.filteredUsers = [...this.users];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err?.name === 'TimeoutError'
          ? 'Request timed out. Make sure AdminService is running, then click Retry.'
          : `Failed to load users (${err?.status ?? 'network error'}). Check that all services are running.`;
        this.users = [];
        this.filteredUsers = [];
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applySearch(): void {
    const s = this.searchText.trim().toLowerCase();
    if (!s) {
      this.filteredUsers = [...this.users];
      return;
    }
    this.filteredUsers = this.users.filter(u =>
      u.fullName?.toLowerCase().includes(s) || u.email?.toLowerCase().includes(s)
    );
  }

  toggleStatus(userId: number, isActive: boolean): void {
    if (!userId) return;
    this.adminService.updateUserStatus(userId, isActive).subscribe({
      next: () => {
        const user = this.users.find(u => u.userId === userId);
        if (user) user.isActive = isActive;
        this.applySearch();
        this.messageService.add({ severity: 'success', summary: 'User updated', detail: `User ${isActive ? 'activated' : 'deactivated'} successfully.` });
      },
      error: (err) => console.error('Error:', err)
    });
  }
}
