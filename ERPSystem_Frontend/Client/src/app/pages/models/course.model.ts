export interface TeacherOptionDto {
  userId: string;
  displayName: string;
}

export interface CourseSessionDto {
  id: number;
  dayOfWeek: number;     // 1..7
  startTime: string;     // "18:00"
  endTime: string;       // "19:30"
  capacity: number | null;
  enrolledActiveCount: number;
}

export interface CourseSessionUpsertDto {
  id?: number | null;  // null/undefined = new
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  teacherUserId: string;
  capacity: number | null;
}
 
export interface CourseListItemDto {
  id: number;
  name: string;
  price?: number | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CourseDetailsDto {
  id: number;
  name: string;
  description?: string | null;
  price?: number | null;
  isActive: boolean;
  createdAtUtc: string;

  teacherUserId: string;
  teacherName?: string | null;

  sessions: CourseSessionDto[];
}
export interface CreateCourseDto {
  name: string;
  description?: string | null;
  price?: number | null;
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