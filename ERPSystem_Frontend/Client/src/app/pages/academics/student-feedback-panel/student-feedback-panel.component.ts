import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-student-feedback-panel', standalone: true, imports: [CommonModule],
  templateUrl: './student-feedback-panel.component.html',
  styleUrls: ['./student-feedback-panel.component.css']
})
export class StudentFeedbackPanelComponent {
  @Input() studentTab: 'evaluations' | 'analytics' = 'evaluations';
  @Input() studentAnalytics: any = null;
  @Input() loadingStudentAnalytics = false;
  @Input() studentEvaluations: any[] = [];
  @Input() loadingEvaluations = false;
  @Input() pageSize = 10;
  @Input() evaluationsPage = 1;
  @Output() tabChange = new EventEmitter<'evaluations' | 'analytics'>();
  @Output() evaluationsPageChange = new EventEmitter<number>();
  setStudentTab(tab: 'evaluations' | 'analytics'): void { this.tabChange.emit(tab); }
  totalPages(list: any[]): number { return Math.max(1, Math.ceil(list.length / this.pageSize)); }
  get pagedStudentEvaluations(): any[] {
    const start = (this.evaluationsPage - 1) * this.pageSize;
    return this.studentEvaluations.slice(start, start + this.pageSize);
  }
  getRiskLabel(level: string): string {
    switch (level) {
      case 'high': return 'Ridicat'; case 'medium': return 'Mediu'; case 'low': return 'Scăzut';
      default: return 'Necunoscut';
    }
  }
}
