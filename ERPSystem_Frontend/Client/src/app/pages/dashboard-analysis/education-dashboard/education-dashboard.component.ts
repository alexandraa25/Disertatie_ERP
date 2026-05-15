import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../services/dashboard.service';

@Component({
  selector: 'app-education-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './education-dashboard.component.html',
  styleUrls: ['./education-dashboard.component.css']
})
export class EducationDashboardComponent implements OnInit {
  loading = false;
  data: any;

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
}