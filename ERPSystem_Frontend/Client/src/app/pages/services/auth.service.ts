import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { UserDetailsModel } from '../models/user-details.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {

  private apiUrl = 'https://localhost:7195/auth';
  private currentUserSubject = new BehaviorSubject<any>(null);
  public user$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    if (!this.isAuthenticated()) {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
      return;
    }

    const savedUser = localStorage.getItem("user");

    try {
      if (savedUser && savedUser !== "undefined" && savedUser !== "null") {
        this.currentUserSubject.next(JSON.parse(savedUser));
      } else {
        this.loadUserFromToken();
      }
    } catch {
      localStorage.removeItem("user");
      this.loadUserFromToken();
    }
  }

  register(formData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, formData);
  }

  confirmMail(userId: string, token: string) {
    const url = `${this.apiUrl}/confirm-email-registration`;

    return this.http.post<any>(url, {
      userId: userId,
      token: token
    });
  }


  checkUserExistence(email: string, phoneNumber: string): Observable<any> {
    const url = `${this.apiUrl}/check-user-existence`;

    const params = new HttpParams()
      .set('email', email)
      .set('phoneNumber', phoneNumber);

    return this.http.get<any>(url, { params });
  }


login(email: string, password: string): Observable<any> {
  return this.http.post<any>(
    `${this.apiUrl}/login`,
    { email, password },
    { withCredentials: true }
  );
}

  refresh() {
    return this.http.post<{ accessToken: string }>(`${this.apiUrl}/refresh`, {}, { withCredentials: true })
      .pipe(
        tap(res => {
          localStorage.setItem("accessToken", res.accessToken);
          this.loadUserFromToken();
        })
      );
  }

  logout() {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('user');
  this.currentUserSubject.next(null);

  return this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true });
}

  isAuthenticated(): boolean {
    const token = localStorage.getItem('accessToken');
    if (!token || token === 'undefined' || token === 'null') return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp ? payload.exp * 1000 > Date.now() : false;
    } catch {
      return false;
    }
  }

  get userRole(): string | null {
    const token = localStorage.getItem('accessToken');
    if (!token || token.split('.').length !== 3) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.role || null;
    } catch {
      return null;
    }
  }

  getUserRoles(): string[] {
    const token = localStorage.getItem('accessToken');
    if (!token || token.split('.').length !== 3) return [];
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const roles =
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        payload.role ??
        payload.roles ??
        [];
      return Array.isArray(roles) ? roles : roles ? [roles] : [];
    } catch {
      return [];
    }
  }

  hasRole(roles: string[]): boolean {
    return this.getUserRoles().some(r => roles.includes(r));
  }

  getRedirectRouteForRole(): string {
    const roles = this.getUserRoles();
    if (roles.includes('Admin') || roles.includes('Manager')) return '/overview-dashboard';
    if (roles.includes('Secretary')) return '/students';
    if (roles.includes('Teacher')) return '/courses';
    if (roles.includes('HR')) return '/employees';
    if (roles.includes('Accountant')) return '/all-contracts';
    if (roles.includes('Marketing')) return '/mk-campaign';
    return '/profil-user';
  }
  setUser(user: any) {
    this.currentUserSubject.next(user);

    if (user === null || user === undefined) {
      localStorage.removeItem("user");
    } else {
      localStorage.setItem("user", JSON.stringify(user));
    }
  }

  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/users`);
  }

  deleteUser(id: number) {
    return this.http.delete(`${this.apiUrl}/users/${id}`);
  }
  activateUser(id: number) {
    return this.http.put(`${this.apiUrl}/${id}/activate`, {});
  }

  loadUserFromToken() {
    const token = localStorage.getItem("accessToken");

    if (!token || token === "undefined" || token === "null") {
      localStorage.removeItem("accessToken");
      this.currentUserSubject.next(null);
      return;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      localStorage.removeItem("accessToken");
      this.currentUserSubject.next(null);
      return;
    }

    try {
      const payload = JSON.parse(atob(parts[1]));
      this.currentUserSubject.next({
        id: payload.sub,
        role: payload.role,
        email: payload.email
      });

    } catch {
      localStorage.removeItem("accessToken");
      this.currentUserSubject.next(null);
    }
  }

  getUserDetails() {
    const token = localStorage.getItem('accessToken');
    return this.http.get<UserDetailsModel>("http://localhost:3000/get-user-details", {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  updateProfile(data: any) {
    const token = localStorage.getItem("accessToken");

    return this.http.put("http://localhost:3000/update-user", data, {
      headers: { Authorization: `Bearer ${token}` }
    });
  }

 verifyLoginCode(code: string, tempToken: string) {
  return this.http.post<any>(`${this.apiUrl}/confirm-login-code`, {
    tempToken,
    code
  }, { withCredentials: true }).pipe(
    tap((res) => {
      const data = res?.value ?? res?.result ?? res;
      const token = data?.accessToken ?? data?.AccessToken;
      const user = data?.user ?? data?.User;

      if (token) {
        localStorage.setItem('accessToken', token);
        this.setUser(user ?? null);
      }
    })
  );
}

  resendVerificationCode(tempToken: string) {
    return this.http.post<any>(`${this.apiUrl}/resend-login-code`, { tempToken });
  }

  requestPasswordReset(email: string) {
    return this.http.post<any>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(userId: string, token: string, newPassword: string) {
    return this.http.post<any>(`${this.apiUrl}/reset-password`, {
      userId, token, newPassword
    });
  }

  getRoles() {
    return this.http.get<any[]>(`${this.apiUrl}/roles`);
  }

}
