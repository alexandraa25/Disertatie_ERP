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

@Component({
  selector: 'app-profil-user',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './profil-user.component.html',
  styleUrls: ['./profil-user.component.css'],
})
export class ProfilUserComponent implements OnInit {

  activeTab: 'info' | 'password' | 'notifications' = 'info';

  loading = true;
  saving = false;
  editMode = false;

  loadingNotifications = true;
  savingNotifications = false;

  NotificationChannel = NotificationChannel;
  DigestMode = DigestMode;

  form!: FormGroup;
  notificationForm!: FormGroup;

  originalData!: UserProfileDto;

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService
  ) {}

  ngOnInit(): void {

    // ===== PROFILE FORM =====
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      phone: [''],
      jobTitle: [''],
      avatarUrl: [''],
      preferredLanguage: ['ro'],
      timeZone: ['Europe/Bucharest']
    });

console.log('Token ' + localStorage.getItem('accessToken'));

    this.loadProfile();

    // ===== NOTIFICATIONS FORM =====
    this.notificationForm = this.fb.group({
      settings: this.fb.array([])
    });

    this.loadNotifications();
  }

  // ================= PROFILE =================

  private loadProfile() {
    this.userProfileService.getProfile()
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (data) => {
          if (!data) return;

          this.form.patchValue(data);
          this.originalData = { ...data };
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
    this.form.patchValue(this.originalData);
  }

  save() {
    if (this.form.invalid) return;

    this.saving = true;

    this.userProfileService.updateProfile(this.form.value)
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => {
          this.editMode = false;
          this.originalData = { ...this.form.value };
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

  // ================= TAB CHANGE =================

  setTab(tab: 'info' | 'password' | 'notifications') {
    this.activeTab = tab;

    // dacă schimbi tab-ul, ieși din edit mode
    if (tab !== 'info') {
      this.editMode = false;
    }
  }

}