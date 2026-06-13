import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { FeedbackService } from '../../services/feedback.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-session-feedback-reviews',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './session-feedback-reviews.component.html',
  styleUrl: './session-feedback-reviews.component.css'
})
export class SessionFeedbackReviewsComponent implements OnInit, OnDestroy {
  @Input() session: any;
  @Output() closed = new EventEmitter<void>();

  reviews: any[] = [];
  loading = false;

  activeTab: 'reviews' | 'analytics' = 'reviews';

  analytics: any = null;
  loadingAnalytics = false;
  trendChart: Chart | null = null;

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

  setTab(tab: 'reviews' | 'analytics'): void {
  this.activeTab = tab;

  if (tab !== 'analytics') {
    this.destroyTrendChart();
    return;
  }

  if (!this.analytics) {
    this.loadAnalytics();
    return;
  }

  setTimeout(() => this.createTrendChart(), 0);
}

  loadAnalytics(): void {
    if (!this.session?.id) return;

    this.loadingAnalytics = true;

    this.feedbackService.getCourseAnalytics(this.session.id).subscribe({
      next: (res: any) => {
        this.analytics = res?.value ?? res?.data ?? res;
        this.loadingAnalytics = false;

        setTimeout(() => this.createTrendChart(), 0);
      },
      error: () => {
        this.loadingAnalytics = false;
        this.snackbar.showError('Nu s-a putut încărca analiza.');
      }
    });
  }

  createTrendChart(): void {
    if (!this.analytics?.trend?.length) return;

    const canvas = document.getElementById('trendChart') as HTMLCanvasElement;
    if (!canvas) return;

    this.destroyTrendChart();

    this.trendChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels: this.analytics.trend.map((x: any) => x.month),
        datasets: [
          {
            label: 'Rating mediu',
            data: this.analytics.trend.map((x: any) => x.averageRating),
            tension: 0.3
          },
          {
            label: 'Pozitiv %',
            data: this.analytics.trend.map((x: any) => x.positivePercent),
            tension: 0.3
          },
          {
            label: 'Negativ %',
            data: this.analytics.trend.map((x: any) => x.negativePercent),
            tension: 0.3
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false
      }
    });
  }

  ngOnDestroy(): void {
  this.destroyTrendChart();
}

destroyTrendChart(): void {
  if (this.trendChart) {
    this.trendChart.destroy();
    this.trendChart = null;
  }
}

close(): void {
  this.destroyTrendChart();
  this.closed.emit();
}

  getAverageRating(): number {
    if (!this.reviews.length) return 0;

    const total = this.reviews.reduce((sum, x) => sum + Number(x.rating || 0), 0);
    return Math.round((total / this.reviews.length) * 10) / 10;
  }

  getSentimentLabel(sentiment: string): string {
    switch (sentiment) {
      case 'pozitiv': return 'Pozitiv';
      case 'negativ': return 'Negativ';
      case 'neutru': return 'Neutru';
      default: return 'Neanalizat';
    }
  }

  getSentimentClass(sentiment: string): string {
    switch (sentiment) {
      case 'pozitiv': return 'positive';
      case 'negativ': return 'negative';
      case 'neutru': return 'neutral';
      default: return 'unknown';
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

  getRiskLevelLabel(level: string): string {
  switch (level) {
    case 'high': return 'Ridicat';
    case 'medium': return 'Mediu';
    case 'low': return 'Scăzut';
    default: return 'Necunoscut';
  }
}

getRiskLevelClass(level: string): string {
  switch (level) {
    case 'high': return 'risk-high';
    case 'medium': return 'risk-medium';
    case 'low': return 'risk-low';
    default: return 'risk-unknown';
  }
}

formatScore(value: number | null | undefined): string {
  if (value === null || value === undefined) return '-';
  return Number(value).toFixed(2);
}

formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) return '0%';
  return `${Number(value).toFixed(2)}%`;
}
}