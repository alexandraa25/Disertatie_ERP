import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {

  constructor(private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const allowedRoles = route.data['roles'] as string[];

    const userJson = localStorage.getItem('user');
    const user = userJson ? JSON.parse(userJson) : null;

    const userRoles: string[] = user?.roles || [];

    const hasAccess = allowedRoles.some(role => userRoles.includes(role));

    if (!hasAccess) {
      this.router.navigate(['/dashboard']);
      return false;
    }

    return true;
  }
}