import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './components/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: 'register', pathMatch: 'full' },
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
  { path: 'contracts/:id', loadComponent: () => import('./pages/financiar/contract-details/contract-details.component').then(m => m.ContractDetailsComponent) },
  { path: 'admin/users', loadComponent: () => import('./pages/admin-users/admin-users.component').then(m => m.AdminUsersComponent) },
  { path: 'sign-contract/:token', loadComponent: () => import('./pages/financiar/sign-contract/sign-contract.component') .then(m => m.SignContractComponent) },
  { path: 'company', loadComponent: () => import('./pages/admin/company/company.component') .then(m => m.CompanyComponent) }, 
  { path: 'log-activity', loadComponent: () => import('./pages/admin/admin-activity/admin-activity.component') .then(m => m.AdminActivityComponent) }


];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true })],
})
export class AppRoutingModule { }
