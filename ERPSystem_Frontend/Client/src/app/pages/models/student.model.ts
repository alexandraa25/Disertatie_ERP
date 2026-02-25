export interface GuardianDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  relationshipType: string;
  isPrimaryContact: boolean;
}
export interface StudentListItemDto {
  id: number;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface StudentDetailsDto extends StudentListItemDto {
  firstName?: string | null;
  lastName?: string | null;
  address?: string | null;
  dateOfBirth?: string | null; // ISO
  guardians: GuardianDto[];
}

export interface CreateGuardianDto {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  relationshipType: string;
  isPrimaryContact: boolean;
}
export interface CreateStudentDto {
  fullName: string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  dateOfBirth?: string | null;
   guardians?: CreateGuardianDto[] | null;
}

export interface UpdateStudentDto extends CreateStudentDto {
  isActive: boolean;
}

export interface PagedResult<T> {
  page: number;
  pageSize: number;
  total: number;
  items: T[];
}
