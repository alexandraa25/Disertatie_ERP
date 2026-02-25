import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [CommonModule, ReactiveFormsModule, MatSnackBarModule]
})
export class LoginComponent implements OnInit {

  loginForm!: FormGroup;
  showPassword = false;
  isLoading = false;

  popupTitle: string = '';
  popupMessage: string = '';
  popupIsError: boolean = false;
  popupVisible: boolean = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private snackbar: SnackbarService
  ) { }

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  goToRegister() {
    this.router.navigate(['/register']);
  }

  goToForgot() {
    this.router.navigate(['/forgot-password']);
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.snackbar.showError('Invalid form. Please check your fields.',3500);
      return;
    }
    this.processLogin();
  }

  processLogin() {
    this.isLoading = true;

    this.auth.login(this.loginForm.value.email, this.loginForm.value.password).subscribe({
      next: (res) => {

        if(res.isSuccess === false){
          this.isLoading = false;
           this.snackbar.showError('Username or password is incorrect. Please try again', 3500);
           return;
        }
        sessionStorage.setItem("tempToken", res.value.tempToken);
        this.isLoading = false;
       // this.router.navigate(['/confirm-login-code']);
       this.router.navigate(['/profil-user']);
      },
      error: () => {
        this.isLoading = false;
        this.snackbar.showError('Login failed. Incorrect credentials.', 2000);
      }
    });
  }
}
