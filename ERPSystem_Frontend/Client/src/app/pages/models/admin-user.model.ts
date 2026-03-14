export interface AdminUser {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  isActive: boolean;
  createdAt: Date;
   roles: string[];
}

export interface AdminDashboard {

  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  adminUsers: number;

  latestUsers: AdminUser[];

  users: AdminUser[];
}