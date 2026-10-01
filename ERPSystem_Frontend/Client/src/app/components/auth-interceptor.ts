import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const apiBase = environment.apiBaseUrl.replace(/\/$/, '');
  const isApiRequest = req.url === apiBase || req.url.startsWith(`${apiBase}/`);
  if (!isApiRequest) return next(req);

  const token = localStorage.getItem('accessToken');

  if (token && token !== 'undefined' && token !== 'null') {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
        router.navigate(['/login']);
      }
      return throwError(() => err);
    })
  );
};
