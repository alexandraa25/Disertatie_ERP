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
      error: () => alert('Eroare la înscriere')
    });

  } else {

    this.courses.enroll(item.courseId, {
      studentId: this.data.studentId!,
      sessionId: item.sessionId
    }).subscribe({
      next: () => this.load(),
      error: () => alert('Eroare la înscriere')
    });

  }

}



  close() {
    this.dialogRef.close(true);
  }
}