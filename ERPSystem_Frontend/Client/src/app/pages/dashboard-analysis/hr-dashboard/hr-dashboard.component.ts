import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../services/dashboard.service';

@Component({
  selector: 'app-hr-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './hr-dashboard.component.html',
  styleUrls: ['./hr-dashboard.component.css']
})
export class HrDashboardComponent implements OnInit {
  loading = false;
  data: any;

  constructor(private dashboardService: DashboardService) {}

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
}