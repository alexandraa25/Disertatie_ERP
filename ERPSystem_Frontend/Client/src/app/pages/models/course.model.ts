export interface TeacherOptionDto {
  userId: string;
  displayName: string;
}

export interface CourseSessionDto {
  id: number;

  dayOfWeek: number;
  startTime: string;
  endTime: string;

  capacity?: number | null;
  enrolledActiveCount: number;

  teacherUserId: string;
  teacherName: string;

  fee: number;

  // 🔥 ADAUGĂ
  feeType: 1 | 2;
  totalSessions?: number | null;
}

export interface CourseSessionUpsertDto {
  id?: number | null;

  dayOfWeek: number;
  startTime: string;
  endTime: string;

  teacherUserId: string;

  fee: number;

  // 🔥 ADAUGĂ ASTEA
  feeType: 1 | 2;
  totalSessions?: number | null;

  capacity?: number | null;
}


export interface CourseSessionFormModel extends Partial<CourseSessionUpsertDto> {
  enrolledActiveCount?: number;
}
 
export interface CourseListItemDto {
  id: number;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CourseDetailsDto {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAtUtc: string;

  teacherUserId: string;
  teacherName?: string | null;

  sessions: CourseSessionDto[];
}
export interface CreateCourseDto {
  name: string;
  description?: string | null;
 // teacherUserId: string;
  sessions: CourseSessionUpsertDto[];
}

export interface UpdateCourseDto extends CreateCourseDto {
  isActive: boolean;
}

export interface EnrollmentDto {
  studentId: number;
  studentName: string;
  studentEmail?: string | null;
  enrolledAtUtc: string;
  isActive: boolean;

  sessionId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface CourseEnrollRequest {
  studentId: number;
  sessionId: number;
}