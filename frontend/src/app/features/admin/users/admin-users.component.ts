import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { AdminUser } from '../../../core/models/admin.models';
import { DatePipe } from '@angular/common';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './admin-users.component.html'
})
export class AdminUsersComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly messageService = inject(MessageService);

  isLoading = true;
  users: AdminUser[] = [];

  ngOnInit(): void {
    setTimeout(() => {
      this.loadUsers();
    }, 0);
  }

  loadUsers(): void {
    this.isLoading = true;
    this.adminService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data || [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Users error:', err);
        this.users = [];
        this.isLoading = false;
      }
    });
  }

  toggleStatus(userId: number, isActive: boolean): void {
    if (!userId || userId === 0) {
      return;
    }
    this.adminService.updateUserStatus(userId, isActive).subscribe({
      next: () => {
        const user = this.users.find((u) => u.userId === userId);
        if (user) {
          user.isActive = isActive;
        }
        this.messageService.add({ severity: 'success', summary: 'User updated', detail: `User ${isActive ? 'activated' : 'deactivated'} successfully.` });
      },
      error: (err) => {
        console.error('Error:', err);
      }
    });
  }
}