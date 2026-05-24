import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../services/dashboard.service';
import { PowerBiModalComponent } from '../power-bi-modal/power-bi-modal.component';

@Component({
  selector: 'app-hr-dashboard',
  standalone: true,
  imports: [CommonModule, PowerBiModalComponent],
  templateUrl: './hr-dashboard.component.html',
  styleUrls: ['./hr-dashboard.component.css']
})
export class HrDashboardComponent implements OnInit {
  loading = false;
  data: any;

  isPowerBiOpen = false;
  powerBiTitle = '';
  powerBiReports: any[] = [];
  currentReportIndex = 0;

  hrReports = [
    {
      title: 'Raport Resurse Umane',
      url: 'https://app.powerbi.com/view?r=eyJrIjoiYWNlOTFmMzItN2E4Zi00Y2Q1LWJlNjMtYWYzZjVkODhhNDIzIiwidCI6IjdlMTg1ZWM2LWFjODYtNGVmNi1iM2FiLWYwZTIyNGEyMWNkNiIsImMiOjh9'
    }
  ];

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    this.loading = true;

    this.dashboardService.getHr().subscribe({
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