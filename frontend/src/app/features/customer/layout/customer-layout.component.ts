import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-customer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './customer-layout.component.html'
})
export class CustomerLayoutComponent {
  private readonly authService = inject(AuthService);
  readonly userName = this.authService.getCurrentUserName();

  logout(): void {
    this.authService.logout();
  }
}