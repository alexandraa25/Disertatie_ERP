export interface UserProfileDto {

  firstName: string;
  lastName: string;

  username: string;

  email: string;
  emailConfirmed: boolean;

  phoneNumber?: string | null;

  roles: string[];

  isActive: boolean;

  birthdayDate?: string | null;

  createdAt: string;
  lastLoginAt?: string | null;

  avatarUrl?: string | null;

  unreadNotificationsCount: number;

  jobTitle?: string;
  hireDate?: string;
  salary?: number;
  contractType?: string;
  employmentStatus?: string;

}

export interface UpdateUserProfileDto {

  firstName?: string;
  lastName?: string;

  phoneNumber?: string | null;

  birthdayDate?: string | null;

  avatarUrl?: string | null;

}
export enum NotificationChannel {
  InApp = 1,
  Email = 2
}

export enum DigestMode {
  Immediate = 1,
  Daily = 2
}

export interface NotificationSettingDto {
  eventType: string;
  channel: NotificationChannel;
  enabled: boolean;
  digest: DigestMode;
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}
