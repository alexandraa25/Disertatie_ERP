import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CoursesService } from '../../services/courses.service';
import { StudentsService } from '../../services/students.service';
import { FormsModule } from '@angular/forms';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './enroll-students.component.html',
  styleUrls: ['./enroll-students.component.css']
})
export class EnrollStudentsComponent implements OnInit {

  students: any[] = [];
  loading = false;
  q = '';
  mode: 'course' | 'student' = 'course';

  constructor(
    private courses: CoursesService,
    private student: StudentsService,
    private dialogRef: MatDialogRef<EnrollStudentsComponent>,
    @Inject(MAT_DIALOG_DATA) public data: {
      courseId?: number;
      sessionId?: number;
      studentId?: number;
    }
  ) {}

  ngOnInit(): void {

  if (this.data.studentId) {
    this.mode = 'student';
  }

  this.load();

}

 load() {

  this.loading = true;

  if (this.mode === 'course') {

    // CURS -> înscrii elevi
    this.courses.getAvailableStudents(
      this.data.courseId!,
      this.data.sessionId!,
      this.q
    ).subscribe({
      next: (res: any) => {
        this.students = res?.value ?? res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Eroare la încărcare cursanți');
      }
    });

  } else {

    // STUDENT -> vezi cursuri disponibile
    this.student.getAvailableCoursesForStudent(
      this.data.studentId!,
      this.q
    ).subscribe({
      next: (res: any) => {
        this.students = res?.value ?? res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Eroare la încărcare cursuri');
      }
    });

  }

}

enroll(item: any) {

  if (this.mode === 'course') {

    this.courses.enroll(this.data.courseId!, {
      studentId: item.id,
      sessionId: this.data.sessionId!
    }).subscribe({
      next: () => this.load(),
     error: (err) => {
  console.log('FULL ERROR:', err);

  if (typeof err.error === 'string') {
    alert('Server error (non-JSON): ' + err.error);
    return;
  }

  const message =
    err?.error?.message ||
    err?.error?.errorMessage ||
    'Eroare la înscriere';

  alert(message);
}
    });

  } else {

    this.courses.enroll(item.courseId, {
      studentId: this.data.studentId!,
      sessionId: item.sessionId
    }).subscribe({
      next: () => this.load(),
     error: (err) => {
  console.log('Enroll error:', err);

  const message =
    err?.error?.message ||
    err?.error?.errorMessage ||
    err?.message ||
    'Eroare la înscriere';

  alert(message);
}
    });

  }

}

  close() {
    this.dialogRef.close(true);
  }

   getDayName(day: number): string {
    const days: { [key: number]: string } = {
      1: 'Luni',
      2: 'Marți',
      3: 'Miercuri',
      4: 'Joi',
      5: 'Vineri',
      6: 'Sâmbătă',
      7: 'Duminică'
    };

    return days[day] || '-';
  }
}