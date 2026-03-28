import { Component, OnInit } from '@angular/core';
import { 
  FormBuilder, 
  FormGroup, 
  Validators, 
  ReactiveFormsModule, 
  FormArray 
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

import { UserProfileService } from '../../services/user-profile.service';
import { 
  UserProfileDto, 
  NotificationSettingDto, 
  NotificationChannel, 
  DigestMode 
} from '../../models/user-profile.model';
import { LeaveService } from '../../services/leave.service';

@Component({
  selector: 'app-profil-user',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './profil-user.component.html',
  styleUrls: ['./profil-user.component.css'],
})
export class ProfilUserComponent implements OnInit {

  activeTab: 'info' | 'password' | 'notifications' | 'leaves' = 'info';

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
  openForm = false;

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService, 
    private leaveService: LeaveService
  ) {}

 ngOnInit(): void {

  // ===== PROFILE FORM =====
  this.form = this.fb.group({
    firstName: [''],
    lastName: [''],
    phoneNumber: [''], 
      birthdayDate: [null],

  avatarUrl: ['']
  });

  console.log('Token ' + localStorage.getItem('accessToken'));

  this.loadProfile();

  // ===== NOTIFICATIONS FORM =====
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
            ? data.birthdayDate.substring(0,10)
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
      ? this.originalData.birthdayDate.substring(0,10)
      : null,

    avatarUrl: this.originalData.avatarUrl
  });
}


loadLeaves() {
  this.leaveService.getMyLeaves().subscribe(res => {
    this.leaves = res;
  });
}

submitLeave() {

  if (this.leaveForm.invalid) return;

  this.leaveService.createLeave(this.leaveForm.value).subscribe(() => {
    this.openForm = false;
    this.leaveForm.reset();
    this.loadLeaves();
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

  setTab(tab: 'info' | 'password' | 'notifications') {
    this.activeTab = tab;

    // dacă schimbi tab-ul, ieși din edit mode
    if (tab !== 'info') {
      this.editMode = false;
    }
  }

}