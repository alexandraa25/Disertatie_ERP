import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StudentsService } from '../../services/students.service';
import { StudentListItemDto } from '../../models/student.model';
import { RouterModule } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { StudentFormComponent } from '../student-form/student-form.component';
import { Router } from '@angular/router';
import { RomanianDayPipe } from '../../../components/pipes/romanian-day.pipe';


@Component({
  selector: 'app-students',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, RomanianDayPipe ],
  templateUrl: './students.component.html',
  styleUrls: ['./students.component.css']
})

export class StudentsComponent implements OnInit {
  q = '';
  loading = true;
  page = 1;
  pageSize = 20;
  total = 0;
  sortBy: 'createdAt' | 'fullName' = 'createdAt';
  sortDir: 'asc' | 'desc' = 'desc';
  onlyRecent = false;
  recentDays = 30;
  items: StudentListItemDto[] = [];
  searchTimeout: any;

  sessions: any[] = [];
selectedSessionId: number | null = null;
  
  constructor(private students: StudentsService, private dialog: MatDialog, private router: Router) { }

  ngOnInit(): void {
    this.loadSessions();
    this.load();
    
  }

  next(): void {
    if (this.page * this.pageSize >= this.total) return;
    this.page++;
    this.load();
  }

  prev(): void {
    if (this.page <= 1) return;
    this.page--;
    this.load();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }


onSearchChange() {
  clearTimeout(this.searchTimeout);

  this.searchTimeout = setTimeout(() => {
    this.page = 1;
    this.load();
  }, 400); // 🔥 debounce
}

  isNew(createdAtUtc: string): boolean {
    const created = new Date(createdAtUtc).getTime();
    const now = Date.now();
    const days = (now - created) / (1000 * 60 * 60 * 24);
    return days <= 7;
  }

  load(): void {
    this.loading = true;
    this.students.list(this.q, this.page, this.pageSize, this.sortBy, this.sortDir, this.onlyRecent, this.recentDays, this.selectedSessionId  )
      .subscribe({
        next: (res: any) => {
          const data = res?.value ?? res?.data ?? res;
          this.items = data?.items ?? [];
          this.total = data?.total ?? 0;
          this.loading = false;
        },
        error: () => { this.loading = false; alert('Eroare la încărcare elevi'); }
      });
  }

  export(): void {
    this.students.exportExcel(this.q, this.sortBy, this.sortDir, this.onlyRecent, this.recentDays,  this.selectedSessionId)
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `students_${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: () => alert('Eroare export Excel')
      });
  }

  toggleSortDirection(): void {
    this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    this.page = 1;
    this.load();
  }

  setSort(field: 'fullName' | 'createdAt'): void {
    if (this.sortBy === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = field;
      this.sortDir = 'asc';
    }

    this.page = 1;
    this.load();
  }

  sortIcon(field: 'fullName' | 'createdAt'): string {
    if (this.sortBy !== field) return '';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  loadSessions() {
  this.students.getSessions().subscribe((res: any) => {
    this.sessions = res?.value ?? res ?? [];
  });
}

  openCreate() {

    const dialogRef = this.dialog.open(StudentFormComponent, {
      width: '720px',
      maxWidth: '92vw',
      panelClass: 'student-dialog'

    });
  }

  openEdit(id: number) {

    if (this.dialog.openDialogs.length > 0) {
      return;
    }

    const dialogRef = this.dialog.open(StudentFormComponent, {
      width: '720px',
      maxWidth: '92vw',
      panelClass: 'student-dialog',
      data: { id }   
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.load();
      }
    });
  }

  openDetails(id: number) {
    this.router.navigate(['/students', id]);
  }

}
