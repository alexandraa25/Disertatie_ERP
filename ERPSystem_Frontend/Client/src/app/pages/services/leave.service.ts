import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Leave, LeavesResponse } from '../models/leave.model';
import { PublicResponse } from '../../app.model';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {

  private baseUrl = 'https://localhost:7195/leaves'

  constructor(private http: HttpClient) { }

  getMyLeaves(): Observable<PublicResponse<LeavesResponse>> {
    return this.http.get<PublicResponse<LeavesResponse>>(`${this.baseUrl}`);
  }

  createLeave(data: any) {
    return this.http.post<any>(`${this.baseUrl}/create`, data);
  }

  updateLeave(id: string, data: any) {
    return this.http.put(`${this.baseUrl}/${id}`, data);
  }

  cancelLeave(id: string) {
    return this.http.put(`${this.baseUrl}/${id}/cancel`, {});
  }

  approve(id: string) {
    return this.http.put(`${this.baseUrl}/${id}/approve`, {});
  }

  reject(id: string, reason: string) {
    return this.http.put(`${this.baseUrl}/${id}/reject?reason=${reason}`, {});
  }

  getHolidays(year: number) {
    return this.http.get<string[]>(`${this.baseUrl}/holidays?year=${year}`);
  }

  getAllLeaves(params: any) {
    return this.http.get(`${this.baseUrl}/all`, { params });
  }

   getConflicts(start: string, end: string): Observable<any> {
    const params = new HttpParams()
      .set('start', start)
      .set('end', end);

    return this.http.get<any>(`${this.baseUrl}/conflicts`, { params });
  }

  exportExcel() {
  return this.http.get(`${this.baseUrl}/export`, {
    responseType: 'blob'
  });
}

}