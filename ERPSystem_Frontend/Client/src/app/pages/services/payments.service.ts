import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PaymentsService {

    private baseUrl = `${environment.apiBaseUrl}`;

    constructor(private http: HttpClient) { }

    payInstallment(dto: any) {
        return this.http.post(`${this.baseUrl}/installments/pay`, dto);
    }

    getInstallments(contractId: number) {
        return this.http.get(`${this.baseUrl}/contracts/${contractId}/installments`);
    }

    getPayments(contractId: number) {
        return this.http.get(`${this.baseUrl}/contracts/${contractId}/payments`);
    }
}