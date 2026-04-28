import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CourseListItemDto,
  CourseDetailsDto,
  CreateCourseDto,
  UpdateCourseDto,
  TeacherOptionDto,
  EnrollmentDto,
  CourseEnrollRequest
} from '../models/course.model';

@Injectable({ providedIn: 'root' })
export class CoursesService {
  private baseUrl = 'https://localhost:7195/courses';

  constructor(private http: HttpClient) { }

  list(q?: string,
    status?: string,
    deleteStatus: string = 'notDeleted',
    scope?: number) {

    let params: any = {
      deleteStatus
    };

    if (q) params.q = q;
    if (status) params.status = status;
    if (scope !== undefined && scope !== null) params.scope = scope;

    return this.http.get<any>(`${this.baseUrl}`, { params });
  }

  get(id: number): Observable<CourseDetailsDto> {
    return this.http.get<CourseDetailsDto>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateCourseDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.baseUrl}`, dto);
  }

  update(id: number, dto: UpdateCourseDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  teachers(): Observable<TeacherOptionDto[]> {
    return this.http.get<TeacherOptionDto[]>(`${this.baseUrl}/teachers`);
  }

  listEnrollments(courseId: number, sessionId?: number): Observable<EnrollmentDto[]> {
    const params: any = {};

    if (sessionId) {
      params.sessionId = sessionId;
    }

    return this.http.get<EnrollmentDto[]>( `${this.baseUrl}/${courseId}/enrollments`,  { params } );
  }

  enroll(courseId: number, body: CourseEnrollRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${courseId}/enrollments`, body);
  }

  setEnrollmentActive(courseId: number, sessionId: number, studentId: number, isActive: boolean): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/${courseId}/enrollments/${sessionId}/${studentId}?isActive=${isActive}`,
      {}
    );
  }

  getAvailableStudents(courseId: number, sessionId: number, q?: string) {
    return this.http.get(
      `${this.baseUrl}/${courseId}/sessions/${sessionId}/available-students`,
      { params: { q: q || '' } }
    );
  }

  deleteCourse(id: number) {
    return this.http.delete<any>(`${this.baseUrl}/${id}/delete`);
  }

  restoreCourse(id: number) {
    return this.http.post<any>(`${this.baseUrl}/${id}/restore`, {});
  }
  toggleCourseStatus(id: number) {
    return this.http.post<any>(`${this.baseUrl}/${id}/toggle-status`, {});
  }

}

