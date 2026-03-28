import { ActivityLog } from "./activity-log.model";

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
 
  invoices?: InvoiceDto[] | null;
  activityLogs?: ActivityLog[] | null;
}



export interface InvoiceDto {
  id: number;
  number: string;
  date: string; // ISO
  amount: number;
  paid: boolean;
}



export interface StudentCourseDetailsDto {
  courseId: number;
  courseName: string;
  price: number;
  sessionId: number;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  teacherName: string;
  isActive: boolean;
  contractId : number;
  endedAtUtc :string;
  feeType : number;
}

export interface StudentCoursesResponse {
  items: StudentCourseDetailsDto[];
  totalAmount: number;
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


export interface StudentOption {
  id: number;
  fullName: string;
  isMinor: boolean;
}

export interface GuardianOption {
  id: number;
  fullName: string;
}

export interface PagedResult<T> {
  page: number;
  pageSize: number;
  total: number;
  items: T[];
}



