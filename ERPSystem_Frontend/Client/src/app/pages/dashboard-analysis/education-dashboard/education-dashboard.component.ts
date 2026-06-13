import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../services/dashboard.service';
import { PowerBiModalComponent } from '../power-bi-modal/power-bi-modal.component';

@Component({
  selector: 'app-education-dashboard',
  standalone: true,
  imports: [CommonModule, PowerBiModalComponent],
  templateUrl: './education-dashboard.component.html',
  styleUrls: ['./education-dashboard.component.css']
})
export class EducationDashboardComponent implements OnInit {
  loading = false;
  data: any;

  
isPowerBiOpen = false;
powerBiTitle = '';
powerBiReports: any[] = [];
currentReportIndex = 0;

educationalReports = [
  {
    title: 'Raport educational general',
    url: 'https://app.powerbi.com/view?r=eyJrIjoiNDZiMzgxMjItZjhjYi00OGM2LWEyZjgtN2IyYzhhMTNlNzc4IiwidCI6IjdlMTg1ZWM2LWFjODYtNGVmNi1iM2FiLWYwZTIyNGEyMWNkNiIsImMiOjh9'
  }
];

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loading = true;

    this.dashboardService.getEducation().subscribe({
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