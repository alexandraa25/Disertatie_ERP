import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../services/dashboard.service';
import { PowerBiModalComponent } from '../power-bi-modal/power-bi-modal.component';

@Component({
  selector: 'app-financial-dashboard',
  standalone: true,
  imports: [CommonModule, PowerBiModalComponent],
  templateUrl: './financial-dashboard.component.html',
  styleUrls: ['./financial-dashboard.component.css']
})
export class FinancialDashboardComponent implements OnInit {
  loading = false;
  data: any;

  isPowerBiOpen = false;
powerBiTitle = '';
powerBiReports: any[] = [];
currentReportIndex = 0;

financialReports = [
  {
    title: 'Raport financiar general',
    url: 'https://app.powerbi.com/view?r=eyJrIjoiMjU0NGU5N2YtNzIyMS00Nzk2LWI2OWQtOTk1OWU5YzYxOTI4IiwidCI6IjdlMTg1ZWM2LWFjODYtNGVmNi1iM2FiLWYwZTIyNGEyMWNkNiIsImMiOjh9&pageName=13b1eeb6e242900e0d24'
  },
  {
    title: 'Cashflow și restanțe',
    url: 'POWER_BI_URL_2'
  }
];

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loading = true;

    this.dashboardService.getFinancial().subscribe({
      next: (res) => {
        this.data = res.value;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  openPowerBiReports(title: string, reports: any[]) {
  this.powerBiTitle = title;
  this.powerBiReports = reports;
  this.currentReportIndex = 0;
  this.isPowerBiOpen = true;
}

closePowerBi() {
  this.isPowerBiOpen = false;
}

nextReport() {
  if (this.currentReportIndex < this.powerBiReports.length - 1) {
    this.currentReportIndex++;
  }
}

prevReport() {
  if (this.currentReportIndex > 0) {
    this.currentReportIndex--;
  }
}
}