import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {

  private baseUrl = `${environment.apiBaseUrl}/company`;

  constructor(private http: HttpClient) {}

  get(): Observable<any> {
    return this.http.get(this.baseUrl);
  }

  save(data: any): Observable<any> {
    return this.http.post(this.baseUrl, data);
  }

}