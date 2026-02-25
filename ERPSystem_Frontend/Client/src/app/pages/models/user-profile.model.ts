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
