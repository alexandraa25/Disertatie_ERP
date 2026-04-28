import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './components/auth-guard';
import { unsavedChangesGuard } from './components/guards/unsaved-changes.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./pages/account/login/login.component').then(c => c.LoginComponent) },
  { path: 'register', loadComponent: () => import('./pages/account/register/register.component').then(c => c.RegisterComponent) },
  { path: 'profil-user', loadComponent: () => import('./pages/account/profil-user/profil-user.component').then(m => m.ProfilUserComponent), canActivate: [AuthGuard] },
  { path: 'confirm-email-registration', loadComponent: () => import('./pages/account/confirm-email-registration/confirm-email-registration.component').then(c => c.ConfirmEmailRegistrationComponent) },
  { path: 'forgot-password', loadComponent: () => import('./pages/account/forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent) },
  { path: 'confirm-login-code', loadComponent: () => import('./pages/account/confirm-login-code/confirm-login-code.component').then(c => c.ConfirmLoginCodeComponent) },
  { path: 'reset-password', loadComponent: () => import('./pages/account/reset-password/reset-password.component').then(c => c.ResetPasswordComponent) },
  { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'students/:id', loadComponent: () => import('./pages/academics/student-details/student-details.component').then(m => m.StudentDetailsComponent) },
  { path: 'students', loadComponent: () => import('./pages/academics/students/students.component').then(m => m.StudentsComponent) },
  { path: 'courses', loadComponent: () => import('./pages/academics/courses/courses.component').then(m => m.CoursesComponent) },
  { path: 'courses/:id', loadComponent: () => import('./pages/academics/course-details/course-details.component').then(m => m.CourseDetailsComponent) },
  { path: 'create-contract', loadComponent: () => import('./pages/financiar/create-contract/create-contract.component').then(m => m.CreateContractComponent) },
  { path: 'contracts/edit/:id', loadComponent: () => import('./pages/financiar/create-contract/create-contract.component').then(m => m.CreateContractComponent) },
  { path: 'sign/:token', loadComponent: () => import('./pages/financiar/sign-contract/sign-contract.component') .then(m => m.SignContractComponent) },
  { path: 'contracts/:id', loadComponent: () => import('./pages/financiar/contract-details/contract-details.component').then(m => m.ContractDetailsComponent), canDeactivate: [unsavedChangesGuard] },
  { path: 'contracts/:contractId/additional-act', loadComponent: () => import('./pages/financiar/aditional-act/aditional-act.component') .then(m => m.AditionalActComponent) }, 
  { path: 'additional-act/:id', loadComponent: () => import('./pages/financiar/additional-act-details/additional-act-details.component') .then(m => m.AdditionalActDetailsComponent) }, 
  { path: 'additional-act/edit/:actId', loadComponent: () => import('./pages/financiar/aditional-act/aditional-act.component') .then(m => m.AditionalActComponent) }, 
  { path: 'employee/:id', loadComponent: () => import('./pages/hr/employee-details/employee-details.component').then(m => m.EmployeeDetailsComponent) },
  { path: 'employees', loadComponent: () => import('./pages/hr/hr-employees/hr-employees.component').then(m => m.HrEmployeesComponent) },
  { path: 'all-leaves', loadComponent: () => import('./pages/hr/all-leaves/all-leaves.component').then(m => m.AllLeavesComponent) },
  { path: 'admin/users', loadComponent: () => import('./pages/admin/admin-users/admin-users.component').then(m => m.AdminUsersComponent) },
  { path: 'user-details/:id',  loadComponent: () => import('./pages/admin/user-details/user-details.component')  .then(c => c.UserDetailsComponent)},
  { path: 'company', loadComponent: () => import('./pages/admin/company/company.component') .then(m => m.CompanyComponent) }, 
  { path: 'log-activity', loadComponent: () => import('./pages/admin/admin-activity/admin-activity.component') .then(m => m.AdminActivityComponent) }, 
  { path: 'mk-campaign', loadComponent: () => import('./pages/marketing/marketing-campaigns/marketing-campaigns.component') .then(m => m.MarketingCampaignsComponent) }, 
  { path: 'feedback/:token', loadComponent: () =>  import('./pages/feedback/feedback-form/feedback-form.component') .then(m => m.FeedbackFormComponent)}, 
  { path: '**', redirectTo: 'login' }



];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true })],
})
export class AppRoutingModule { }
