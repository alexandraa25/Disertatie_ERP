import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FeedbackService } from '../../services/feedback.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-session-feedback-reviews',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './session-feedback-reviews.component.html',
  styleUrl: './session-feedback-reviews.component.css'
})
export class SessionFeedbackReviewsComponent implements OnInit {
  @Input() session: any;

  @Output() closed = new EventEmitter<void>();

  reviews: any[] = [];
  loading = false;

  constructor(
    private feedbackService: FeedbackService,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    this.loadReviews();
  }

  loadReviews(): void {
    if (!this.session?.id) return;

    this.loading = true;

    this.feedbackService.getSessionReviews(this.session.id).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.reviews = Array.isArray(data) ? data : [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Nu s-au putut încărca feedbackurile.');
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  getAverageRating(): number {
    if (!this.reviews.length) return 0;

    const total = this.reviews.reduce((sum, x) => sum + Number(x.rating || 0), 0);
    return Math.round((total / this.reviews.length) * 10) / 10;
  }

  getSentimentLabel(sentiment: string): string {
    switch (sentiment) {
      case 'positive':
        return 'Pozitiv';
      case 'negative':
        return 'Negativ';
      case 'neutral':
        return 'Neutru';
      default:
        return 'Neanalizat';
    }
  }

  getSentimentClass(sentiment: string): string {
    switch (sentiment) {
      case 'positive':
        return 'positive';
      case 'negative':
        return 'negative';
      case 'neutral':
        return 'neutral';
      default:
        return 'unknown';
    }
  }

  parseKeywords(keywords: string | null): string[] {
    if (!keywords) return [];

    try {
      const parsed = JSON.parse(keywords);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return keywords.split(',').map(x => x.trim()).filter(Boolean);
    }
  }
}
