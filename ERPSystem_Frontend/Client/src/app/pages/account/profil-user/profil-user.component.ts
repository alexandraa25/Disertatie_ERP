import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { UserProfileService } from '../../services/user-profile.service';
import { UserProfileDto, NotificationSettingDto, NotificationChannel, DigestMode } from '../../models/user-profile.model';
import { LeaveService } from '../../services/leave.service';

import { CreateLeaveModalComponent } from '../../hr/create-leave-modal/create-leave-modal.component';
import { EmployeeService } from '../../services/employee.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';


@Component({
  selector: 'app-profil-user',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, MatDialogModule],
  templateUrl: './profil-user.component.html',
  styleUrls: ['./profil-user.component.css'],
})
export class ProfilUserComponent implements OnInit {

  activeTab: 'password' | 'info' |'leaves'| 'notifications' = 'info';

  loading = true;
  saving = false;
  editMode = false;

  loadingNotifications = true;
  savingNotifications = false;

  NotificationChannel = NotificationChannel;
  DigestMode = DigestMode;

  form!: FormGroup;
  notificationForm!: FormGroup;
  passwordForm!: FormGroup;
  leaveForm!: FormGroup;

  originalData!: UserProfileDto;

  leaves: any[] = [];
  vacation: any;
  medical: any;

  holidays: string[] = [];
  openForm = false;

  address?: any;
  contact?: any;
  bank?: any;
  documents?: any[];

