import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CreateContractDto } from '../models/contract.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ContractsService {

  private baseUrl = 'https://localhost:7195/contracts';

  constructor(private http: HttpClient) { }

  create(dto: CreateContractDto): Observable<any> {
    return this.http.post(this.baseUrl, dto);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }


  listByStudent(studentId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/students/${studentId}/contracts`);
  }


  updateBody(id: number, dto: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/body`, dto);
  }


  finalize(id: number) {
    return this.http.put<any>(`${this.baseUrl}/${id}/finalize`, {});
  }

  send(id: number) {
    return this.http.post(`${this.baseUrl}/${id}/send`, {});
  }

  getForSigning(token: string) {
    return this.http.get(`${this.baseUrl}/sign/${token}`);
  }


  signClient(dto: any) {
    return this.http.post(`${this.baseUrl}/client-sign`, dto);
  }

  sign(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/sign`, {});
  }


  activate(id: number) {
    return this.http.put<any>(`${this.baseUrl}/contracts/${id}/activate`, {});
  }



  cancel(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/cancel`, {});
  }


  generatePdf(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/generate-pdf`, {});
  }

  getLatestByStudent(studentId: number): Observable<any> {
    return this.http.get<any>(
      `${this.baseUrl}/latest-by-student/${studentId}`
    );
  }

  adminSign(id: number, signature: string) {

    return this.http.post(`${this.baseUrl}/contracts/${id}/admin-sign`, {
      signature: signature
    });

  }

  download(id: number) {
    return this.http.get(`${this.baseUrl}/contracts/${id}/download`, {
      responseType: 'blob'
    });
  }

}