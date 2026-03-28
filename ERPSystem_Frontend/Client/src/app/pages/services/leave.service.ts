import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Leave } from '../models/leave.model';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {

  private baseUrl = 'https://localhost:7195/employee'

  constructor(private http: HttpClient) {}

  // 🔥 GET - concediile userului
  getMyLeaves(): Observable<Leave[]> {
  return this.http.get<Leave[]>(`${this.baseUrl}/my`);
}

  // 🔥 POST - cerere nouă
  createLeave(data: any): Observable<any> {
    return this.http.post(this.baseUrl, data);
  }

  // 🔥 OPTIONAL - admin approve
  approveLeave(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/approve`, {});
  }

  // 🔥 OPTIONAL - admin reject
  rejectLeave(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/reject`, {});
  }

}