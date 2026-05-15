import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {

  private baseUrl = 'https://localhost:7195/dashboard'; // 👈 ajustează dacă ai alt prefix

  constructor(private http: HttpClient) {}

  getOverview(): Observable<any> {
    return this.http.get(`${this.baseUrl}/overview`, {
      withCredentials: false
    });
  }

  getFinancial(): Observable<any> {
    return this.http.get(`${this.baseUrl}/financial`, { withCredentials: true });
  }

  getEducation(): Observable<any> {
    return this.http.get(`${this.baseUrl}/education`, { withCredentials: true });
  }

  getHr(): Observable<any> {
    return this.http.get(`${this.baseUrl}/hr`, { withCredentials: true });
  }

}