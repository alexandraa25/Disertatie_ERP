import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DigestMode, NotificationChannel } from '../../models/user-profile.model';

@Component({
  selector: 'app-profile-notifications', standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile-notifications.component.html',
  styleUrls: ['./profile-notifications.component.css']
})
export class ProfileNotificationsComponent {
  @Input({ required: true }) notificationForm!: FormGroup;
  @Input() loadingNotifications = true;
  @Input() savingNotifications = false;
  @Input() labels: Record<string, string> = {};
  @Output() save = new EventEmitter<void>();
  readonly DigestMode = DigestMode;
  get settings(): FormArray { return this.notificationForm.get('settings') as FormArray; }
  getNotificationLabel(eventType: string): string { return this.labels[eventType] ?? eventType; }
  getChannelLabel(channel: NotificationChannel): string {
    return Number(channel) === NotificationChannel.InApp ? 'În aplicație'
      : Number(channel) === NotificationChannel.Email ? 'Email' : 'Necunoscut';
  }
  saveNotifications(): void { this.save.emit(); }
}
