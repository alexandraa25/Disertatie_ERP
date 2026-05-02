import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FeedbackService } from '../../services/feedback.service';

@Component({
  selector: 'app-global-feedback-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './global-feedback-analytics.component.html',
  styleUrl: './global-feedback-analytics.component.css'
})
export class GlobalFeedbackAnalyticsComponent implements OnInit {
  analytics: any = null;
  loading = false;

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading = true;

    this.feedbackService.getGlobalAnalytics().subscribe({
      next: (res: any) => {
        this.analytics = res?.value ?? res?.data ?? res;
        this.loading = false;
      },
      error: () => {
        this.analytics = null;
        this.loading = false;
      }
    });
  }
}
