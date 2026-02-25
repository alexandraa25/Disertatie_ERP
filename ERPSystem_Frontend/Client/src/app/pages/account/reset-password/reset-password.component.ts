import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {

  resetForm!: FormGroup;
  userId!: string;
  token!: string;
  isLoading = false;
  showNewPassword = false;
  showConfirmPassword = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private auth: AuthService,
    private snackbar: SnackbarService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.queryParamMap.get("userId") || "";
    this.token = this.route.snapshot.queryParamMap.get("token") || "";

    if (!this.userId || !this.token) {
      this.snackbar.showError("Invalid password reset link.", 2000);
      this.router.navigate(['/login']);
      return;
    }

    this.resetForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(12)]],
      confirmPassword: ['', Validators.required]
    });
  }

  submit() {
    if (this.resetForm.invalid) {
      this.snackbar.showError("Complete all fields correctly.", 2000);
      return;
    }

    if (this.resetForm.value.newPassword !== this.resetForm.value.confirmPassword) {
      this.snackbar.showError("Passwords do not match.", 2000);
      return;
    }

    this.isLoading = true;

    this.auth.resetPassword(this.userId, this.token,this.resetForm.value.newPassword).subscribe({
      next: (res) => {
        if(res.isSucces === false){
          this.isLoading = false;
          this.snackbar.showError("Password reset failed.", 2000);
          return;
        }

        this.isLoading = false;
        this.snackbar.showSuccess("Password successfully reset!", 1500);
        this.router.navigate(['/login']);
      },
      error: () => {
        this.isLoading = false;
        this.snackbar.showError("Invalid or expired link.", 2000);
      }
    });
  }
  
toggleNewPassword() {
  this.showNewPassword = !this.showNewPassword;
}

toggleConfirmPassword() {
  this.showConfirmPassword = !this.showConfirmPassword;
}
}
