import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityLogService } from '../../services/activity-log.service';
import { ActivityFilters, ActivityLog,  } from '../../models/activity-log.model';


@Component({
  selector: 'app-admin-activity',
  standalone: true,
   imports: [CommonModule, FormsModule],
  templateUrl: './admin-activity.component.html',
  styleUrls: ['./admin-activity.component.scss']
})
export class AdminActivityComponent implements OnInit {

  activityLogs: ActivityLog[] = [];
  groupedLogs: { label: string; logs: ActivityLog[] }[] = [];

  entities: string[] = [];
actions: string[] = [];
users: string[] = [];

entityDropdownOpen = false;
entitySearch = '';
actionSearch = '';
userSearch = '';

  filters: ActivityFilters = {
  entity: '',
  action: '',
  performedBy: '',
  from: '',
  to: '',
  page: 1,
  pageSize: 20
};;

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

get filteredEntities() {
  return this.entities.filter(e =>
    e.toLowerCase().includes(this.entitySearch.toLowerCase())
  );
}

toggleEntityDropdown() {
  this.entityDropdownOpen = !this.entityDropdownOpen;
}

selectEntity(value: string) {
  this.filters.entity = value;
  this.entityDropdownOpen = false;
  this.entitySearch = '';
}

get filteredActions() {
  return this.actions.filter(a =>
    a.toLowerCase().includes(this.actionSearch.toLowerCase())
  );
}

get filteredUsers() {
  return this.users.filter(u =>
    u.toLowerCase().includes(this.userSearch.toLowerCase())
  );
}
}
