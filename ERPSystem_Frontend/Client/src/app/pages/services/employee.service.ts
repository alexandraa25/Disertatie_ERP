import { Injectable } from '@angular/core'
import { HttpClient } from '@angular/common/http'
import { Observable } from 'rxjs'
import { Employee, HrDashboard, SimpleUser } from '../models/employee.model'
import { PublicResponse } from '../../app.model'

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {

  private baseUrl = 'https://localhost:7195/employee'

  constructor(private http: HttpClient) { }

  getEmployees(): Observable<PublicResponse<Employee[]>> {
  return this.http.get<PublicResponse<Employee[]>>(`${this.baseUrl}/list`);
}

getEmployeeById(id: string) {
  return this.http.get<any>(`${this.baseUrl}/${id}`);
}

  createEmployee(data: any): Observable<PublicResponse<any>> {
  return this.http.post<PublicResponse<any>>(this.baseUrl, data);
}

  uploadDocuments(employeeId: string, files: File[]) {
    const formData = new FormData();

    files.forEach(file => {
      formData.append('files', file);
    });

    return this.http.post(`${this.baseUrl}/${employeeId}/documents`, formData);
  }

  terminateEmployee(id: string, body: any) {
    return this.http.post(`${this.baseUrl}/${id}/terminate`, body)
  }

  getDashboard(): Observable<PublicResponse<HrDashboard>> {
  return this.http.get<PublicResponse<HrDashboard>>(`${this.baseUrl}/dashboard`);
}

  getUsers(): Observable<PublicResponse<SimpleUser[]>> {
  return this.http.get<PublicResponse<SimpleUser[]>>(`${this.baseUrl}/users`);
}



}