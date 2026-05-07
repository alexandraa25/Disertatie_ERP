import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-confirm-email-registration',
  standalone: true,
  templateUrl: './confirm-email-registration.component.html',
  styleUrl: './confirm-email-registration.component.css'
})
export class ConfirmEmailRegistrationComponent implements OnInit {

  constructor(
    private authService: AuthService,
    private router: Router,
    private snackbar: SnackbarService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {

    this.route.queryParams.subscribe(params => {

      const userId = params['userId'];
      const token = params['token'];

      console.log('userId:', userId);
      console.log('token:', token);

      if (userId && token) {
        this.confirmEmail(userId, token);
      } else {
        this.snackbar.showError(
          'Linkul de confirmare este invalid.',
          2500
        );
      }
    });
  }

  confirmEmail(userId: string, token: string) {

    this.authService.confirmMail(userId, token).subscribe({

      next: () => {

        this.snackbar.showSuccess(
          'Email confirmat cu succes!',
          2000
        );

        this.router.navigate(['/login']);
      },

      error: (err) => {

        console.error(err);

        const msg =
          err?.error?.message ||
          'Confirmarea emailului a eșuat. Linkul este invalid sau expirat.';

        this.snackbar.showError(msg, 3000);
      }
    });
  }
}