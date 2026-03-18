import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityLogService } from '../../services/activity-log.service';
import { ActivityFilters, ActivityLog,  } from '../../models/activity-log.model';
import { NgSelectModule } from '@ng-select/ng-select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';


@Component({
  selector: 'app-admin-activity',
  standalone: true,
   imports: [CommonModule, FormsModule, NgSelectModule, MatDatepickerModule, MatFormFieldModule, MatInputModule, MatNativeDateModule],
  templateUrl: './admin-activity.component.html',
  styleUrls: ['./admin-activity.component.scss']
})
export class AdminActivityComponent implements OnInit {

  activityLogs: ActivityLog[] = [];
  groupedLogs: { label: string; logs: ActivityLog[] }[] = [];

  entities: string[] = [];
actions: string[] = [];
users: string[] = [];

 filters: ActivityFilters = {
  entity: [],        // 🔥 array
  action: [],
  performedBy: [],
  from: '',
  to: '',
  page: 1,
  pageSize: 20
};

  loading = false;

  constructor(private activityService: ActivityLogService) {}

  ngOnInit(): void {
    this.loadLogs();
    this.loadFilters();
  }

  loadLogs() {
    this.loading = true;

    this.activityService.getAllActivity(this.filters)
      .subscribe({
        next: (res: any) => {
          this.activityLogs = res.items;
          this.groupLogs(this.activityLogs);
          this.loading = false;
        },
        error: () => this.loading = false
      });
  }

  groupLogs(logs: ActivityLog[]) {
    const groups: { [key: string]: ActivityLog[] } = {};

    logs.forEach(log => {
      const date = new Date(log.createdAtUtc);
      const label = this.getGroupLabel(date);

      if (!groups[label]) {
        groups[label] = [];
      }

      groups[label].push(log);
    });

    this.groupedLogs = Object.keys(groups).map(label => ({
      label,
      logs: groups[label]
    }));
  }

  getGroupLabel(date: Date): string {
    const today = new Date();

    if (date.toDateString() === today.toDateString())
      return 'Azi';

    const yesterday = new Date();
    yesterday.setDate(today.getDate() - 1);

    if (date.toDateString() === yesterday.toDateString())
      return 'Ieri';

    return date.toLocaleDateString();
  }

  loadFilters() {
  this.activityService.getFilters().subscribe(res => {
    this.entities = res.entities;
    this.actions = res.actions;
    this.users = res.users;
  });

  
}

clearDate() {
  this.filters.from = '';
  this.filters.to = '';
  this.loadLogs();
}


// setPreset(type: string) {
//   const today = new Date();

//   let from: Date;
//   let to: Date = new Date(today);

//   switch (type) {
//     case 'today':
//       from = new Date(today);
//       break;

//     case 'yesterday':
//       from = new Date(today);
//       from.setDate(today.getDate() - 1);
//       to = new Date(from);
//       break;

//     case 'week':
//       from = new Date(today);
//       from.setDate(today.getDate() - 7);
//       break;

//     case 'month':
//       from = new Date(today);
//       from.setMonth(today.getMonth() - 1);
//       break;

//     default:
//       return;
//   }

//   this.filters.from = from.toISOString();
//   this.filters.to = to.toISOString();

//   this.loadLogs();
// }
}

