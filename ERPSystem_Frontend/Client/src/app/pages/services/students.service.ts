import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, StudentListItemDto, StudentDetailsDto, CreateStudentDto, UpdateStudentDto, StudentOption, GuardianOption } from '../models/student.model';
import { map } from 'rxjs/operators';


import { StudentCoursesResponse } from '../models/student.model';

@Injectable({ providedIn: 'root' })
export class StudentsService {
  private baseUrl = 'https://localhost:7195/students';

  constructor(private http: HttpClient) { }

  list(q = '', page = 1, pageSize = 20, sortBy = 'createdAt', sortDir = 'desc', onlyRecent = false, recentDays = 30) {
    const params = new URLSearchParams({
      q,
      page: String(page),
      pageSize: String(pageSize),
      sortBy,
      sortDir,
      onlyRecent: String(onlyRecent),
      recentDays: String(recentDays)
    }).toString();

    return this.http.get<PagedResult<StudentListItemDto>>(`${this.baseUrl}?${params}`);
  }
  exportExcel(q = '', sortBy = 'createdAt', sortDir = 'desc', onlyRecent = false, recentDays = 30) {
    const params = new URLSearchParams({
      q,
      sortBy,
      sortDir,
      onlyRecent: String(onlyRecent),
      recentDays: String(recentDays)
    }).toString();

    return this.http.get(`${this.baseUrl}/export?${params}`, { responseType: 'blob' });
  }

  get(id: number): Observable<StudentDetailsDto> {
    return this.http
      .get<any>(`${this.baseUrl}/${id}`)
      .pipe(map(res => res.value));
  }

  create(dto: CreateStudentDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.baseUrl}`, dto, { withCredentials: false });
  }

  update(id: number, dto: UpdateStudentDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto, { withCredentials: false });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, { withCredentials: false });
  }

  searchOptions(q: string) {
    return this.http.get<any[]>(
      `https://localhost:7195/students/options?q=${encodeURIComponent(q)}`
    );
  }

  getStudentCourses(id: number): Observable<StudentCoursesResponse> {
    return this.http
      .get<any>(`${this.baseUrl}/${id}/courses`)
      .pipe(map(res => res.value));
  }

  search(q: string = ''): Observable<StudentOption[]> {
    return this.http.get<StudentOption[]>(
      `${this.baseUrl}/options?q=${q}`
    );
  }

  getPrimaryGuardian(studentId: number) {
  return this.http
    .get<any>(`${this.baseUrl}/${studentId}/primary-guardian`)
    .pipe(map(res => res.value));
}

   getById(id: number) {
  return this.http.get<any>(`${this.baseUrl}/students/${id}`);
}
}
