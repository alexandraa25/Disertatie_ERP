import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserProfileDto, NotificationSettingDto, ChangePasswordRequest, UpdateUserProfileDto } from '../models/user-profile.model';

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {

  private baseUrl = 'https://localhost:7195/me';

  constructor(private http: HttpClient) { }


  getProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.baseUrl}/profile`);
  }
  updateProfile(profile: UpdateUserProfileDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/profile`, profile, { withCredentials: true });
  }


  getNotificationSettings() {
    return this.http.get<NotificationSettingDto[]>(
      `${this.baseUrl}/notification-settings`,
    );
  }

  updateNotificationSettings(payload: NotificationSettingDto[]) {
    return this.http.put<void>(
      `${this.baseUrl}/notification-settings`,
      payload
    );
  }

  changePassword(data: ChangePasswordRequest) {
    return this.http.post(`https://localhost:7195/auth/change-password`, data)
  }
}
