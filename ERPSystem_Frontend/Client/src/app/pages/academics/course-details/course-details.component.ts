import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CoursesService } from '../../services/courses.service';
import { MatDialog } from '@angular/material/dialog';
import { EnrollStudentsComponent } from '../enroll-students/enroll-students.component';
import { SendFeedbackFormsComponent } from '../../feedback/send-feedback-forms/send-feedback-forms.component';
import { SessionFeedbackReviewsComponent } from '../../feedback/session-feedback-reviews/session-feedback-reviews.component';
import { StudentEvaluationModalComponent } from '../../feedback/student-evaluation-modal/student-evaluation-modal.component';
import { ConfirmService } from '../../services/confirm.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';


@Component({
  standalone: true,
  selector: 'app-course-details',
  imports: [CommonModule, SendFeedbackFormsComponent, SessionFeedbackReviewsComponent, StudentEvaluationModalComponent, ConfirmCustomModalComponent],
  templateUrl: './course-details.component.html',
  styleUrls: ['./course-details.component.css']
})
export class CourseDetailsComponent implements OnInit {

  loading = true;
  course: any;
  expandedSessionId: number | null = null;
  loadingEnrollments = false;
  enrollments: { [sessionId: number]: any[] } = {};

  showFeedbackModal = false;
  selectedFeedbackSession: any = null;

  showReviewsModal = false;
  selectedReviewsSession: any = null;

  showStudentEvaluationModal = false;
  selectedEvaluationStudent: any = null;
  selectedEvaluationSession: any = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private courses: CoursesService,
    private dialog: MatDialog,
    private confirmService: ConfirmService,
    private snackbar: SnackbarService
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
        this.snackbar.showError('Nu am putut încărca cursul.', 2500);
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

    this.courses.listEnrollments(this.course.id, sessionId).subscribe({
      next: (res: any) => {
        const list = res?.value ?? res;

        this.enrollments[sessionId] = list;

        this.loadingEnrollments = false;
      },
      error: () => {
        this.loadingEnrollments = false;
         this.snackbar.showError('Eroare la încărcare cursanți.', 2500);
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
      this.loadEnrollments(sessionId);

      this.snackbar.showSuccess(
        enrollment.isActive
          ? 'Cursant dezactivat.'
          : 'Cursant activat.',
        1800
      );
    },
    error: (err) => {
      this.snackbar.showError(
        err?.error?.message || 'Eroare la actualizare.',
        2500
      );
    }
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

  openFeedbackModal(session: any): void {
    this.selectedFeedbackSession = session;
    this.showFeedbackModal = true;
  }

  closeFeedbackModal(): void {
    this.showFeedbackModal = false;
    this.selectedFeedbackSession = null;
  }

  onFeedbackFormsSent(): void {
    const sessionId = this.selectedFeedbackSession?.id;

    this.closeFeedbackModal();

    if (sessionId) {
      this.loadEnrollments(sessionId);
    }
  }

  openReviewsModal(session: any): void {
    this.selectedReviewsSession = session;
    this.showReviewsModal = true;
  }

  closeReviewsModal(): void {
    this.showReviewsModal = false;
    this.selectedReviewsSession = null;
  }

  openStudentEvaluationModal(session: any, enrollment: any): void {
    this.selectedEvaluationSession = session;
    this.selectedEvaluationStudent = enrollment;
    this.showStudentEvaluationModal = true;
  }

  closeStudentEvaluationModal(): void {
    this.showStudentEvaluationModal = false;
    this.selectedEvaluationStudent = null;
    this.selectedEvaluationSession = null;
  }

  onStudentEvaluationSaved(): void {
    this.closeStudentEvaluationModal();
  }

  remainingSeats(s: any): number {
    return Math.max((s.capacity ?? 0) - (s.enrolledActiveCount ?? 0), 0);
  }

  async toggleSessionStatus(session: any): Promise<void> {
  const action = session.isActive ? 'dezactivezi' : 'activezi';

  const confirmed = await this.confirmService.confirm(
    `Sigur vrei să ${action} această sesiune?`,
    'Confirmare'
  );

  if (!confirmed) return;

  this.courses.toggleSessionStatus(session.id).subscribe({
    next: (res: any) => {
      const updated = res?.value;

      session.isActive = updated
        ? updated.isActive
        : !session.isActive;

      this.snackbar.showSuccess(
        session.isActive
          ? 'Sesiune activată'
          : 'Sesiune dezactivată',
        1500
      );
    },

    error: (err) => {
      const message = err?.error?.message ?? '';
      const code = err?.error?.code ?? '';

      if (code === 'BUSINESS_RULE') {
        this.snackbar.showError(
          message || 'Nu poți dezactiva sesiunea. Există cursanți activi.',
          3000
        );
        return;
      }

      if (code === 'NOT_FOUND') {
        this.snackbar.showError('Sesiunea nu a fost găsită.', 2500);
        return;
      }

      this.snackbar.showError('Eroare la actualizarea sesiunii', 2500);
    }
  });
}
}