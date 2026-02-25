import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CoursesService } from '../../services/courses.service';
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

  constructor(
    private courses: CoursesService,
    private dialogRef: MatDialogRef<EnrollStudentsComponent>,
    @Inject(MAT_DIALOG_DATA) public data: {
      courseId: number;
      sessionId: number;
    }
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.loading = true;

    this.courses.getAvailableStudents(
      this.data.courseId,
      this.data.sessionId,
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
  }

  enroll(studentId: number) {
   this.courses.enroll(this.data.courseId, {
  studentId: studentId,
  sessionId: this.data.sessionId
}).subscribe({
  next: () => this.load(),
  error: () => alert('Eroare la înscriere')
});
  }

  close() {
    this.dialogRef.close(true);
  }
}