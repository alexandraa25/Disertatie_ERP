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

  userRoles: string[] = [];

  menuGroups = [
  {
    label: 'Academie',
    children: [
      { label: 'Cursanți', route: '/students', roles: ['Admin', 'Manager', 'Secretary', 'Teacher'] },
      { label: 'Cursuri', route: '/courses', roles: ['Admin', 'Manager', 'Secretary', 'Teacher'] }, 
    ]
  },
  
  {
    label: 'HR',
    children: [
      { label: 'Angajați', route: '/employees', roles: ['Admin', 'HR', 'Manager'] },
      { label: 'Concedii', route: '/all-leaves', roles: ['Admin', 'HR', 'Manager'] }
    ]
  },
  {
    label: 'Administrare',
    children: [
      { label: 'Utilizatori', route: '/admin/users', roles: ['Admin', 'Manager'] },
      { label: 'Înregistrare utilizator', route: '/register', roles: ['Admin'] },
      { label: 'Companie', route: '/company', roles: ['Admin', 'Manager', 'Secretary'] },
      { label: 'Activitate', route: '/log-activity', roles: ['Admin'] },
      { label: 'Contracte cursanti', route: '/all-contracts', roles: ['Admin', 'Manager', 'Accountant', 'Secretary'] }
    ]
  },
  {
    label: 'Marketing',
    children: [
      { label: 'Campanii', route: '/mk-campaign', roles: ['Admin', 'Manager', 'Marketing'] }
    ]
  },
  {
    label: 'Feedback',
    children: [
      { label: 'Feedback extern', route: '/external-feedback', roles: ['Admin', 'Manager', 'Marketing'] },
      { label: 'Analiză globală', route: '/feedback/analytics/global', roles: ['Admin', 'Manager', 'Marketing'] }
    ]
  }, 
  {
    label: 'Statistici',
    children: [
      { label: 'Analiză globală', route: '/feedback/analytics/global', roles: ['Admin', 'Manager', 'Marketing'] },
      { label: 'Dashboard general', route: '/overview-dashboard', roles: ['Admin', 'Manager'] },
      { label: 'Dashboard educațional', route: '/education-dashboard', roles: ['Admin', 'Manager'] }, 
      { label: 'Dashboard financiar', route: '/financial-dashboard', roles: ['Admin', 'Manager'] }, 
      { label: 'Dashboard HR', route: '/hr-dashboard', roles: ['Admin', 'Manager', 'HR'] }
    ]
  }
];

  constructor(
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.authService.user$.subscribe(u => {
      this.user = u;
      this.setUserRoles(u);
    });

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
  this.authService.logout().subscribe({
    next: () => this.router.navigate(['/login']),
    error: () => this.router.navigate(['/login'])
  });
}

  canSeeGroup(group: any): boolean {
    return group.children.some((item: any) => this.canSee(item));
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some(role => this.userRoles.includes(role));
  }

  canSee(item: any): boolean {
    return item.roles.some((role: string) => this.userRoles.includes(role));
  }

  activeDropdown: string | null = null;
  private dropdownCloseTimer: any;

  openDropdown(label: string): void {
    clearTimeout(this.dropdownCloseTimer);
    this.activeDropdown = label;
  }

  closeDropdownWithDelay(): void {
    this.dropdownCloseTimer = setTimeout(() => {
      this.activeDropdown = null;
    }, 250);
  }

  private setUserRoles(user: any): void {
    const rolesFromUser =
      user?.roles ??
      user?.Roles ??
      user?.role ??
      user?.Role;

    if (rolesFromUser) {
      this.userRoles = Array.isArray(rolesFromUser)
        ? rolesFromUser
        : [rolesFromUser];

      return;
    }

    const token = localStorage.getItem('accessToken');

    if (!token) {
      this.userRoles = [];
      return;
    }

    const decoded: any = jwtDecode(token);

    const tokenRoles =
      decoded['role'] ??
      decoded['roles'] ??
      decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    this.userRoles = Array.isArray(tokenRoles)
      ? tokenRoles
      : tokenRoles
        ? [tokenRoles]
        : [];

    console.log('DECODED TOKEN:', decoded);
    console.log('ROLES:', this.userRoles);
  }
  goToProfile() {
    const token = localStorage.getItem('accessToken');

    if (!token) {
      this.router.navigate(['/profil-user']);
      return;
    }

    try {
      const decoded: any = jwtDecode(token);
      const role = decoded["role"];

     
        this.router.navigate(['/profil-user']);
      
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
