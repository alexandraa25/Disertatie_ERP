import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminDashboard, AdminUser} from '../models/admin-user.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  private apiUrl = 'https://localhost:7195/admin';

  constructor(private http: HttpClient) {}

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.apiUrl}/users`);
    
  }

  getEmployeesWithoutUser() {
  return this.http.get<any[]>(`${this.apiUrl}/employees-without-user`);
}

  getDashboard(): Observable<AdminDashboard> {
  return this.http.get<AdminDashboard>(`${this.apiUrl}/users`);
}

}