import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FeedbackService } from '../../services/feedback.service';


@Component({
  selector: 'app-external-feedback',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './external-feedback.component.html',
  styleUrl: './external-feedback.component.css'
})
export class ExternalFeedbackComponent implements OnInit {
  reviews: any[] = [];
  analytics: any = null;

  loadingReviews = false;
  loadingAnalytics = false;
  saving = false;

  filter = {
    targetType: '',
    targetId: null as string | null,
    source: ''
  };

  form = {
    source: 'Manual',
    targetType: 'General',
    targetId: null as string | null,
    rating: null as number | null,
    comment: ''
  };

  showCreateModal = false;

  targetOptions: any[] = [];
  loadingTargets = false;

  filterTimeout: any;

  filterTargetOptions: any[] = [];
  loadingFilterTargets = false;


  activeTab: 'reviews' | 'analytics' = 'reviews';

  currentPage = 1;
  pageSize = 5;

  openedReviewAnalysisIds = new Set<number>();

  constructor(private feedbackService: FeedbackService) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loadReviews();
    this.loadAnalytics();
  }

  loadReviews(): void {
    this.loadingReviews = true;

    this.feedbackService
      .getExternalReviews(this.filter.targetType || undefined, this.filter.targetId || undefined, this.filter.source || undefined)
      .subscribe({
        next: (res: any) => {
          const data = res?.value ?? res?.data ?? res;
          this.reviews = Array.isArray(data) ? data : [];
          this.currentPage = 1;
          this.openedReviewAnalysisIds.clear();
          this.loadingReviews = false;
        },
        error: () => {
          this.loadingReviews = false;
        }
      });
  }

  loadAnalytics(): void {
    this.loadingAnalytics = true;

    this.feedbackService
      .getExternalAnalytics(this.filter.targetType || undefined, this.filter.targetId || undefined, this.filter.source || undefined)
      .subscribe({
        next: (res: any) => {
          this.analytics = res?.value ?? res?.data ?? res;
          this.loadingAnalytics = false;
        },
        error: () => {
          this.analytics = null;
          this.loadingAnalytics = false;
        }
      });
  }

  openCreateModal(): void {
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  save(): void {
    if (!this.form.source || !this.form.comment) {
      return;
    }

    this.saving = true;

    this.feedbackService.createExternalReview(this.form).subscribe({
      next: () => {
        this.saving = false;
        this.closeCreateModal();
        this.form = {
          source: 'Manual',
          targetType: 'General',
          targetId: null,
          rating: null,
          comment: ''
        };
        this.loadData();
      },
      error: () => {
        this.saving = false;
      }
    });
  }

  applyFilters(): void {
    this.loadData();
  }

  onFilterTargetTypeChange(): void {
    this.filter.targetId = null;
    this.filterTargetOptions = [];

    if (!this.filter.targetType || this.filter.targetType === 'General') {
      this.onFilterChange();
      return;
    }

    this.loadingFilterTargets = true;

    this.feedbackService.getExternalReviewTargets(this.filter.targetType).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.filterTargetOptions = Array.isArray(data) ? data : [];
        this.loadingFilterTargets = false;
        this.onFilterChange();
      },
      error: () => {
        this.filterTargetOptions = [];
        this.loadingFilterTargets = false;
        this.onFilterChange();
      }
    });
  }


  onFilterChange(): void {
    clearTimeout(this.filterTimeout);

    this.filterTimeout = setTimeout(() => {
      this.analytics = null;
      this.loadReviews();

      if (this.activeTab === 'analytics') {
        this.loadAnalytics();
      }
    }, 300);
  }

  parseKeywords(keywords: string | null): string[] {
    if (!keywords) return [];
    return keywords.split(',').map(x => x.trim()).filter(Boolean);
  }

  onTargetTypeChange(): void {
    this.form.targetId = null;
    this.targetOptions = [];

    if (this.form.targetType === 'General') {
      return;
    }

    this.loadingTargets = true;

    this.feedbackService.getExternalReviewTargets(this.form.targetType).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.targetOptions = Array.isArray(data) ? data : [];
        this.loadingTargets = false;
      },
      error: () => {
        this.targetOptions = [];
        this.loadingTargets = false;
      }
    });
  }

  setTab(tab: 'reviews' | 'analytics'): void {
    this.activeTab = tab;

    if (tab === 'analytics' && !this.analytics) {
      this.loadAnalytics();
    }
  }

  parseTopics(topicsJson: string | null): any[] {
    if (!topicsJson) return [];

    try {
      const parsed = JSON.parse(topicsJson);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  get totalPages(): number {
    return Math.ceil(this.reviews.length / this.pageSize) || 1;
  }

  get pagedReviews(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.reviews.slice(start, start + this.pageSize);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  toggleReviewAnalysis(reviewId: number): void {
    if (this.openedReviewAnalysisIds.has(reviewId)) {
      this.openedReviewAnalysisIds.delete(reviewId);
    } else {
      this.openedReviewAnalysisIds.add(reviewId);
    }
  }

  isReviewAnalysisOpen(reviewId: number): boolean {
    return this.openedReviewAnalysisIds.has(reviewId);
  }
}
