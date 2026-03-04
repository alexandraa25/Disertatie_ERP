import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from '../services/dashboard.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

  stats: any;
  loading = true;

  constructor(private service: DashboardService) {}

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.service.getDashboard().subscribe(res => {

      this.stats = res.value;
      this.loading = false;

      setTimeout(() => {
        this.initChart();
      }, 200);
    });
  }

  initChart() {

    const months = this.stats.monthlyRevenue.map((x: any) =>
      `${x.month}/${x.year}`
    );

    const revenue = this.stats.monthlyRevenue.map((x: any) =>
      x.revenue
    );

    new Chart('revenueChart', {
      type: 'line',
      data: {
        labels: months,
        datasets: [{
          label: 'Venit Lunar',
          data: revenue,
          borderWidth: 3,
          tension: 0.4
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: true }
        }
      }
    });
  }
}