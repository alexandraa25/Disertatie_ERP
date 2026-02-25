import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserProfileDto, MeDto } from '../models/user-profile.model';

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {

  private baseUrl = 'https://localhost:7195/me'; 
  // ⚠️ dacă backend rulează pe alt port modifici

  constructor(private http: HttpClient) {}

  getMe(): Observable<MeDto> {
    return this.http.get<MeDto>(this.baseUrl, { withCredentials: true });
  }

  getProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.baseUrl}/profile`, { withCredentials: true });
  }

  updateProfile(profile: UserProfileDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/profile`, profile, { withCredentials: true });
  }
}
