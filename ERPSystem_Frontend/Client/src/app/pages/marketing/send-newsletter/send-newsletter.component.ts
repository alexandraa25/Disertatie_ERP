import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketingCampaignService } from '../../services/marketing.service';
import { StudentsService } from '../../services/students.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-send-newsletter',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './send-newsletter.component.html',
  styleUrl: './send-newsletter.component.css'
})
export class SendNewsletterComponent implements OnInit{

   @Input() campaign: any;

  @Output() closed = new EventEmitter<void>();
  @Output() sent = new EventEmitter<void>();

  recipientMode: 'active' | 'inactive' | 'all' | 'manual' = 'active';

  sending = false;

  searchTerm = '';
studentStatus: 'all' | 'active' | 'inactive' = 'all';

selectedSessionId: number | null = null;

students: any[] = [];
selectedStudentIds: number[] = [];

studentsPage = 1;
studentsPageSize = 20;
studentsTotal = 0;
studentsTotalPages = 1;

loadingStudents = false;
searchTimeout: any;

subject = '';
htmlContent = '';
loadingTemplate = false;

  constructor(
    private campaignService: MarketingCampaignService,
    private studentsService: StudentsService,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
     this.loadNewsletterTemplate();
    this.loadStudents();
  }

 loadStudents(): void {
  this.loadingStudents = true;

  const params: any = {
    q: this.searchTerm || '',
    page: this.studentsPage,
    pageSize: this.studentsPageSize,
    sortBy: 'fullName',
    sortDir: 'asc'
  };

  if (this.selectedSessionId) {
    params.sessionId = this.selectedSessionId;
  }

  this.studentsService.getStudents(params).subscribe({
    next: (res: any) => {
      const value = res?.value ?? res?.data ?? res;

      let items = value?.items ?? [];

      if (this.studentStatus === 'active') {
        items = items.filter((x: any) => x.isActive);
      }

      if (this.studentStatus === 'inactive') {
        items = items.filter((x: any) => !x.isActive);
      }

      this.students = items;
      this.studentsTotal = value?.total ?? items.length;
      this.studentsTotalPages = Math.ceil(this.studentsTotal / this.studentsPageSize);

      this.loadingStudents = false;
    },
    error: () => {
      this.students = [];
      this.loadingStudents = false;
      this.snackbar.showError('Nu s-au putut încărca cursanții.');
    }
  });
}

  onRecipientModeChange(): void {
    this.selectedStudentIds = [];
  }

  onStudentSearchChange(): void {
  clearTimeout(this.searchTimeout);

  this.searchTimeout = setTimeout(() => {
    this.studentsPage = 1;
    this.loadStudents();
  }, 300);
}

onStudentFilterChange(): void {
  this.studentsPage = 1;
  this.loadStudents();
}

changeStudentsPage(page: number): void {
  if (page < 1 || page > this.studentsTotalPages) return;

  this.studentsPage = page;
  this.loadStudents();
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

selectVisibleStudents(): void {
  const visibleIds = this.students.map(x => x.id);

  this.selectedStudentIds = [
    ...new Set([...this.selectedStudentIds, ...visibleIds])
  ];
}

clearVisibleStudents(): void {
  const visibleIds = this.students.map(x => x.id);

  this.selectedStudentIds = this.selectedStudentIds.filter(
    id => !visibleIds.includes(id)
  );
}

  selectAllStudents(): void {
    this.selectedStudentIds = this.students.map(x => x.id);
  }

  clearSelectedStudents(): void {
    this.selectedStudentIds = [];
  }

  clearAllSelectedStudents(): void {
  this.selectedStudentIds = [];
}

  sendNewsletter(): void {
    if (!this.campaign?.id) {
      this.snackbar.showError('Campania nu este validă.');
      return;
    }

    if (this.recipientMode === 'manual' && this.selectedStudentIds.length === 0) {
      this.snackbar.showError('Selectează cel puțin un cursant.');
      return;
    }

    if (!this.subject.trim()) {
  this.snackbar.showError('Subiectul emailului este obligatoriu.');
  return;
}

if (!this.htmlContent.trim()) {
  this.snackbar.showError('Conținutul emailului este obligatoriu.');
  return;
}

    const dto = {
  campaignId: this.campaign.id,
  recipientMode: this.recipientMode,
  studentIds: this.recipientMode === 'manual' ? this.selectedStudentIds : [],
  subject: this.subject,
  htmlContent: this.htmlContent
};

    this.sending = true;

    this.campaignService.sendNewsletter(dto).subscribe({
      next: (res: any) => {
        this.sending = false;

        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Newsletterul nu a putut fi trimis.'
          );
          return;
        }

        this.snackbar.showSuccess('Newsletterul a fost trimis cu succes.');
        this.sent.emit();
      },
      error: () => {
        this.sending = false;
        this.snackbar.showError('Eroare la trimiterea newsletterului.');
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  getDiscountLabel(type: number, value: number): string {
    return type === 1 ? `${value}%` : `${value} lei`;
  }

loadNewsletterTemplate(): void {
  if (!this.campaign?.id) return;

  this.loadingTemplate = true;

  this.campaignService.getNewsletterTemplate(this.campaign.id).subscribe({
    next: (res: any) => {
      const data = res?.value ?? res?.data ?? res;

      this.subject = data?.subject ?? '';
      this.htmlContent = data?.htmlContent ?? '';

      this.loadingTemplate = false;
    },
    error: () => {
      this.loadingTemplate = false;
      this.snackbar.showError('Nu s-a putut încărca template-ul emailului.');
    }
  });
}

formatDate(date: string): string {
  if (!date) return '-';

  return new Date(date).toLocaleDateString('ro-RO');
}

get campaignSessions(): any[] {
  return this.campaign?.courseSessions ?? [];
}

}
