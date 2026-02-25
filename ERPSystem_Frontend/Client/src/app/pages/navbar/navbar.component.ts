import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { jwtDecode } from 'jwt-decode';

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

  isMenuOpen: boolean = false; // <<<<<< AICI TREBUIE SĂ EXISTE

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.authService.user$.subscribe(u => this.user = u);
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
}
