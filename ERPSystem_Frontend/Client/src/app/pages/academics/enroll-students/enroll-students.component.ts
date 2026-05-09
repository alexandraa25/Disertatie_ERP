import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CoursesService } from '../../services/courses.service';
import { StudentsService } from '../../services/students.service';
import { FormsModule } from '@angular/forms';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

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
    private snackbar: SnackbarService,
    private dialogRef: MatDialogRef<EnrollStudentsComponent>,
    @Inject(MAT_DIALOG_DATA) public data: {
      courseId?: number;
      sessionId?: number;
      studentId?: number;
    }
  ) { }

  ngOnInit(): void {
    if (this.data.studentId) {
      this.mode = 'student';
    }

    this.load();
  }

  load() {
    this.loading = true;

    if (this.mode === 'course') {

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
          this.snackbar.showError('Eroare la încărcare cursanți.', 2500);
        }
      });

    }
    else {

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
          this.snackbar.showError('Eroare la încărcare cursuri.', 2500);
        }
      });

    }

  }

  enroll(item: any) {
    const courseId = this.mode === 'course'
      ? this.data.courseId!
      : item.courseId;

    const body = {
      studentId: this.mode === 'course'
        ? item.id
        : this.data.studentId!,
      sessionId: this.mode === 'course'
        ? this.data.sessionId!
        : item.sessionId
    };

    this.courses.enroll(courseId, body).subscribe({
      next: () => {
        this.snackbar.showSuccess('Înscriere realizată cu succes.', 1800);
        this.dialogRef.close(true);
        this.load();
      },
      error: (err) => {
        console.log('Enroll error:', err);

        const message =
          err?.error?.message ||
          err?.error?.errorMessage ||
          err?.message ||
          'Eroare la înscriere.';

        this.snackbar.showError(message, 3000);
      }
    });
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