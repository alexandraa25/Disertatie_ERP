import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CreateContractDto } from '../models/contract.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ContractsService {

  private baseUrl = 'https://localhost:7195/contracts';

  constructor(private http: HttpClient) {}

  create(dto: CreateContractDto): Observable<any> {
    return this.http.post(this.baseUrl, dto);
  }
  // ============================
  // GET BY ID
  // ============================
  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  // ============================
  // LIST BY STUDENT
  // ============================
  listByStudent(studentId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/students/${studentId}/contracts`);
  }

  // ============================
  // UPDATE BODY
  // ============================
  updateBody(id: number, dto: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/body`, dto);
  }

  // ============================
  // FINALIZE
  // ============================
  finalize(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/finalize`, {});
  }

  // ============================
  // SIGN
  // ============================
  sign(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/sign`, {});
  }

  // ============================
  // ACTIVATE
  // ============================
  activate(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/contracts/${id}/activate`, {});
  }

  // ============================
  // CANCEL
  // ============================
  cancel(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/cancel`, {});
  }

  // ============================
  // GENERATE PDF
  // ============================
  generatePdf(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/generate-pdf`, {});
  }

}