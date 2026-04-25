import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { LandingComponent } from './features/landing/landing.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CustomerLayoutComponent } from './features/customer/layout/customer-layout.component';
import { CustomerDashboardComponent } from './features/customer/dashboard/customer-dashboard.component';
import { PolicyListComponent } from './features/customer/policies/policy-list.component';
import { PolicyDetailComponent } from './features/customer/policy-detail/policy-detail.component';
import { ClaimListComponent } from './features/customer/claims/claim-list.component';
import { ClaimDetailComponent } from './features/customer/claim-detail/claim-detail.component';
import { BuyPolicyComponent } from './features/customer/buy-policy/buy-policy.component';
import { InitiateClaimComponent } from './features/customer/initiate-claim/initiate-claim.component';
import { AdminLayoutComponent } from './features/admin/layout/admin-layout.component';
import { AdminDashboardComponent } from './features/admin/dashboard/admin-dashboard.component';
import { AdminClaimsComponent } from './features/admin/claims/admin-claims.component';
import { AdminUsersComponent } from './features/admin/users/admin-users.component';
import { AdminReportsComponent } from './features/admin/reports/admin-reports.component';
import { AdminPolicyManagementComponent } from './features/admin/policy-management/admin-policy-management.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: 'home', component: LandingComponent },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', component: RegisterComponent },
  {
    path: 'customer',
    component: CustomerLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: CustomerDashboardComponent },
      { path: 'policies', component: PolicyListComponent },
      { path: 'policies/:id', component: PolicyDetailComponent },
      { path: 'claims', component: ClaimListComponent },
      { path: 'claims/:id', component: ClaimDetailComponent },
      { path: 'buy-policy', component: BuyPolicyComponent },
      { path: 'initiate-claim', component: InitiateClaimComponent }
    ]
  },
  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [adminGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: AdminDashboardComponent },
      { path: 'claims', component: AdminClaimsComponent },
      { path: 'users', component: AdminUsersComponent },
      { path: 'reports', component: AdminReportsComponent },
      { path: 'policies', component: AdminPolicyManagementComponent }
    ]
  },
  { path: '**', redirectTo: 'home' }
];