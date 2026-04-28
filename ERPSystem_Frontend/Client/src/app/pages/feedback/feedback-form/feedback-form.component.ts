import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { FeedbackService } from '../../services/feedback.service';

@Component({
  selector: 'app-feedback-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './feedback-form.component.html',
  styleUrl: './feedback-form.component.css'
})
export class FeedbackFormComponent implements OnInit {
  token = '';

  formDetails: any = null;

  rating = 5;
  comment = '';

  loading = false;
  submitting = false;
  submitted = false;

  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private feedbackService: FeedbackService
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token') || '';
    this.loadForm();
  }

  loadForm(): void {
    this.loading = true;

    this.feedbackService.getFeedbackForm(this.token).subscribe({
      next: (res: any) => {
        this.formDetails = res?.value ?? res?.data ?? res;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage =
          err?.error?.error?.errorMessage ||
          err?.error?.errorMessage ||
          'Formularul nu a putut fi încărcat.';
      }
    });
  }

  setRating(value: number): void {
    this.rating = value;
  }

  submit(): void {
    if (!this.rating || this.rating < 1 || this.rating > 5) {
      this.errorMessage = 'Alege un rating între 1 și 5.';
      return;
    }

    if (!this.comment.trim()) {
      this.errorMessage = 'Comentariul este obligatoriu.';
      return;
    }

    this.errorMessage = '';
    this.submitting = true;

    const dto = {
      token: this.token,
      rating: this.rating,
      comment: this.comment
    };

    this.feedbackService.submitFeedback(dto).subscribe({
      next: () => {
        this.submitting = false;
        this.submitted = true;
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage =
          err?.error?.error?.errorMessage ||
          err?.error?.errorMessage ||
          'Feedbackul nu a putut fi trimis.';
      }
    });
  }
}
