import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FeedbackService } from '../../services/feedback.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-student-evaluation-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './student-evaluation-modal.component.html',
  styleUrl: './student-evaluation-modal.component.css'
})
export class StudentEvaluationModalComponent {
  @Input() student: any;
  @Input() session: any;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  rating = 5;
  attendanceScore = 5;
  behaviorScore = 5;
  progressScore = 5;
  comment = '';

  saving = false;

  constructor(
    private feedbackService: FeedbackService,
    private snackbar: SnackbarService
  ) {}

  setRating(field: 'rating' | 'attendanceScore' | 'behaviorScore' | 'progressScore', value: number): void {
    this[field] = value;
  }

  save(): void {
    if (!this.student?.studentId || !this.session?.id) {
      this.snackbar.showError('Datele pentru evaluare nu sunt valide.');
      return;
    }

    const dto = {
      studentId: this.student.studentId,
      courseSessionId: this.session.id,
      rating: this.rating,
      attendanceScore: this.attendanceScore,
      behaviorScore: this.behaviorScore,
      progressScore: this.progressScore,
      comment: this.comment
    };

    this.saving = true;

    this.feedbackService.createStudentEvaluation(dto).subscribe({
      next: (res: any) => {
        this.saving = false;

        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Evaluarea nu a putut fi salvată.'
          );
          return;
        }

        this.snackbar.showSuccess('Evaluarea cursantului a fost salvată.');
        this.saved.emit();
      },
      error: () => {
        this.saving = false;
        this.snackbar.showError('Eroare la salvarea evaluării.');
      }
    });
  }

  close(): void {
    this.closed.emit();
  }
}
