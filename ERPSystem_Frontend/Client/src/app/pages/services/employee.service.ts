import { Injectable } from '@angular/core'
import { HttpClient } from '@angular/common/http'
import { Observable } from 'rxjs'
import { Employee, HrDashboard } from '../models/employee.model'

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {

  private api = 'https://localhost:7195'

  constructor(private http: HttpClient) {}

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${this.api}/employees`)
  }

  createEmployee(body: any) {
    return this.http.post(`${this.api}/employee`, body)
  }

  terminateEmployee(id: string, body: any) {
    return this.http.post(`${this.api}/employee/${id}/terminate`, body)
  }

  getDashboard(): Observable<HrDashboard> {
    return this.http.get<HrDashboard>(`${this.api}/employees/dashboard`)
  }

}