  notificationLabels: Record<string, string> = {
    FeedbackSubmitted: 'Feedback',
    FormSubmitted: 'Formulare interne trimise',
    ReportGenerated: 'Rapoarte generate',
    UserActivity: 'Activitate utilizatori',
    SystemUpdate: 'Modificări administrative'
  };


  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService,
    private leaveService: LeaveService,
    private employeeService: EmployeeService,
    private dialog: MatDialog, 
    private snackbar: SnackbarService
  ) { }

  ngOnInit(): void {

    this.form = this.fb.group({
      firstName: [''],
      lastName: [''],
      phoneNumber: [''],
      birthdayDate: [null],
      avatarUrl: [''],

      street: [''],
      city: [''],
      country: [''],
      postalCode: [''],

      emergencyContactName: [''],
      emergencyContactPhone: ['']
    });

    console.log('Token ' + localStorage.getItem('accessToken'));

    this.loadProfile();

    this.notificationForm = this.fb.group({
      settings: this.fb.array([])
    });

    this.loadNotifications();

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', Validators.required],
      confirmPassword: ['', Validators.required]
    })

    this.leaveForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: ['']
    });

    this.loadLeaves();
    this.loadHolidays();
  }

  private loadProfile() {
    this.userProfileService.getProfile()
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (res: any) => {
          const data: UserProfileDto = res.value ?? res;

          if (!data) return;

          this.form.patchValue({
            firstName: data.firstName,
            lastName: data.lastName,
            phoneNumber: data.phoneNumber,
            birthdayDate: data.birthdayDate ? data.birthdayDate.substring(0, 10) : null,
            avatarUrl: data.avatarUrl,

            street: data.address?.street || '',
            city: data.address?.city || '',
            country: data.address?.country || '',
            postalCode: data.address?.postalCode || '',

            emergencyContactName: data.contact?.emergencyContactName || '',
            emergencyContactPhone: data.contact?.emergencyContactPhone || ''
          });

          this.originalData = { ...data };

          console.log('PROFILE DATA:', data);
        },
        error: () => {
          alert('Eroare la încărcarea profilului');
        }
      });
  }

  enableEdit() {
    this.editMode = true;
  }

  cancelEdit() {
    this.editMode = false;

    this.form.patchValue({
      firstName: this.originalData.firstName,
      lastName: this.originalData.lastName,
      phoneNumber: this.originalData.phoneNumber,

      birthdayDate: this.originalData.birthdayDate
        ? this.originalData.birthdayDate.substring(0, 10)
        : null,

      avatarUrl: this.originalData.avatarUrl,

      street: this.originalData.address?.street || '',
      city: this.originalData.address?.city || '',
      country: this.originalData.address?.country || '',
      postalCode: this.originalData.address?.postalCode || '',
      emergencyContactName: this.originalData.contact?.emergencyContactName || '',
      emergencyContactPhone: this.originalData.contact?.emergencyContactPhone || ''
    });
  }

  save() {

    const body = {
      firstName: this.form.value.firstName,
      lastName: this.form.value.lastName,
      phoneNumber: this.form.value.phoneNumber || null,
      birthdayDate: this.form.value.birthdayDate || null,
      avatarUrl: this.form.value.avatarUrl || null,

      street: this.form.value.street || null,
      city: this.form.value.city || null,
      country: this.form.value.country || null,
      postalCode: this.form.value.postalCode || null,

      emergencyContactName: this.form.value.emergencyContactName || null,
      emergencyContactPhone: this.form.value.emergencyContactPhone || null
    };

    console.log(body);

    this.userProfileService.updateProfile(body)
      .subscribe(() => {

        this.editMode = false;
        this.loadProfile();

      });

  }

  loadLeaves() {
    this.leaveService.getMyLeaves().subscribe(res => {
      console.log('RESPONSE:', res); // 🔥

      if (res.isSuccess) {
        this.leaves = res.value.leaves;
        this.vacation = res.value.vacation;
        this.medical = res.value.medical;
      } else {
        console.error(res.error?.errorMessage);
      }
    });
  }

  loadHolidays() {
    const year = new Date().getFullYear();

    this.leaveService.getHolidays(year).subscribe(res => {
      this.holidays = res;
    });
  }

  openLeaveModal() {
    const dialogRef = this.dialog.open(CreateLeaveModalComponent, {
      width: '400px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.createLeave(result);
      }
    });

  }

  createLeave(data: any) {
    this.leaveService.createLeave(data).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          alert('Cerere trimisă');

          this.loadLeaves();
        } else {
          alert(res.error?.errorMessage);
        }
      },
      error: () => {
        alert('Eroare server');
      }
    });
  }

  editLeave(leave: any) {
    if (leave.status !== 'Pending') return;

    const dialogRef = this.dialog.open(CreateLeaveModalComponent, {
      width: '420px',
      data: leave
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.updateLeave(leave.id, result);
      }
    });
  }

  updateLeave(id: string, data: any) {
    this.leaveService.updateLeave(id, data).subscribe({
      next: () => {
        this.loadLeaves();
      },
      error: () => {
        alert('Eroare la update');
      }
    });
  }

  canCancel(leave: any): boolean {
    if (leave.status === 'Pending') return true;

    if (leave.status === 'Approved') {
      const today = new Date();
      const start = new Date(leave.startDate);

      return start > today;
    }

    return false;
  }

  cancelLeave(leave: any) {
    if (!confirm('Sigur vrei să anulezi cererea?')) return;

    this.leaveService.cancelLeave(leave.id).subscribe({
      next: () => {
        this.loadLeaves();
      },
      error: (err) => {
        alert(err.error?.errorMessage || 'Eroare');
      }
    });
  }

  get settings(): FormArray {
    return this.notificationForm.get('settings') as FormArray;
  }


  changePassword() {

    if (this.passwordForm.invalid) return

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value

    if (newPassword !== confirmPassword) {
      alert("Parolele nu coincid")
      return
    }

    this.userProfileService.changePassword({
      currentPassword,
      newPassword
    })
      .subscribe({
        next: () => {
          alert("Parola a fost schimbată")
          this.passwordForm.reset()
        },
        error: () => {
          alert("Eroare schimbare parolă")
        }
      })
  }


  setTab(tab: 'info' | 'password' | 'notifications' | 'leaves') {
    this.activeTab = tab;

    if (tab !== 'info') {
      this.editMode = false;
    }
  }


  isTerminated(): boolean {
    return this.originalData?.employmentStatus === 'Terminated';
  }

  openDocument(doc: any): void {
    this.employeeService.viewDocument(doc.id).subscribe({
      next: (blob) => {
        const fileUrl = URL.createObjectURL(blob);
        window.open(fileUrl, '_blank');
      },
      error: () => {
        alert('Documentul nu a putut fi deschis.');
      }
    });
  }

  downloadDocument(doc: any): void {
    this.employeeService.downloadDocument(doc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = doc.fileName; // 🔥 numele real
        a.click();

        window.URL.revokeObjectURL(url);
      },
      error: () => {
        alert('Documentul nu a putut fi descărcat.');
      }
    });
  }


  private loadNotifications() {
  this.loadingNotifications = true;

  this.userProfileService.getNotificationSettings()
    .pipe(finalize(() => this.loadingNotifications = false))
    .subscribe({
      next: (data) => {
        this.settings.clear();

        data.forEach(item => {
          this.settings.push(
            this.fb.group({
              eventType: [item.eventType],
              channel: [item.channel],
              enabled: [item.enabled],
              digest: [item.digest]
            })
          );
        });
      },
      error: () => {
        alert('Eroare la încărcarea notificărilor');
      }
    });
}

saveNotifications() {
  if (this.notificationForm.invalid) return;

  this.savingNotifications = true;

  const payload = this.notificationForm.getRawValue()
    .settings as NotificationSettingDto[];

  this.userProfileService
    .updateNotificationSettings(payload)
    .pipe(finalize(() => this.savingNotifications = false))
    .subscribe({
      next: () => {
        this.snackbar.showSuccess('Setările de notificare au fost salvate');
      },
      error: () => {
        this.snackbar.showError('Eroare la salvarea notificărilor');
      }
    });
}

  getNotificationLabel(eventType: string): string {
  return this.notificationLabels[eventType] ?? eventType;
}

getChannelLabel(channel: NotificationChannel): string {
  switch (Number(channel)) {
    case NotificationChannel.InApp:
      return 'În aplicație';
    case NotificationChannel.Email:
      return 'Email';
    default:
      return 'Necunoscut';
  }
}

}