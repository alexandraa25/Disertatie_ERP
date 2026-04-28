import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
   private baseUrl = 'https://localhost:7195/feedback';

  constructor(private http: HttpClient) {}

  sendFeedbackForms(dto: any) {
    return this.http.post(`${this.baseUrl}/send`, dto);
  }

  getFeedbackForm(token: string) {
    return this.http.get(`${this.baseUrl}/${token}`);
  }

  submitFeedback(dto: any) {
    return this.http.post(`${this.baseUrl}/submit`, dto);
  }

  getSessionReviews(sessionId: number) {
  return this.http.get(`${this.baseUrl}/sessions/${sessionId}/reviews`);
}
}