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

createStudentEvaluation(dto: any) {
  return this.http.post(`${this.baseUrl}/student-evaluations`, dto);
}

getStudentEvaluations(studentId: number, sessionId?: number) {
  const params: any = {};

  if (sessionId) {
    params.sessionId = sessionId;
  }

  return this.http.get(`${this.baseUrl}/student-evaluations/${studentId}`, { params });
}


getCourseAnalytics(courseSessionId: number) {
  return this.http.get(`${this.baseUrl}/course-analytics/${courseSessionId}`);
}

getStudentAnalytics(studentId: number) {
  return this.http.get(`${this.baseUrl}/student-analytics/${studentId}`);
} 

createExternalReview(dto: any) {
  return this.http.post(`${this.baseUrl}/external-review`, dto);
}

getExternalReviews(targetType?: string, targetId?: string, source?: string) {
  const params: any = {};

  if (targetType) params.targetType = targetType;
  if (targetId) params.targetId = targetId;
  if (source) params.source = source;

  return this.http.get(`${this.baseUrl}/external-reviews`, { params });
}

getExternalAnalytics(targetType?: string, targetId?: string, source?: string) {
  const params: any = {};

  if (targetType) params.targetType = targetType;
  if (targetId) params.targetId = targetId;
  if (source) params.source = source;

  return this.http.get(`${this.baseUrl}/external-analytics`, { params });
}

getExternalReviewTargets(targetType: string) {
  return this.http.get(`${this.baseUrl}/external-review-targets/${targetType}`);
}
}