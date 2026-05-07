import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class MarketingCampaignService {

  private baseUrl = 'https://localhost:7195/mk-campaign';

  constructor(private http: HttpClient) { }

  getAll(filters: any) {
    let params: any = {};

    Object.keys(filters).forEach(key => {
      const value = filters[key];

      if (value !== null && value !== undefined && value !== '') {
        params[key] = value;
      }
    });

    return this.http.get<any>(this.baseUrl, { params });
  }

  create(dto: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/create`, dto);
  }

  update(id: number, dto: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/update`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}/delete`);
  }

  toggleActive(id: number, endDate: string | null) {
    return this.http.put<any>(`${this.baseUrl}/${id}/toggle-active`, { endDate });
  }

  getAvailableCampaigns(courseSessionIds: number[]) {
    return this.http.post<any>(`${this.baseUrl}/available`, { courseSessionIds });
  }

  sendNewsletter(dto: any) {
    return this.http.post(`${this.baseUrl}/send-newsletter`, dto);
  }

  getNewsletterTemplate(campaignId: number) {
    return this.http.get(`${this.baseUrl}/newsletter-template`, {
      params: { campaignId }
    });
  }

  getEmailLogs(dto: any) {
    return this.http.post(`${this.baseUrl}/email-logs`, dto);
  }

  getEmailLogDetails(id: number) {
    return this.http.get(`${this.baseUrl}/email-logs/${id}`);
  }
} 