import { Injectable } from '@angular/core'
import { HttpClient, HttpParams } from '@angular/common/http'
import { Observable } from 'rxjs'
import { Employee, HrDashboard, SimpleUser } from '../models/employee.model'
import { PublicResponse } from '../../app.model'

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {

  private baseUrl = 'https://localhost:7195/employee'

  constructor(private http: HttpClient) { }

 
getEmployees(params?: any) {
  return this.http.get<any>(`${this.baseUrl}/list`, { params });
}

getEmployeeById(id: string) {
  return this.http.get<any>(`${this.baseUrl}/${id}`);
}

 createEmployee(formData: FormData) {
  return this.http.post<PublicResponse<any>>(this.baseUrl, formData);
}

updateEmployee(data: any) {
  return this.http.put<PublicResponse<any>>( `${this.baseUrl}/update`,  data );
}

uploadEmployeeDocuments(formData: FormData) {
  return this.http.post<any>( `${this.baseUrl}/upload-documents`, formData);
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

getEmployeeActivity(employeeId: string) {
  return this.http.get<any>(`${this.baseUrl}/activity/Employee/${employeeId}`);
}

viewDocument(documentId: string) {
  return this.http.get(
    `${this.baseUrl}/documents/${documentId}/view`,
    { responseType: 'blob' }
  );
}

downloadDocument(documentId: string) {
  return this.http.get(
    `${this.baseUrl}/documents/${documentId}/download`,
    { responseType: 'blob' }
  );
}

deleteDocument(documentId: string) {
  return this.http.delete<any>(
    `${this.baseUrl}/documents/${documentId}`
  );
}

// employees.service.ts

exportEmployeesExcel( q?: string,status?: string, contractType?: string) {
  let params = new HttpParams();

  if (q)
    params = params.set('q', q);

  if (status)
    params = params.set('status', status);

  if (contractType)
    params = params.set('contractType', contractType);

  return this.http.get(
    `${this.baseUrl}/export/excel`,
    {
      params,
      responseType: 'blob'
    }
  );
}

}