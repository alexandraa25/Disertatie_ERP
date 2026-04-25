import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NotificationDto } from '../models/user-profile.model';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  private baseUrl = 'https://localhost:7195/notifications';

  constructor(private http: HttpClient) {}

  getMyNotifications() {
    return this.http.get<NotificationDto[]>(`${this.baseUrl}/`);
  }

  getUnreadCount() {
    return this.http.get<number>(`${this.baseUrl}/unread-count`);
  }

  markAsRead(id: number) {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/read`, {});
  }

  markAllAsRead() {
    return this.http.post<boolean>(`${this.baseUrl}/read-all`, {});
  }

  markAllAsSeen() {
  return this.http.post<boolean>( `${this.baseUrl}/seen-all`,  {} );
}
}