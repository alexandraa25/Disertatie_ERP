import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketingCampaignService } from '../../services/marketing.service';
import { CoursesService } from '../../services/courses.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-create-campaign',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-campaign.component.html',
  styleUrl: './create-campaign.component.css'
})
export class CreateCampaignComponent implements OnInit {
  @Input() campaign: any = null;

  @Output() saved = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  campaignForm: any = this.getEmptyForm();

  courses: any[] = [];
  courseSessions: any[] = [];

  constructor(
    private campaignService: MarketingCampaignService,
    private courseService: CoursesService,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    if (this.campaign) {
      this.prepareEditForm(this.campaign);
    } else {
      this.campaignForm = this.getEmptyForm();
      this.loadCourses();
    }
  }

  getEmptyForm(): any {
    return {
      name: '',
      description: '',
      startDate: '',
      endDate: '',
      isActive: true,
      discountType: 1,
      discountValue: 0,
      discountScope: 0,
      courseIds: [],
      courseSessionIds: []
    };
  }

  prepareEditForm(campaign: any): void {
    const relations = campaign.courseSessions ?? [];

    const sessionIds = relations.map((x: any) => x.courseSessionId);

    const courseIds = [
      ...new Set(
        relations
          .map((x: any) => x.courseId)
          .filter((x: any) => x)
      )
    ];

    this.campaignForm = {
      name: campaign.name,
      description: campaign.description,
      startDate: this.toDateInputValue(campaign.startDate),
      endDate: this.toDateInputValue(campaign.endDate),
      isActive: campaign.isActive,
      discountType: campaign.discountType,
      discountValue: campaign.discountValue,
      discountScope: campaign.discountScope,
      courseIds: courseIds,
      courseSessionIds: sessionIds
    };

    this.courseSessions = [];
    this.loadCourses();

    setTimeout(() => {
      this.loadSessionsForSelectedCourses(false);
    }, 100);
  }

  saveCampaign(): void {
    if (!this.campaignForm.name?.trim()) {
      this.snackbar.showError('Numele campaniei este obligatoriu.');
      return;
    }

    if (!this.campaignForm.startDate || !this.campaignForm.endDate) {
      this.snackbar.showError('Perioada campaniei este obligatorie.');
      return;
    }

    if (this.campaignForm.endDate < this.campaignForm.startDate) {
      this.snackbar.showError('Data de final nu poate fi înainte de data de început.');
      return;
    }

    if (Number(this.campaignForm.discountValue) <= 0) {
      this.snackbar.showError('Valoarea discountului trebuie să fie mai mare decât 0.');
      return;
    }

    if (
      Number(this.campaignForm.discountType) === 1 &&
      Number(this.campaignForm.discountValue) > 100
    ) {
      this.snackbar.showError('Discountul procentual nu poate depăși 100%.');
      return;
    }

    if (
      !this.campaignForm.courseSessionIds ||
      this.campaignForm.courseSessionIds.length === 0
    ) {
      this.snackbar.showError('Trebuie să selectezi cel puțin o sesiune de curs.');
      return;
    }

    const dto = {
      ...this.campaignForm,
      discountType: Number(this.campaignForm.discountType),
      discountScope: Number(this.campaignForm.discountScope),
      discountValue: Number(this.campaignForm.discountValue)
    };

    const request$ = this.campaign
      ? this.campaignService.update(this.campaign.id, dto)
      : this.campaignService.create(dto);

    request$.subscribe({
      next: (res: any) => {
  if (res?.isSuccess === false) {
    this.snackbar.showError(
      res.error?.errorMessage || 'Campania nu a putut fi salvată.'
    );
    return;
  }

  this.snackbar.showSuccess('Campania a fost salvată cu succes.');
  this.saved.emit();
  this.closeModal();
},
      error: () => {
        this.snackbar.showError('Eroare la salvarea campaniei.');
      }
    });
  }

  closeModal(): void {
    this.closed.emit();
  }

  onScopeChange(): void {
    this.campaignForm.courseIds = [];
    this.campaignForm.courseSessionIds = [];
    this.courseSessions = [];
    this.loadCourses();
  }

  loadCourses(): void {
    this.courseService.list(
      undefined,
      'active',
      'notDeleted',
      this.campaignForm.discountScope
    ).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.courses = Array.isArray(data) ? data : [];
      },
      error: () => {
        this.courses = [];
        this.snackbar.showError('Eroare la încărcarea cursurilor.');
      }
    });
  }

  loadSessionsForSelectedCourses(resetSelectedSessions: boolean = false): void {
    this.courseSessions = [];

    if (resetSelectedSessions) {
      this.campaignForm.courseSessionIds = [];
    }

    if (!this.campaignForm.courseIds || this.campaignForm.courseIds.length === 0) {
      this.campaignForm.courseSessionIds = [];
      return;
    }

    const feeType = this.getFeeTypeFromScope(this.campaignForm.discountScope);

    let allSessions: any[] = [];
    let loadedCount = 0;

    this.campaignForm.courseIds.forEach((courseId: number) => {
      this.courseService.get(courseId).subscribe({
        next: (res: any) => {
          const course = res?.value ?? res?.data ?? res;
          const sessions = course?.sessions ?? [];

          const filteredSessions = feeType
            ? sessions.filter((s: any) => s.feeType === feeType)
            : sessions;

          allSessions = [...allSessions, ...filteredSessions];
          loadedCount++;

          if (loadedCount === this.campaignForm.courseIds.length) {
            this.courseSessions = allSessions;

            const availableSessionIds = this.courseSessions.map((s: any) => s.id);

            this.campaignForm.courseSessionIds =
              this.campaignForm.courseSessionIds.filter((id: number) =>
                availableSessionIds.includes(id)
              );
          }
        },
        error: () => {
          loadedCount++;

          if (loadedCount === this.campaignForm.courseIds.length) {
            this.courseSessions = allSessions;
          }
        }
      });
    });
  }

  toggleCourse(courseId: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;

    if (checked) {
      this.campaignForm.courseIds.push(courseId);
    } else {
      this.campaignForm.courseIds = this.campaignForm.courseIds.filter(
        (id: number) => id !== courseId
      );
    }

    this.campaignForm.courseSessionIds = [];
    this.loadSessionsForSelectedCourses();
  }

  toggleSession(sessionId: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;

    if (checked) {
      this.campaignForm.courseSessionIds.push(sessionId);
    } else {
      this.campaignForm.courseSessionIds =
        this.campaignForm.courseSessionIds.filter(
          (id: number) => id !== sessionId
        );
    }
  }

  getFeeTypeFromScope(scope: number): number | null {
    if (scope === 1) return 1;
    if (scope === 2) return 2;
    return null;
  }

  private toDateInputValue(date: string): string {
    return date ? date.substring(0, 10) : '';
  }
}