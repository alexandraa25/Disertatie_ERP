import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-confirm-login-code',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './confirm-login-code.component.html',
  styleUrl: './confirm-login-code.component.css'
})
export class ConfirmLoginCodeComponent implements OnInit {

  codeForm!: FormGroup;
  isLoading = false;

  codeControls = ['c1', 'c2', 'c3', 'c4', 'c5', 'c6'];

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private snackbar: SnackbarService
  ) { }

  ngOnInit(): void {
    this.codeForm = this.fb.group({
      c1: ['', Validators.required],
      c2: ['', Validators.required],
      c3: ['', Validators.required],
      c4: ['', Validators.required],
      c5: ['', Validators.required],
      c6: ['', Validators.required],
    });
  }

  moveToNext(event: any, index: number) {
    if (event.target.value.length === 1) {
      const next = document.querySelectorAll('.code-box')[index + 1] as HTMLElement;
      if (next) next.focus();
    }
  }

  moveToPrev(event: any, index: number) {
    if (event.key === 'Backspace' && !event.target.value) {
      const prev = document.querySelectorAll('.code-box')[index - 1] as HTMLElement;
      if (prev) prev.focus();
    }
  }

  onSubmit() {
    if (this.codeForm.invalid) {
      this.snackbar.showError('Please enter the 6-digit code.', 1500);
      return;
    }

    const code =
      this.codeForm.value.c1 +
      this.codeForm.value.c2 +
      this.codeForm.value.c3 +
      this.codeForm.value.c4 +
      this.codeForm.value.c5 +
      this.codeForm.value.c6;

    this.verify(code);
  }

  verify(code: string) {
    this.isLoading = true;

    const tempToken = sessionStorage.getItem("tempToken");

    if (!tempToken) {
      this.snackbar.showError("Session expired. Please login again.", 2000);
      this.isLoading = false;
      this.router.navigate(['/login']);
      return;
    }

    this.auth.verifyLoginCode(code, tempToken).subscribe({
      next: (res) => {
        this.isLoading = false;
        const accessToken = res.value?.accessToken;
        const user = res.value?.user;

        if (!accessToken) {
          this.snackbar.showError("Unexpected error. No token returned.", 2000);
          return;
        }

        localStorage.setItem("accessToken", accessToken);
        localStorage.setItem("user", JSON.stringify(user));
        sessionStorage.removeItem("tempToken");
        this.snackbar.showSuccess("Login confirmed!", 1500);
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.isLoading = false;
        this.snackbar.showError("Invalid or expired code.", 2000);
      }
    });
  }

  resend() {
    const tempToken = sessionStorage.getItem("tempToken");

    if (!tempToken) {
      this.snackbar.showError("Session expired. Please login again.", 2000);
      this.router.navigate(['/login']);
      return;
    }

    this.auth.resendVerificationCode(tempToken).subscribe({
      next: () => this.snackbar.showSuccess("New code sent!", 1500),
      error: () => this.snackbar.showError("Failed to resend code.", 1500)
    });
  }

  handlePaste(event: ClipboardEvent) {
    event.preventDefault();

    const pasted = event.clipboardData?.getData('text') || '';
    const digits = pasted.replace(/\D/g, '').split('');

    digits.forEach((d, index) => {
      if (this.codeControls[index]) {
        this.codeForm.get(this.codeControls[index])?.setValue(d);
      }
    });
  }
}
