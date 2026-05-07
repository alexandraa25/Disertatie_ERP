import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { ActivityLogService } from '../../services/activity-log.service';

@Component({
  selector: 'app-user-details',
  standalone: true,
  imports: [CommonModule, ConfirmCustomModalComponent],
  templateUrl: './user-details.component.html',
  styleUrls: ['./user-details.component.css']
})
export class UserDetailsComponent implements OnInit {

  user: any;
  userId!: string;
  activeTab: 'profile' | 'security' | 'audit' = 'profile';
  availableRoles = [
    'Admin',
    'Manager',
    'Secretary',
    'Teacher',
    'Marketing',
    'Accountant'
  ];

  selectedRoles: string[] = [];
  isRoleEditMode = false;

  auditLogs: any[] = [];
auditLoaded = false;

auditPage = 1;
auditPageSize = 10;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adminService: AdminService,
    private snackbar: SnackbarService,
    private confirmService: ConfirmService,
    private activityLogService: ActivityLogService
  ) { }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.router.navigate(['/admin-users']);
      return;
    }

    this.userId = id;

    this.adminService.getProfileByUserId(id).subscribe({
      next: (res: any) => {
        this.user = res.value ?? res;
        this.selectedRoles = [...(this.user.roles || [])];
      },
      error: () => {
        this.router.navigate(['/admin-users']);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/users']);
  }

  viewEmployeeDetails(): void {
    if (!this.user?.employeeId) {
      return;
    }

    this.router.navigate(['/employee', this.user.employeeId]);
  }

  enableRoleEdit(): void {
    this.selectedRoles = [...(this.user.roles || [])];
    this.isRoleEditMode = true;
  }

  cancelRoleEdit(): void {
    this.selectedRoles = [...(this.user.roles || [])];
    this.isRoleEditMode = false;
  }

  toggleRole(role: string): void {
    if (this.selectedRoles.includes(role)) {
      this.selectedRoles = this.selectedRoles.filter(r => r !== role);
    } else {
      this.selectedRoles.push(role);
    }
  }

  saveRoles(): void {
    this.adminService.updateUserRoles(this.userId, this.selectedRoles).subscribe({
      next: (res: any) => {
        if (res.isSuccess === false) {
          this.snackbar.showError('Rolurile nu au putut fi actualizate.', 2000);
          return;
        }

        this.user.roles = res.value.roles ?? res.value.Roles ?? this.selectedRoles;
        this.selectedRoles = [...this.user.roles];
        this.isRoleEditMode = false;

        this.snackbar.showSuccess('Roluri actualizate cu succes.', 1500);
      },
      error: () => {
        this.snackbar.showError('A apărut o eroare.', 2000);
      }
    });
  }

  goToForgotPassword(): void {
    if (!this.user?.email) {
      return;
    }

    this.router.navigate(['/forgot-password'], {
      queryParams: {
        email: this.user.email
      }
    });
  }

  async askToggleUserStatus(): Promise<void> {
    const action = this.user.isActive ? 'dezactivezi' : 'activezi';

    const confirmed = await this.confirmService.confirm(
      'Confirmare',
      `Sigur vrei să ${action} acest cont?`
    );

    if (!confirmed) return;

    this.toggleUser();
  }

  toggleUser(): void {
    this.adminService.toggleUserStatus(this.userId).subscribe({
      next: (res: any) => {
        if (res.isSuccess === false) {
          this.snackbar.showError('Statusul contului nu a putut fi actualizat.', 2000);
          return;
        }

        this.user.isActive = res.value.isActive;

        this.snackbar.showSuccess(
          this.user.isActive ? 'Cont activat.' : 'Cont dezactivat.',
          1500
        );
      },
      error: () => {
        this.snackbar.showError('A apărut o eroare.', 2000);
      }
    });
  }

  async confirmEmailManually(): Promise<void> {
    const confirmed = await this.confirmService.confirm(
      'Confirmare',
      `Sigur vrei să confirmi manual emailul ${this.user.email}?`
    );

    if (!confirmed) return;

    this.adminService.confirmUserEmail(this.userId).subscribe({
      next: (res: any) => {
        if (res.isSuccess === false) {
          this.snackbar.showError('Emailul nu a putut fi confirmat.', 2000);
          return;
        }

        this.user.emailConfirmed = true;
        this.snackbar.showSuccess('Email confirmat cu succes.', 1500);
      },
      error: () => {
        this.snackbar.showError('A apărut o eroare.', 2000);
      }
    });
  }

openAuditTab(): void {
  this.activeTab = 'audit';

  if (this.auditLoaded || !this.userId) return;

  this.activityLogService
    .getActivity('User', this.userId.toString())
    .subscribe({
      next: (res: any[]) => {
        this.auditLogs = res ?? [];
        this.auditPage = 1;
        this.auditLoaded = true;
      },
      error: () => {
        this.snackbar.showError(
          'Istoricul nu a putut fi încărcat.',
          2500
        );
      }
    });
}

get pagedAuditLogs(): any[] {
  const start = (this.auditPage - 1) * this.auditPageSize;
  return this.auditLogs.slice(start, start + this.auditPageSize);
}

get auditTotalPages(): number {
  return Math.ceil(this.auditLogs.length / this.auditPageSize);
}

changeAuditPage(page: number): void {
  if (page < 1 || page > this.auditTotalPages) {
    return;
  }

  this.auditPage = page;
}
}