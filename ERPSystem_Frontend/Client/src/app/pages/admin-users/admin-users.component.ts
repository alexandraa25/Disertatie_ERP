import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { AdminUser, AdminDashboard } from '../models/admin-user.model';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {

   users: AdminUser[] = [];
  loading = false;

   totalUsers = 0;
  activeUsers = 0;
  inactiveUsers = 0;
  adminUsers = 0;
   searchText = '';
   roleFilter = '';
statusFilter = '';
currentPage = 1;
pageSize = 5;

  constructor(private adminService: AdminService) {}

  

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

  editUser(user: AdminUser) {

    console.log("Edit user", user);

  }

  toggleUser(user: AdminUser) {

    user.isActive = !user.isActive;

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
    this.loadUsers();
  }

  loadUsers() {
     this.adminService.getDashboard().subscribe(data => {

      this.users = data.users;
   });

  }
}
