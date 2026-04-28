import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CoursesService } from '../../services/courses.service';
import { FeedbackService } from '../../services/feedback.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-send-feedback-forms',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './send-feedback-forms.component.html',
  styleUrl: './send-feedback-forms.component.css'
})
export class SendFeedbackFormsComponent implements OnInit {
  @Input() courseId!: number;
  @Input() session: any;

  @Output() closed = new EventEmitter<void>();
  @Output() sent = new EventEmitter<void>();

  enrollments: any[] = [];
  selectedStudentIds: number[] = [];

  loading = false;
  sending = false;

  onlyActive = true;
  onlyNotSent = true;

  constructor(
    private coursesService: CoursesService,
    private feedbackService: FeedbackService,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    this.loadEnrollments();
  }

  loadEnrollments(): void {
    if (!this.courseId || !this.session?.id) return;

    this.loading = true;

    this.coursesService.listEnrollments(this.courseId, this.session.id).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.enrollments = Array.isArray(data) ? data : [];

        this.selectRecommended();

        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Nu s-au putut încărca cursanții.');
      }
    });
  }

  get filteredEnrollments(): any[] {
    return this.enrollments.filter(e => {
      if (this.onlyActive && !e.isActive) return false;
      if (this.onlyNotSent && e.feedbackSent) return false;
      return true;
    });
  }

  onFilterChange(): void {
    this.selectRecommended();
  }

  selectRecommended(): void {
    this.selectedStudentIds = this.filteredEnrollments.map(e => e.studentId);
  }

  selectAllVisible(): void {
    const ids = this.filteredEnrollments.map(e => e.studentId);

    this.selectedStudentIds = [
      ...new Set([...this.selectedStudentIds, ...ids])
    ];
  }

  clearSelection(): void {
    this.selectedStudentIds = [];
  }

  toggleStudent(studentId: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;

    if (checked) {
      if (!this.selectedStudentIds.includes(studentId)) {
        this.selectedStudentIds.push(studentId);
      }
    } else {
      this.selectedStudentIds = this.selectedStudentIds.filter(id => id !== studentId);
    }
  }

  sendForms(): void {
    if (!this.session?.id) {
      this.snackbar.showError('Sesiunea nu este validă.');
      return;
    }

    if (this.selectedStudentIds.length === 0) {
      this.snackbar.showError('Selectează cel puțin un cursant.');
      return;
    }

    const dto = {
      courseSessionId: this.session.id,
      studentIds: this.selectedStudentIds
    };

    this.sending = true;

    this.feedbackService.sendFeedbackForms(dto).subscribe({
      next: (res: any) => {
        this.sending = false;

        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Formularele nu au putut fi trimise.'
          );
          return;
        }

        this.snackbar.showSuccess('Formularele de feedback au fost trimise.');
        this.sent.emit();
      },
      error: () => {
        this.sending = false;
        this.snackbar.showError('Eroare la trimiterea formularelor.');
      }
    });
  }

  close(): void {
    this.closed.emit();
  }
}