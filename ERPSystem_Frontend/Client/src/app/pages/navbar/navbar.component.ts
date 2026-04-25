import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { jwtDecode } from 'jwt-decode';
import { NotificationService } from '../services/notification.service';
import { NotificationDto } from '../models/user-profile.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  
  user: any;
  cartCount = 0;

  isMenuOpen: boolean = false; 

   notifications: NotificationDto[] = [];
  unreadCount = 0;
  notificationsOpen = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.authService.user$.subscribe(u => this.user = u);
    this.loadNotifications();
    this.loadUnreadCount();
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  closeMenu() {
    this.isMenuOpen = false;
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  goToAnnouncements() {
    this.router.navigate(['/announcement-list']); 
  }

  goToProfile() {
    const token = localStorage.getItem('accessToken');

    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    try {
      const decoded: any = jwtDecode(token);
      const role = decoded["role"];

      if (role === 'User') {
        this.router.navigate(['/profil-user']);
      } else {
        this.router.navigate(['/admin-dashboard']);
      }
    } catch (error) {
      console.error('Invalid token:', error);
      this.router.navigate(['/login']);
    }
  }


toggleNotifications(): void {
  this.notificationsOpen = !this.notificationsOpen;

  if (this.notificationsOpen) {
    this.loadNotifications();

    this.notificationService.markAllAsSeen().subscribe();
  }
}

openNotification(notification: NotificationDto): void {
  this.markAsRead(notification);

  if (notification.link) {
    this.notificationsOpen = false;
    this.router.navigateByUrl(notification.link);
  }
}

getNotificationIcon(type: string): string {
  switch (type) {
    case 'Success':
      return '✅';
    case 'Warning':
      return '⚠️';
    case 'Error':
      return '⛔';
    default:
      return 'ℹ️';
  }
}

  loadNotifications(): void {
    this.notificationService.getMyNotifications().subscribe({
      next: (res) => {
        this.notifications = res;
      }
    });
  }

  loadUnreadCount(): void {
    this.notificationService.getUnreadCount().subscribe({
      next: (count) => {
        this.unreadCount = count;
      }
    });
  }

 markAsRead(notification: NotificationDto): void {
  if (notification.isRead) return;

  this.notificationService.markAsRead(notification.id).subscribe({
    next: () => {
      notification.isRead = true;
      notification.readAt = new Date().toISOString();
      this.unreadCount = Math.max(0, this.unreadCount - 1);
    }
  });
}

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications = this.notifications.map(n => ({
          ...n,
          isRead: true
        }));

        this.unreadCount = 0;
      }
    });
  }

  getTimeAgo(date: string): string {
    const created = new Date(date).getTime();
    const now = new Date().getTime();
    const diffMinutes = Math.floor((now - created) / 60000);

    if (diffMinutes < 1) return 'acum';
    if (diffMinutes < 60) return `acum ${diffMinutes} min`;

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) return `acum ${diffHours} ore`;

    const diffDays = Math.floor(diffHours / 24);
    return `acum ${diffDays} zile`;
  }
}
