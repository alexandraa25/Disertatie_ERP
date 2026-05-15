import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from '../../services/dashboard.service';
import {OverviewDashboard} from "../../models/dashboard.models"

@Component({
  selector: 'app-overview-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './overview-dashboard.component.html',
  styleUrls: ['./overview-dashboard.component.css']
})
export class OverviewDashboardComponent implements OnInit {
  loading = false;

  data: OverviewDashboard | null = null;

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;

    this.dashboardService.getOverview().subscribe({
      next: (res) => {
  console.log(res);

  this.data = res.value;

  this.loading = false;
},
      error: () => {
        this.loading = false;
      }
    });
  }
}