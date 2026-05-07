import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css'],
  imports: [CommonModule, ReactiveFormsModule, MatSnackBarModule]
})
export class ForgotPasswordComponent implements OnInit {

  forgotForm!: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  goBack() {
    this.router.navigate(['/login']);
  }

  onSubmit() {
    if (this.forgotForm.invalid) {
      this.snackbar.showError('Te rugăm să introduci un email valid.', 1500);
      return;
    }

    this.sendResetLink();
  }

  sendResetLink() {
    this.isLoading = true;

    const email = this.forgotForm.value.email;

    this.auth.requestPasswordReset(email).subscribe({
      next: (res) => {

        if (res.isSuccess === false) {
          this.isLoading = false;

          this.snackbar.showError(
            res.message || 'Linkul de resetare nu a putut fi trimis.',
            2000
          );

          return;
        }

        this.isLoading = false;

        this.snackbar.showSuccess(
          'Linkul de resetare a fost trimis pe email.',
          2000
        );

        this.router.navigate(['/login']);
      },

      error: () => {
        this.isLoading = false;

        this.snackbar.showError(
          'A apărut o eroare la trimiterea linkului de resetare.',
          2000
        );
      }
    });
  }
}