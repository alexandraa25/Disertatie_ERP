import { Component, OnInit } from '@angular/core';
import { MarketingCampaignService } from '../../services/marketing.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ConfirmService } from '../../services/confirm.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { CreateCampaignComponent } from '../create-campaign/create-campaign.component';
import { SendNewsletterComponent } from '../send-newsletter/send-newsletter.component';
import { EmailLogsComponent } from '../email-logs/email-logs.component';

@Component({
  selector: 'app-marketing-campaigns',
  standalone: true,
  imports: [
    FormsModule,
    CommonModule,
    ConfirmCustomModalComponent,
    CreateCampaignComponent, 
    SendNewsletterComponent,
     EmailLogsComponent
  ],
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

  showNewsletterModal = false;
newsletterCampaign: any = null;

showEmailLogsModal = false;
emailLogsCampaign: any = null;

  today: string = new Date().toISOString().split('T')[0];

  filters = {
    Search: '',
    IsActive: '',
    Scope: '',
    PeriodStatus: '',
    SortBy: 'startDate',
    Desc: true,
    Page: 1,
    PageSize: 10
  };

  showEndDateModal = false;
  tempEndDate: string = '';

  constructor(
    private campaignService: MarketingCampaignService,
    public confirmService: ConfirmService,
    private snackbar: SnackbarService
  ) {}

  ngOnInit(): void {
    this.loadCampaigns();
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
      error: () => {
        this.loading = false;
      }
    });
  }

  openCreateModal(): void {
    this.selectedCampaign = null;
    this.showCreateModal = true;
  }

  openEditModal(campaign: any): void {
    this.selectedCampaign = campaign;
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.selectedCampaign = null;
  }

  onCampaignSaved(): void {
    this.closeCreateModal();
    this.loadCampaigns();
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
          this.snackbar.showError(
            res.error?.errorMessage || 'Campania nu a putut fi dezactivată.'
          );
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
      this.snackbar.showError('Selectează data de final.');
      return;
    }

    if (this.tempEndDate < this.today) {
      this.snackbar.showError('Data trebuie să fie în viitor.');
      return;
    }

    this.campaignService.toggleActive(
      this.selectedCampaign.id,
      this.tempEndDate
    ).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Campania nu a putut fi activată.'
          );
          return;
        }

        this.snackbar.showSuccess('Campania a fost activată cu succes.');
        this.showEndDateModal = false;
        this.selectedCampaign = null;
        this.loadCampaigns();
      },
      error: () => {
        this.snackbar.showError('Eroare la activarea campaniei.');
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
          this.snackbar.showError(
            res.error?.errorMessage || 'Campania nu a putut fi ștearsă.'
          );
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
      case 0:
        return 'Total';
      case 1:
        return 'Pachet';
      case 2:
        return 'Abonament';
      default:
        return '-';
    }
  }

  getCampaignStatusLabel(status: string): string {
    switch (status) {
      case 'Active':
        return 'Activă';
      case 'Inactive':
        return 'Inactivă';
      case 'Expired':
        return 'Expirată';
      case 'Scheduled':
        return 'Programată';
      default:
        return '-';
    }
  }

  getCampaignStatusClass(status: string): string {
    switch (status) {
      case 'Active':
        return 'approved';
      case 'Inactive':
        return 'cancelled';
      case 'Expired':
        return 'rejected';
      case 'Scheduled':
        return 'pending';
      default:
        return 'cancelled';
    }
  }

  openNewsletterModal(campaign: any): void {
  this.newsletterCampaign = campaign;
  this.showNewsletterModal = true;
}

closeNewsletterModal(): void {
  this.showNewsletterModal = false;
  this.newsletterCampaign = null;
}

onNewsletterSent(): void {
  this.closeNewsletterModal();
}

openEmailLogsModal(campaign: any): void {
  this.emailLogsCampaign = campaign;
  this.showEmailLogsModal = true;
}

closeEmailLogsModal(): void {
  this.showEmailLogsModal = false;
  this.emailLogsCampaign = null;
}
}