import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityLogService } from '../../services/activity-log.service';
import { ActivityFilters, ActivityLog, } from '../../models/activity-log.model';
import { NgSelectModule } from '@ng-select/ng-select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { DateRangePickerComponent } from '../../../components/date-range-picker/date-range-picker.component';


@Component({
  selector: 'app-admin-activity',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule, MatDatepickerModule, MatFormFieldModule, MatInputModule, MatNativeDateModule, DateRangePickerComponent],
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
    entity: [],
    action: [],
    performedBy: [],
    from: null as Date | null,   
    to: null as Date | null,     
    page: 1,
    pageSize: 10
  };

  loading = false;

  totalCount = 0;
  totalPages = 0;

  constructor(private activityService: ActivityLogService) { }

  ngOnInit(): void {
    this.loadLogs();
    this.loadFilters();
  }

  loadLogs() {
    this.loading = true;

    const payload = {
      ...this.filters,
      from: this.filters.from ? this.filters.from.toISOString() : null,
      to: this.filters.to ? this.filters.to.toISOString() : null
    };

    this.activityService.getAllActivity(payload)
      .subscribe({
        next: (res: any) => {
          this.activityLogs = res.items;

          this.totalCount = res.total;
          this.totalPages = Math.ceil(this.totalCount / this.filters.pageSize);
          console.log('Pages:', res.total)
          console.log('totalPages:', this.totalPages);
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
  onFilterChange() {
    this.filters.page = 1;
    this.loadLogs();
  }
  clearDate() {
    this.filters.from = null;
    this.filters.to = null;
    this.loadLogs();
  }
  onDateChange(range: any) {
    this.filters.from = range.from;
    this.filters.to = range.to;
    this.filters.page = 1;
    this.loadLogs();
  }


  nextPage() {
    if (this.filters.page < this.totalPages) {
      this.filters.page++;
      this.loadLogs();
    }
  }

  prevPage() {
    if (this.filters.page > 1) {
      this.filters.page--;
      this.loadLogs();
    }
  }

  goToPage(page: number) {
    this.filters.page = page;
    this.loadLogs();
  }
}

