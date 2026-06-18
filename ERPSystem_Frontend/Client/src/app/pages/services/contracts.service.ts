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

  listContracts(studentId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/students/${studentId}`);
  }

  update(id: number, dto: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, dto);
  }

  updateBody(id: number, dto: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/body`, dto);
  }

  resetBody(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/reset-body`, {});
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

  // sign(id: number): Observable<any> {
  //   return this.http.put<any>(`${this.baseUrl}/${id}/sign`, {});
  // }

  activate(id: number) {
    return this.http.put<any>(`${this.baseUrl}/${id}/activate`, {});
  }

  cancel(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/cancel`, {});
  }


  generatePdf(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/generate-pdf`, {});
  }

  adminSign(id: number, signature: string) {

    return this.http.post(`${this.baseUrl}/${id}/admin-sign`, {
      signature: signature
    });
  }

  download(id: number) {
    return this.http.get(`${this.baseUrl}/${id}/download`, {
      responseType: 'blob'
    });
  }

  suspend(id: number) {
    return this.http.put(`${this.baseUrl}/${id}/suspend`, {});
  }

  resume(id: number) {
    return this.http.put(`${this.baseUrl}/${id}/resume`, {});
  }

  complete(id: number) {
    return this.http.put(`${this.baseUrl}/${id}/complete`, {});
  }

  deleteDraft(id: number): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}/delete`);
  }

  createAct(contractId: number, dto: any) {
    return this.http.post(`${this.baseUrl}/${contractId}/additional-act`, dto);
  }

  finalizeAct(id: number) {
    return this.http.put(`${this.baseUrl}/additional-acts/${id}/finalize`, {});
  }

  getActs(contractId: number) {
    return this.http.get(`${this.baseUrl}/${contractId}/additional-acts`);
  }

  getContractsOverview() {
    return this.http.get(`${this.baseUrl}/overview`);
  }

  exportContractsExcel(from?: string | null, to?: string | null) {
    const params: any = {};

    if (from) params.from = from;
    if (to) params.to = to;

    return this.http.get(`${this.baseUrl}/export`, {
      params,
      responseType: 'blob'
    });
  }


}