export interface UserProfileDto {
  firstName: string;
  lastName: string;
  phone?: string | null;
  jobTitle?: string | null;
  avatarUrl?: string | null;
  preferredLanguage: string;
  timeZone: string;
}

export interface MeDto {
  userId: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  profile: UserProfileDto;
  unreadNotificationsCount: number;
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
