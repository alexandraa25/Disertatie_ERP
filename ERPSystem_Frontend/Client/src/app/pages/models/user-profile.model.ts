export interface UserProfileDto {
  firstName: string;
  lastName: string;
  fullName: string;
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
  unreadNotifications: number;
  employeeId?: string;
  jobTitle?: string;
  hireDate?: string;
  salary?: number;
  contractType?: string;
  employmentStatus?: string;
  terminationDate?: string | null;
  address?: AddressDto;
  contact?: ContactDto;
  bank?: BankDto;
  documents?: DocumentDto[];
}

export interface UpdateUserProfileDto {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string | null;
  birthdayDate?: string | null;
  avatarUrl?: string | null;
  street?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

export interface AddressDto {
  street?: string;
  city?: string;
  country?: string;
  postalCode?: string;
}

export interface ContactDto {
  phoneNumber?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

export interface BankDto {
  iban?: string;
  bankName?: string;
}

export interface DocumentDto {
  id: string;
  fileName: string;
  filePath: string;
  documentType?: string;
  uploadedAt: string;
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

export interface NotificationDto {
  id: number;
  userId: string;
  eventType: string;
  title: string;
  message: string;
  type: 'Info' | 'Success' | 'Warning' | 'Error';
  link?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  isRead: boolean;
  seenAt?: string | null;
  readAt?: string | null;
  createdAt: string;
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}
