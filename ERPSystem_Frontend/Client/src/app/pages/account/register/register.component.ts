import { Component, OnInit, HostListener, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ViewChild } from '@angular/core';


@Component({
  selector: 'app-register',
  standalone: true,
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
  imports: [CommonModule, ReactiveFormsModule, MatSnackBarModule]

})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  showPassword = false;
  isLoading = false;
  errorAnim = false;
  roles: any[] = [];


  jobTitleFromQuery: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private customSnackBarService: SnackbarService
  ) { }


  ngOnInit(): void {
    
    this.registerForm = this.fb.group({
      username: ['', Validators.required],
      emailAddress: ['', [Validators.required, Validators.email]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      phoneNumber: [''],
      role: ['', Validators.required],
      isActive: [true],

    });

    this.route.queryParams.subscribe(params => {

  if (params['email']) {

    this.registerForm.patchValue({
      firstName: params['firstName'],
      lastName: params['lastName'],
      emailAddress: params['email'],
      username: params['email'],
      phoneNumber: params['phoneNumber'] // 🔥 TELEFONUL
    });
     this.jobTitleFromQuery = params['jobTitle'];

  }
  });
   this.loadRoles();

  }

 loadRoles() {
  this.authService.getRoles().subscribe({
    next: (data) => {
      this.roles = data;
    },
    error: () => {
      this.customSnackBarService.showError(
        'Rolurile nu au putut fi încărcate.',
        2500
      );
    }
  });
}

 
  generateRandomPassword(length: number = 12): string {
    const upper = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const lower = 'abcdefghijklmnopqrstuvwxyz';
    const numbers = '0123456789';
    const special = '@$!%*?&';
    const all = upper + lower + numbers + special;

    let password =
      upper[Math.floor(Math.random() * upper.length)] +
      lower[Math.floor(Math.random() * lower.length)] +
      numbers[Math.floor(Math.random() * numbers.length)] +
      special[Math.floor(Math.random() * special.length)];

    for (let i = 4; i < length; i++) {
      password += all[Math.floor(Math.random() * all.length)];
    }

    return password
      .split('')
      .sort(() => 0.5 - Math.random())
      .join('');
  }

 onSubmit(): void {
  if (this.registerForm.invalid) {
    this.registerForm.markAllAsTouched();

    this.customSnackBarService.showError(
      'Completează câmpurile obligatorii.',
      2200
    );

    return;
  }

  this.processRegistration();
}

 processRegistration(): void {
  this.isLoading = true;
  const randomPassword = this.generateRandomPassword();

  const userData = {
    username: this.registerForm.value.username,
    email: this.registerForm.value.emailAddress,
    password: randomPassword,
    firstName: this.registerForm.value.firstName,
    lastName: this.registerForm.value.lastName,
    phoneNumber: this.registerForm.value.phoneNumber,
    role: this.registerForm.value.role,
    isActive: this.registerForm.value.isActive,
    employeeId: this.route.snapshot.queryParams['employeeId']
  };

  this.authService.register(userData).subscribe({
    next: (res: any) => {
      this.isLoading = false;

      if (res?.isSuccess === false) {
        this.customSnackBarService.showError(
          res.error?.errorMessage || 'Utilizatorul nu a putut fi creat.',
          2500
        );
        return;
      }

      this.customSnackBarService.showSuccess(
        'Utilizator creat cu succes.',
        1800
      );

    },
    error: () => {
      this.isLoading = false;

      this.customSnackBarService.showError(
        'Utilizatorul nu a putut fi creat.',
        2500
      );
    }
  });
}

  onModalConfirmed(event: boolean) {
    this.registerForm.reset();
    this.router.navigate(['/login']);
  }

 

}