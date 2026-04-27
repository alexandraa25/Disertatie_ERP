import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketingCampaignService } from '../../services/marketing.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-email-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './email-logs.component.html',
  styleUrl: './email-logs.component.css'
})
export class EmailLogsComponent  implements OnInit {

   @Input() campaign: any;

  @Output() closed = new EventEmitter<void>();

  logs: any[] = [];
  selectedLog: any = null;

  loading = false;
  loadingDetails = false;

  search = '';
  page = 1;
  pageSize = 10;
  total = 0;
  totalPages = 1;

  constructor(
    private campaignService: MarketingCampaignService,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    if (!this.campaign?.id) return;

    this.loading = true;

    const dto = {
      type: 'CampaignNewsletter',
      referenceId: this.campaign.id,
      search: this.search,
      page: this.page,
      pageSize: this.pageSize
    };

    this.campaignService.getEmailLogs(dto).subscribe({
      next: (res: any) => {
        const value = res?.value ?? res?.data ?? res;

        this.logs = value?.items ?? [];
        this.total = value?.total ?? 0;
        this.totalPages = Math.ceil(this.total / this.pageSize);

        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Nu s-a putut încărca istoricul emailurilor.');
      }
    });
  }

  onSearchChange(): void {
    this.page = 1;
    this.loadLogs();
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages) return;

    this.page = page;
    this.loadLogs();
  }

  openDetails(log: any): void {
    this.loadingDetails = true;
    this.selectedLog = null;

    this.campaignService.getEmailLogDetails(log.id).subscribe({
      next: (res: any) => {
        this.selectedLog = res?.value ?? res?.data ?? res;
        this.loadingDetails = false;
      },
      error: () => {
        this.loadingDetails = false;
        this.snackbar.showError('Nu s-au putut încărca detaliile emailului.');
      }
    });
  }

  backToList(): void {
    this.selectedLog = null;
  }

  close(): void {
    this.closed.emit();
  }

  getStatusLabel(log: any): string {
    if (!log) return '-';

    if (log.failedCount === 0 && log.sentCount > 0) {
      return 'Trimis';
    }

    if (log.sentCount > 0 && log.failedCount > 0) {
      return 'Parțial';
    }

    return 'Eșuat';
  }

  getStatusClass(log: any): string {
    if (!log) return 'failed';

    if (log.failedCount === 0 && log.sentCount > 0) {
      return 'sent';
    }

    if (log.sentCount > 0 && log.failedCount > 0) {
      return 'partial';
    }

    return 'failed';
  }
}

