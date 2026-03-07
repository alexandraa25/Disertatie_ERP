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

  constructor(private http: HttpClient) {}

  list(q = ''): Observable<CourseListItemDto[]> {
    const params = new URLSearchParams({ q }).toString();
    return this.http.get<CourseListItemDto[]>(`${this.baseUrl}?${params}`);
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

  // ========================
  // ENROLLMENTS
  // ========================

  listEnrollments(courseId: number): Observable<EnrollmentDto[]> {
    return this.http.get<EnrollmentDto[]>(
      `${this.baseUrl}/${courseId}/enrollments`
    );
  }

 enroll(
  courseId: number,
  body: CourseEnrollRequest
): Observable<void> {
  return this.http.post<void>(
    `${this.baseUrl}/${courseId}/enrollments`,
    body
  );
}

  setEnrollmentActive(
    courseId: number,
    sessionId: number,
    studentId: number,
    isActive: boolean
  ): Observable<void> {
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

}

