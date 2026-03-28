import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CreateContractDto } from '../models/contract.model';
import { Observable } from 'rxjs';
import { AdditionalActListDto, CreateAdditionalActDto } from '../models/additional-act.model';

@Injectable({ providedIn: 'root' })
export class AdditionalActService {

  private baseUrl = 'https://localhost:7195/additional-act';

  constructor(private http: HttpClient) { }

  create(contractId: number, dto: CreateAdditionalActDto) {
    return this.http.post(`${this.baseUrl}/contract/${contractId}/create`, dto);
  }

  getById(id: number) {
    return this.http.get(`${this.baseUrl}/${id}`);
  }

  getByContract(contractId: number) {
    return this.http.get<AdditionalActListDto[]>(`${this.baseUrl}/contract/${contractId}`);
  }

  update(id: number, dto: any) {
    return this.http.put(`${this.baseUrl}/${id}/update`, dto);
  }

  updateBody(id: number, body: string) {
    return this.http.put(`${this.baseUrl}/${id}/body`, { body: body});
  }

  sendToClient(id: number) {
  return this.http.post(`${this.baseUrl}/${id}/send`, {});
}

finalize(id: number) {
  return this.http.post(`${this.baseUrl}/${id}/finalize`, {});
}

adminSignAct(id: number, signature: string) {
  return this.http.post(`${this.baseUrl}/${id}/admin-sign`, {signature });
}

downloadAct(id: number) {
  return this.http.get(`${this.baseUrl}/${id}/download`, {
    responseType: 'blob'
  });
}

saveFile(blob: Blob, fileName: string) {
  const url = window.URL.createObjectURL(blob);

  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();

  window.URL.revokeObjectURL(url);
}
}