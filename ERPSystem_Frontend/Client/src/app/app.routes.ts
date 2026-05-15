import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './components/auth-guard';
import { RoleGuard } from './components/role.guard';
import { unsavedChangesGuard } from './components/guards/unsaved-changes.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  { path: 'login', loadComponent: () => import('./pages/account/login/login.component').then(c => c.LoginComponent) },
  { path: 'confirm-email-registration', loadComponent: () => import('./pages/account/confirm-email-registration/confirm-email-registration.component').then(c => c.ConfirmEmailRegistrationComponent) },
  { path: 'forgot-password', loadComponent: () => import('./pages/account/forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent) },
  { path: 'confirm-login-code', loadComponent: () => import('./pages/account/confirm-login-code/confirm-login-code.component').then(c => c.ConfirmLoginCodeComponent) },
  { path: 'reset-password', loadComponent: () => import('./pages/account/reset-password/reset-password.component').then(c => c.ResetPasswordComponent) },
  { path: 'sign/:token', loadComponent: () => import('./pages/financiar/sign-contract/sign-contract.component').then(m => m.SignContractComponent) },
  { path: 'feedback/:token', loadComponent: () => import('./pages/feedback/feedback-form/feedback-form.component').then(m => m.FeedbackFormComponent) },

  {
    path: 'overview-dashboard',
    loadComponent: () => import('./pages/dashboard-analysis/overview-dashboard/overview-dashboard.component').then(m => m.OverviewDashboardComponent),
    canActivate: [AuthGuard]
  },

  {
    path: 'financial-dashboard',
    loadComponent: () => import('./pages/dashboard-analysis/financial-dashboard/financial-dashboard.component').then(m => m.FinancialDashboardComponent),
    canActivate: [AuthGuard]
  },

   {
    path: 'education-dashboard',
    loadComponent: () => import('./pages/dashboard-analysis/education-dashboard/education-dashboard.component').then(m => m.EducationDashboardComponent),
    canActivate: [AuthGuard]
  },

  {
    path: 'hr-dashboard',
    loadComponent: () => import('./pages/dashboard-analysis/hr-dashboard/hr-dashboard.component').then(m => m.HrDashboardComponent),
    canActivate: [AuthGuard]
  },

  {
    path: 'profil-user',
    loadComponent: () => import('./pages/account/profil-user/profil-user.component').then(m => m.ProfilUserComponent),
    canActivate: [AuthGuard]
  },

  {
    path: 'register',
    loadComponent: () => import('./pages/account/register/register.component').then(c => c.RegisterComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },

  {
    path: 'students',
    loadComponent: () => import('./pages/academics/students/students.component').then(m => m.StudentsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Secretary', 'Teacher'] }
  },
  {
    path: 'students/:id',
    loadComponent: () => import('./pages/academics/student-details/student-details.component').then(m => m.StudentDetailsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Secretary', 'Teacher'] }
  },
  {
    path: 'courses',
    loadComponent: () => import('./pages/academics/courses/courses.component').then(m => m.CoursesComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Secretary', 'Teacher'] }
  },
  {
    path: 'courses/:id',
    loadComponent: () => import('./pages/academics/course-details/course-details.component').then(m => m.CourseDetailsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Secretary', 'Teacher'] }
  },

  {
    path: 'create-contract',
    loadComponent: () => import('./pages/financiar/create-contract/create-contract.component').then(m => m.CreateContractComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant', 'Secretary'] }
  },
  {
    path: 'contracts/edit/:id',
    loadComponent: () => import('./pages/financiar/create-contract/create-contract.component').then(m => m.CreateContractComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant', 'Secretary'] }
  },
  {
    path: 'contracts/:id',
    loadComponent: () => import('./pages/financiar/contract-details/contract-details.component').then(m => m.ContractDetailsComponent),
    canActivate: [AuthGuard, RoleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant', 'Secretary'] }
  },
  {
    path: 'contracts/:contractId/additional-act',
    loadComponent: () => import('./pages/financiar/aditional-act/aditional-act.component').then(m => m.AditionalActComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant'] }
  },
  {
    path: 'additional-act/:id',
    loadComponent: () => import('./pages/financiar/additional-act-details/additional-act-details.component').then(m => m.AdditionalActDetailsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant', 'Secretary'] }
  },
  {
    path: 'additional-act/edit/:actId',
    loadComponent: () => import('./pages/financiar/aditional-act/aditional-act.component').then(m => m.AditionalActComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant'] }
  },
  {
    path: 'all-contracts',
    loadComponent: () => import('./pages/financiar/all-contracts/all-contracts.component').then(m => m.AllContractsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Accountant', 'Secretary'] }
  },

  {
    path: 'employee/:id',
    loadComponent: () => import('./pages/hr/employee-details/employee-details.component').then(m => m.EmployeeDetailsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'HR', 'Manager'] }
  },
  {
    path: 'employees',
    loadComponent: () => import('./pages/hr/hr-employees/hr-employees.component').then(m => m.HrEmployeesComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'HR', 'Manager'] }
  },
  {
    path: 'all-leaves',
    loadComponent: () => import('./pages/hr/all-leaves/all-leaves.component').then(m => m.AllLeavesComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'HR', 'Manager'] }
  },

  {
    path: 'admin/users',
    loadComponent: () => import('./pages/admin/admin-users/admin-users.component').then(m => m.AdminUsersComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager' ] }
  },
  {
    path: 'user-details/:id',
    loadComponent: () => import('./pages/admin/user-details/user-details.component').then(c => c.UserDetailsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager'] }
  },
  {
    path: 'company',
    loadComponent: () => import('./pages/admin/company/company.component').then(m => m.CompanyComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Secretary'] }
  },
  {
    path: 'log-activity',
    loadComponent: () => import('./pages/admin/admin-activity/admin-activity.component').then(m => m.AdminActivityComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },

  {
    path: 'mk-campaign',
    loadComponent: () => import('./pages/marketing/marketing-campaigns/marketing-campaigns.component').then(m => m.MarketingCampaignsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Marketing'] }
  },

  {
    path: 'external-feedback',
    loadComponent: () => import('./pages/feedback/external-feedback/external-feedback.component').then(m => m.ExternalFeedbackComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Marketing'] }
  },
  {
    path: 'feedback/analytics/global',
    loadComponent: () => import('./pages/feedback/global-feedback-analytics/global-feedback-analytics.component').then(m => m.GlobalFeedbackAnalyticsComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Manager', 'Marketing'] }
  },

  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true })],
})
export class AppRoutingModule { }
