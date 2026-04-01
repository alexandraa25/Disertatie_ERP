import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { AppComponent } from './app/app.component';
import { provideAnimations } from '@angular/platform-browser/animations';
import { authInterceptor } from './app/components/auth-interceptor';
import { provideNativeDateAdapter } from '@angular/material/core';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),

    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor])
    ),

   
    provideAnimations(),
    provideNativeDateAdapter() 
  ]
}).catch(err => console.error(err));