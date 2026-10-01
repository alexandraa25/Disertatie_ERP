import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivityLog } from '../../models/activity-log.model';

@Component({
  selector: 'app-student-activity-panel', standalone: true, imports: [CommonModule],
  templateUrl: './student-activity-panel.component.html', styleUrls: ['./student-activity-panel.component.css']
})
export class StudentActivityPanelComponent {
  @Input() activityLogs: ActivityLog[] = [];
  @Input() pageSize = 10;
  @Input() activityPage = 1;
  @Output() activityPageChange = new EventEmitter<number>();
  readonly objectKeys = Object.keys;
  totalPages(list: ActivityLog[]): number { return Math.max(1, Math.ceil(list.length / this.pageSize)); }
  get pagedActivityLogs(): ActivityLog[] {
    const start = (this.activityPage - 1) * this.pageSize;
    return this.activityLogs.slice(start, start + this.pageSize);
  }
}
