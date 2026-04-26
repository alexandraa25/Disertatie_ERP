import { Component, OnInit } from '@angular/core';
import { MarketingCampaignService } from '../../services/marketing.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { CoursesService } from '../../services/courses.service';
import { ConfirmService } from '../../services/confirm.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';


@Component({
  selector: 'app-marketing-campaigns',
  standalone: true,
  imports: [FormsModule, CommonModule, ConfirmCustomModalComponent],
  templateUrl: './marketing-campaigns.component.html',
  styleUrl: './marketing-campaigns.component.css'
})
export class MarketingCampaignsComponent implements OnInit {
  campaigns: any[] = [];
  loading = false;

  searchTerm = '';
  totalCount = 0;
  totalPages = 1;

  showCreateModal = false;
  selectedCampaign: any = null;

  today: string = new Date().toISOString().split('T')[0];

  filters = {
  Search: '',
  IsActive: '',
  Scope: '',
  SortBy: 'startDate',
  Desc: true,
  Page: 1,
  PageSize: 10
};

  campaignForm: any = this.getEmptyForm();

  courses: any[] = [];
  courseSessions: any[] = [];

  showEndDateModal = false;
tempEndDate: string = '';

  constructor(
    private campaignService: MarketingCampaignService,
    private courseService: CoursesService, 
    public confirmService: ConfirmService,
  private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    this.loadCampaigns();
    this.loadCourses();
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

  loadCampaigns(): void {
    this.loading = true;

    this.campaignService.getAll(this.filters).subscribe({
      next: (res: any) => {
        if (res.isSuccess) {
          this.campaigns = res.value.items;
          this.totalCount = res.value.totalCount;
          this.totalPages = Math.ceil(this.totalCount / this.filters.PageSize);
        }

        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  openCreateModal(): void {
    this.selectedCampaign = null;
    this.campaignForm = this.getEmptyForm();
    this.courseSessions = [];
    this.loadCourses();
    this.showCreateModal = true;
  }

 openEditModal(campaign: any): void {
  this.selectedCampaign = campaign;

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

  this.showCreateModal = true;
}

 saveCampaign(): void {
  if (!this.campaignForm.courseSessionIds || this.campaignForm.courseSessionIds.length === 0) {
    alert('Trebuie să selectezi cel puțin o sesiune de curs.');
    return;
  }

  const request$ = this.selectedCampaign
    ? this.campaignService.update(this.selectedCampaign.id, this.campaignForm)
    : this.campaignService.create(this.campaignForm);

  request$.subscribe({
    next: (res: any) => {
      if (res?.isSuccess === false) {
        alert(res?.error?.errorMessage || 'Campania nu a putut fi salvată.');
        return;
      }

      this.closeCreateModal();
      this.loadCampaigns();
    },
    error: (err) => {
      console.error(err);
      alert('Eroare la salvarea campaniei.');
    }
  });
}

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.selectedCampaign = null;
  }

onCoursesChange(): void {
  this.loadSessionsForSelectedCourses(false);
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
      }
    });
  });
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
      console.log('COURSES RESPONSE:', res);

      const data = res?.value ?? res?.data ?? res;
      this.courses = Array.isArray(data) ? data : [];

      console.log('COURSES:', this.courses);
    },
    error: (err) => {
      console.error('Eroare cursuri:', err);
      this.courses = [];
    }
  });
}

async toggleActive(campaign: any): Promise<void> {
  if (!campaign.isActive) {
    this.selectedCampaign = campaign;
    this.tempEndDate = '';
    this.showEndDateModal = true;
    return;
  }

  const confirmed = await this.confirmService.open(
    'Dezactivare campanie',
    'Ești sigur că vrei să dezactivezi această campanie?'
  );

  if (!confirmed) return;

  this.campaignService.toggleActive(campaign.id, null).subscribe({
    next: (res: any) => {
      if (res?.isSuccess === false) {
        this.snackbar.showError(res.error?.errorMessage || 'Campania nu a putut fi dezactivată.');
        return;
      }

      this.snackbar.showSuccess('Campania a fost dezactivată cu succes.');
      this.loadCampaigns();
    },
    error: () => {
      this.snackbar.showError('Eroare la dezactivarea campaniei.');
    }
  });
}
confirmActivate(): void {
  if (!this.tempEndDate) {
    alert('Selectează data de final.');
    return;
  }

  if (this.tempEndDate < this.today) {
    alert('Data trebuie să fie în viitor.');
    return;
  }

  this.campaignService.toggleActive(
    this.selectedCampaign.id,
    this.tempEndDate
  ).subscribe({
    next: () => {
      this.showEndDateModal = false;
      this.loadCampaigns();
    }
  });
}
async deleteCampaign(id: number): Promise<void> {
  const confirmed = await this.confirmService.open(
    'Ștergere campanie',
    'Ești sigur că vrei să ștergi această campanie?'
  );

  if (!confirmed) return;

  this.campaignService.delete(id).subscribe({
    next: (res: any) => {
      if (res?.isSuccess === false) {
        this.snackbar.showError(res.error?.errorMessage || 'Campania nu a putut fi ștearsă.');
        return;
      }

      this.snackbar.showSuccess('Campania a fost ștearsă cu succes.');
      this.loadCampaigns();
    },
    error: () => {
      this.snackbar.showError('Eroare la ștergerea campaniei.');
    }
  });
}

  onSearchChange(): void {
    this.filters.Search = this.searchTerm;
    this.filters.Page = 1;
    this.loadCampaigns();
  }

  onFilterChange(): void {
    this.filters.Page = 1;
    this.loadCampaigns();
  }

  sortBy(column: string): void {
    if (this.filters.SortBy === column) {
      this.filters.Desc = !this.filters.Desc;
    } else {
      this.filters.SortBy = column;
      this.filters.Desc = true;
    }

    this.loadCampaigns();
  }

  changePage(page: number): void {
    this.filters.Page = page;
    this.loadCampaigns();
  }

  getDiscountLabel(type: number, value: number): string {
    return type === 1 ? `${value}%` : `${value} lei`;
  }

  getScopeLabel(scope: number): string {
    switch (scope) {
      case 0: return 'Total';
      case 1: return 'Pachet';
      case 2: return 'Abonament';
      default: return '-';
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

  toggleCourse(courseId: number, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked;

  if (checked) {
    this.campaignForm.courseIds.push(courseId);
  } else {
    this.campaignForm.courseIds = this.campaignForm.courseIds.filter((id: number) => id !== courseId);
  }

  this.campaignForm.courseSessionIds = [];
  this.loadSessionsForSelectedCourses();
}

toggleSession(sessionId: number, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked;

  if (checked) {
    this.campaignForm.courseSessionIds.push(sessionId);
  } else {
    this.campaignForm.courseSessionIds = this.campaignForm.courseSessionIds.filter((id: number) => id !== sessionId);
  }
}
}