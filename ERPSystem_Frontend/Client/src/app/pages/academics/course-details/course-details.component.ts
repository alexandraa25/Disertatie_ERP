import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CoursesService } from '../../services/courses.service';
import { MatDialog } from '@angular/material/dialog';
import { EnrollStudentsComponent } from '../enroll-students/enroll-students.component';


@Component({
  standalone: true,
  selector: 'app-course-details',
  imports: [CommonModule],
  templateUrl: './course-details.component.html',
  styleUrls: ['./course-details.component.css']
})
export class CourseDetailsComponent implements OnInit {

  loading = true;
  course: any;
  expandedSessionId: number | null = null;
  loadingEnrollments = false;
  enrollments: { [sessionId: number]: any[] } = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private courses: CoursesService,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.courses.get(id).subscribe({
      next: (res: any) => {
        this.course = res?.value ?? res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Nu am putut încărca cursul.');
        this.router.navigate(['/courses']);
      }
    });
  }

  back(): void {
    this.router.navigate(['/courses']);
  }

  toggleSession(sessionId: number) {

    if (this.expandedSessionId === sessionId) {
      this.expandedSessionId = null;
      return;
    }

    this.expandedSessionId = sessionId;

    if (!this.enrollments[sessionId]) {
      this.loadEnrollments(sessionId);
    }
  }

  loadEnrollments(sessionId: number) {

    this.loadingEnrollments = true;

    this.courses.listEnrollments(this.course.id).subscribe({
      next: (res: any) => {
        const list = res?.value ?? res;

        this.enrollments[sessionId] =
          list.filter((x: any) => x.sessionId === sessionId);

        this.loadingEnrollments = false;
      },
      error: () => {
        this.loadingEnrollments = false;
        alert('Eroare la încărcare cursanți.');
      }
    });
  }

  toggleEnrollment(sessionId: number, enrollment: any) {
    this.courses.setEnrollmentActive(
      this.course.id,
      sessionId,
      enrollment.studentId,
      !enrollment.isActive
    ).subscribe({
      next: () => {
        this.loadEnrollments(sessionId); // 🔥 reîncarci lista din backend
      },
      error: () => alert('Eroare la actualizare.')
    });
  }

  openEnrollModal(session: any) {
    const ref = this.dialog.open(EnrollStudentsComponent, {
      width: '600px',
      data: {
        courseId: this.course.id,
        sessionId: session.id
      }
    });

    ref.afterClosed().subscribe(result => {
      if (result) {
        this.loadEnrollments(session.id);
      }
    });
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