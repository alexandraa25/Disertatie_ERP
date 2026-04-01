import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { MatDialog,  MatDialogModule  } from '@angular/material/dialog';

import { UserProfileService } from '../../services/user-profile.service';
import { UserProfileDto, NotificationSettingDto, NotificationChannel, DigestMode } from '../../models/user-profile.model';
import { LeaveService } from '../../services/leave.service';

import { CreateLeaveModalComponent } from '../../hr/create-leave-modal/create-leave-modal.component';

@Component({
  selector: 'app-profil-user',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, MatDialogModule],
  templateUrl: './profil-user.component.html',
  styleUrls: ['./profil-user.component.css'],
})
export class ProfilUserComponent implements OnInit {

  activeTab: 'password' | 'info' | 'notifications' | 'leaves' = 'info';

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

  originalData!: UserProfileDto;

  leaveForm!: FormGroup;
  leaves: any[] = [];
vacation: any;
medical: any;

holidays: string[] = [];
  openForm = false;
  

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService,
    private leaveService: LeaveService,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {

    this.form = this.fb.group({
      firstName: [''],
      lastName: [''],
      phoneNumber: [''],
      birthdayDate: [null],
      avatarUrl: ['']
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
  // ================= PROFILE =================

  private loadProfile() {

    this.userProfileService.getProfile()
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (data: UserProfileDto) => {

          if (!data) return;

          this.form.patchValue({
            firstName: data.firstName,
            lastName: data.lastName,

            phoneNumber: data.phoneNumber,

            birthdayDate: data.birthdayDate
              ? data.birthdayDate.substring(0, 10)
              : null,

            avatarUrl: data.avatarUrl
          });

          this.originalData = { ...data };

          console.log("PROFILE", data);
        },

        error: () => {
          alert("Eroare la încărcarea profilului");
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

      avatarUrl: this.originalData.avatarUrl
    });
  }


  save() {

      const body = {

        firstName: this.form.value.firstName,
        lastName: this.form.value.lastName,

        phoneNumber: this.form.value.phoneNumber || null,

        birthdayDate: this.form.value.birthdayDate || null,

        avatarUrl: this.form.value.avatarUrl || null

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

    return start > today; // 🔥 doar dacă NU a început
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

    
  // ================= NOTIFICATIONS =================

  get settings(): FormArray {
      return this.notificationForm.get('settings') as FormArray;
    }

  private loadNotifications() {

    this.userProfileService.getNotificationSettings()
      .pipe(finalize(() => this.loadingNotifications = false))
      .subscribe({
        next: (data) => {

          this.settings.clear();

          if (!data?.length) return;

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
        }
      });
  }

  saveNotifications() {

    this.savingNotifications = true;

    const payload: NotificationSettingDto[] =
      this.notificationForm.value.settings;

    this.userProfileService
      .updateNotificationSettings(payload)
      .pipe(finalize(() => this.savingNotifications = false))
      .subscribe();
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

  // ================= TAB CHANGE =================

 setTab(tab: 'info' | 'password' | 'notifications' | 'leaves') {
    this.activeTab = tab;

    // dacă schimbi tab-ul, ieși din edit mode
    if (tab !== 'info') {
      this.editMode = false;
    }
  }

}