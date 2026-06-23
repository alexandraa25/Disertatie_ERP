import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { AdminUser, AdminDashboard } from '../../models/admin-user.model';
import { Router } from '@angular/router';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmCustomModalComponent],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.css']
})
export class AdminUsersComponent implements OnInit {

  users: AdminUser[] = [];
  loading = false;

  employeesWithoutUser: any[] = [];

  totalUsers = 0;
  activeUsers = 0;
  inactiveUsers = 0;
  adminUsers = 0;
  searchText = '';
  roleFilter = '';
  statusFilter = '';
  currentPage = 1;
  pageSize = 10;

  canToggleStatus = false;
  canCreateUser = false;

  constructor(
    private adminService: AdminService,
    private router: Router,
    private snackbar: SnackbarService,
    private confirmService: ConfirmService,
    private auth: AuthService,
  ) {
    this.canToggleStatus = this.auth.hasRole(['Admin', 'Manager']);
    this.canCreateUser = this.auth.hasRole(['Admin']);
  }


  filteredUsers() {

    let result = this.users;

    if (this.searchText) {
      const text = this.searchText.toLowerCase();

      result = result.filter(u =>
        u.username?.toLowerCase().includes(text) ||
        u.email?.toLowerCase().includes(text) ||
        (u.firstName + ' ' + u.lastName).toLowerCase().includes(text)
      );
    }

    if (this.roleFilter) {
      result = result.filter(u =>
        u.roles?.includes(this.roleFilter)
      );
    }

    if (this.statusFilter === 'active') {
      result = result.filter(u => u.isActive);
    }

    if (this.statusFilter === 'inactive') {
      result = result.filter(u => !u.isActive);
    }

    return result;

  }

 detailsUser(user: AdminUser) {
  this.router.navigate(['/user-details', user.id]);
}

async askToggleUserStatus(user: AdminUser): Promise<void> {
  const action = user.isActive ? 'dezactivezi' : 'activezi';

  const confirmed = await this.confirmService.confirm(
    'Confirmare',
    `Sigur vrei să ${action} contul utilizatorului ${user.email}?`
  );

  if (!confirmed) return;

  this.toggleUser(user);
}

 toggleUser(user: AdminUser): void {
  this.adminService.toggleUserStatus(user.id).subscribe({
    next: (res: any) => {
      if (res.isSuccess === false) {
        this.snackbar.showError('Statusul utilizatorului nu a putut fi actualizat.', 2000);
        return;
      }

      user.isActive = res.value.isActive;

      this.snackbar.showSuccess(
        user.isActive ? 'Utilizator activat.' : 'Utilizator dezactivat.',
        1500
      );
    },
    error: () => {
      this.snackbar.showError('A apărut o eroare.', 2000);
    }
  });
}

  loadDashboard() {

    this.adminService.getDashboard().subscribe(data => {

      this.users = data.users;

      this.totalUsers = data.totalUsers;
      this.activeUsers = data.activeUsers;
      this.inactiveUsers = data.inactiveUsers;
      this.adminUsers = data.adminUsers;

    });

  }


  get totalPages(): number {
    return Math.ceil(this.filteredUsers().length / this.pageSize);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  paginatedUsers() {

    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;

    return this.filteredUsers().slice(start, end);

  }
  ngOnInit(): void {
  this.loadDashboard();
  this.loadEmployeesWithoutUser();
}

  loadUsers() {
    this.adminService.getDashboard().subscribe(data => {

      this.users = data.users;
    });

  }

  loadEmployeesWithoutUser() {
  this.adminService.getEmployeesWithoutUser().subscribe(res => {
    console.log('EMP NO USER:', res); // 🔥 IMPORTANT
    this.employeesWithoutUser = res;
  });
}

  goToRegister(emp: any) {
    this.router.navigate(['/register'], {
      queryParams: {
        firstName: emp.firstName,
        lastName: emp.lastName,
        email: emp.email,
        phoneNumber: emp.phoneNumber,
        jobTitle: emp.jobTitle,
        employeeId: emp.id
      }
    });
  }
}
