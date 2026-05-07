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
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';


@Component({
  selector: 'app-profil-user',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, MatDialogModule, ConfirmCustomModalComponent],
  templateUrl: './profil-user.component.html',
  styleUrls: ['./profil-user.component.css'],
})
export class ProfilUserComponent implements OnInit {

  activeTab: 'password' | 'info' | 'leaves' | 'notifications' = 'info';

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
    FeedbackSubmitted: 'Feedback trimis',
    FormSubmitted: 'Formulare interne trimise',
    ReportGenerated: 'Rapoarte generate',
    UserActivity: 'Activitate utilizatori',
    SystemUpdate: 'Modificări administrative',
    Leave: 'Concedii',
    Employee: 'Activitate angajați',
    CourseActivity: 'Activitate cursuri',
    StudentActivity: 'Activitate elevi',
    ContractActivity: 'Activitate contracte',
    Feedback: 'Feedback'
  };

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService,
    private leaveService: LeaveService,
    private employeeService: EmployeeService,
    private dialog: MatDialog,
    private snackbar: SnackbarService,
    private confirmService: ConfirmService,
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

          if (!data) {
            this.snackbar.showError('Profilul nu a putut fi încărcat.', 2500);
            return;
          }

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
        },
        error: () => {
          this.snackbar.showError('Eroare la încărcarea profilului.', 2500);
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

    this.saving = true;

    this.userProfileService.updateProfile(body)
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: (res: any) => {
          if (res?.isSuccess === false) {
            this.snackbar.showError(res.error?.errorMessage || 'Profilul nu a putut fi salvat.', 2500);
            return;
          }

          this.editMode = false;
          this.snackbar.showSuccess('Profil actualizat cu succes.', 1800);
          this.loadProfile();
        },
        error: () => {
          this.snackbar.showError('Eroare la salvarea profilului.', 2500);
        }
      });
  }

  loadLeaves() {
    this.leaveService.getMyLeaves().subscribe({
      next: (res: any) => {
        if (res.isSuccess) {
          this.leaves = res.value.leaves;
          this.vacation = res.value.vacation;
          this.medical = res.value.medical;
        } else {
          this.snackbar.showError(res.error?.errorMessage || 'Concediile nu au putut fi încărcate.', 2500);
        }
      },
      error: () => {
        this.snackbar.showError('Eroare la încărcarea concediilor.', 2500);
      }
    });
  }

  loadHolidays() {
    const year = new Date().getFullYear();

    this.leaveService.getHolidays(year).subscribe({
      next: (res) => {
        this.holidays = res;
      },
      error: () => {
        this.snackbar.showError('Sărbătorile legale nu au putut fi încărcate.', 2500);
      }
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
      next: (res: any) => {
        if (res.isSuccess) {
          this.snackbar.showSuccess('Cerere de concediu trimisă.', 1800);
          this.loadLeaves();
        } else {
          this.snackbar.showError(res.error?.errorMessage || 'Cererea nu a putut fi trimisă.', 2500);
        }
      },
      error: () => {
        this.snackbar.showError('Eroare server la trimiterea cererii.', 2500);
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
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Concediul nu a putut fi actualizat.', 2500);
          return;
        }

        this.snackbar.showSuccess('Concediul a fost actualizat.', 1800);
        this.loadLeaves();
      },
      error: () => {
        this.snackbar.showError('Eroare la actualizarea concediului.', 2500);
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

  async cancelLeave(leave: any): Promise<void> {
    const confirmed = await this.confirmService.confirm(
      'Sigur vrei să anulezi această cerere de concediu?',
      'Confirmare anulare'
    );

    if (!confirmed) return;

    this.leaveService.cancelLeave(leave.id).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Cererea nu a putut fi anulată.',
            2500
          );
          return;
        }

        this.snackbar.showSuccess('Cererea a fost anulată.', 1800);
        this.loadLeaves();
      },
      error: (err) => {
        this.snackbar.showError(
          err.error?.errorMessage || 'Eroare la anularea cererii.',
          2500
        );
      }
    });
  }

  get settings(): FormArray {
    return this.notificationForm.get('settings') as FormArray;
  }

  changePassword() {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      this.snackbar.showError('Completează toate câmpurile pentru schimbarea parolei.', 2200);
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

    if (newPassword !== confirmPassword) {
      this.snackbar.showError('Parolele nu coincid.', 2200);
      return;
    }

    this.userProfileService.changePassword({
      currentPassword,
      newPassword
    }).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Parola nu a putut fi schimbată.', 2500);
          return;
        }

        this.snackbar.showSuccess('Parola a fost schimbată.', 1800);
        this.passwordForm.reset();
      },
      error: () => {
        this.snackbar.showError('Eroare la schimbarea parolei.', 2500);
      }
    });
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
        this.snackbar.showError('Documentul nu a putut fi deschis.', 2500);
      }
    });
  }

  downloadDocument(doc: any): void {
    this.employeeService.downloadDocument(doc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = doc.fileName;
        a.click();

        window.URL.revokeObjectURL(url);

        this.snackbar.showSuccess('Document descărcat.', 1800);
      },
      error: () => {
        this.snackbar.showError('Documentul nu a putut fi descărcat.', 2500);
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
          this.snackbar.showError('Eroare la încărcarea notificărilor.', 2500);
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

  getExperience(hireDate: string | Date): string {
    const start = new Date(hireDate);
    const now = new Date();

    let years = now.getFullYear() - start.getFullYear();
    let months = now.getMonth() - start.getMonth();

    if (months < 0) {
      years--;
      months += 12;
    }

    if (years <= 0) {
      return `${months} luni vechime`;
    }

    if (months === 0) {
      return `${years} ${years === 1 ? 'an' : 'ani'} vechime`;
    }

    return `${years} ${years === 1 ? 'an' : 'ani'} și ${months} luni`;
  }

}