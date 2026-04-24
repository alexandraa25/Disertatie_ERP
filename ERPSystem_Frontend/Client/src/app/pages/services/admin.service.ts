import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminDashboard, AdminUser} from '../models/admin-user.model';
import { UserProfileDto } from '../models/user-profile.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  private baseUrl = 'https://localhost:7195/admin';

  constructor(private http: HttpClient) {}

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.baseUrl}/users`);
    
  }

  getEmployeesWithoutUser() {
  return this.http.get<any[]>(`${this.baseUrl}/employees-without-user`);
}

  getDashboard(): Observable<AdminDashboard> {
  return this.http.get<AdminDashboard>(`${this.baseUrl}/users`);
}

getProfileByUserId(userId: string) {
  return this.http.get<any>(`${this.baseUrl}/user-details/${userId}`);
}

toggleUserStatus(userId: string): Observable<any> {
  return this.http.put<any>(`${this.baseUrl}/users/${userId}/toggle-status`, {});
}

updateUserRoles(userId: string, roles: string[]) {
  return this.http.put<any>(`${this.baseUrl}/users/update-roles`, {userId, roles});
}

confirmUserEmail(userId: string) {
  return this.http.put<any>( `${this.baseUrl}/users/${userId}/confirm-email`, {} );
}

getUserActivityLog(userId: string) {
  return this.http.get<any>(`${this.baseUrl}/users/${userId}/activity-log`);
}
}