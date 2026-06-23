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
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { AuthService } from '../../services/auth.service';



@Component({
  selector: 'app-students',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, RomanianDayPipe, ConfirmCustomModalComponent],
  templateUrl: './students.component.html',
  styleUrls: ['./students.component.css']
})

export class StudentsComponent implements OnInit {
  q = '';
  loading = true;
  page = 1;
  pageSize = 6;
  total = 0;
  sortBy: 'createdAt' | 'fullName' = 'createdAt';
  sortDir: 'asc' | 'desc' = 'desc';
  onlyRecent = false;
  recentDays = 30;
  items: StudentListItemDto[] = [];
  searchTimeout: any;

  sessions: any[] = [];
  selectedSessionId: number | null = null;

statusFilter = '';
deleteFilter = 'notDeleted';
isExporting = false;

  canWrite = false;
  canExport = false;

  constructor(private students: StudentsService, private dialog: MatDialog, private router: Router, private snackbar: SnackbarService, private confirmService: ConfirmService, private auth: AuthService) {
    this.canWrite = this.auth.hasRole(['Admin', 'Manager', 'Secretary']);
    this.canExport = this.auth.hasRole(['Admin', 'Manager', 'Secretary', 'Teacher']);
  }

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
    }, 400); 
  }

  isNew(createdAtUtc: string): boolean {
    const created = new Date(createdAtUtc).getTime();
    const now = Date.now();
    const days = (now - created) / (1000 * 60 * 60 * 24);
    return days <= 7;
  }

  load(): void {
    this.loading = true;
    this.students.list(this.q, this.page, this.pageSize, this.sortBy, this.sortDir, this.onlyRecent, this.recentDays, this.selectedSessionId,  this.statusFilter,  this.deleteFilter )
      .subscribe({
        next: (res: any) => {
          const data = res?.value ?? res?.data ?? res;
          this.items = data?.items ?? [];
          this.total = data?.total ?? 0;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.snackbar.showError('Eroare la încărcare elevi.', 2500);
        }
      });
  }

 export(): void {

  if (this.isExporting) return;

  this.isExporting = true;

  this.students.exportExcel(
    this.q,
    this.sortBy,
    this.sortDir,
    this.onlyRecent,
    this.recentDays,
    this.selectedSessionId,
    this.statusFilter,
    this.deleteFilter
  ).subscribe({
    next: (blob) => {

      const url = window.URL.createObjectURL(blob);

      const a = document.createElement('a');
      a.href = url;

      a.download =
        `students_${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.xlsx`;

      a.click();

      window.URL.revokeObjectURL(url);

      this.snackbar.showSuccess('Exportul Excel a fost generat.', 1800);

      this.isExporting = false;
    },

    error: () => {

      this.isExporting = false;

      this.snackbar.showError('Eroare export Excel.', 2500);
    }
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

  loadSessions(): void {
    this.students.getSessions().subscribe({
      next: (res: any) => {
        this.sessions = res?.value ?? res ?? [];
      },
      error: () => {
        this.snackbar.showError('Nu am putut încărca lista de sesiuni.', 2500);
      }
    });
  }

  openCreate(): void {
    const dialogRef = this.dialog.open(StudentFormComponent, {
      width: '720px',
      maxWidth: '92vw',
      panelClass: 'student-dialog'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackbar.showSuccess('Elev adăugat cu succes.', 1800);
        this.load();
      }
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

 async toggleStudent(student: any): Promise<void> {
  const action = student.isActive ? 'dezactivezi' : 'activezi';

  const confirmed = await this.confirmService.confirm(
    `Sigur vrei să ${action} elevul "${student.fullName}"?`,
    'Confirmare'
  );

  if (!confirmed) return;

  this.students.toggleStatus(student.id).subscribe({
    next: (res: any) => {
      const updated = res?.value ?? res;
      student.isActive = updated.isActive;

      this.snackbar.showSuccess(
        student.isActive ? 'Elev activat.' : 'Elev dezactivat.',
        1800
      );
    },
    error: (err) => {
      this.snackbar.showError(
        this.getErrorMessage(err, 'Statusul elevului nu a putut fi actualizat.'),
        3000
      );
    }
  });
}

async deleteStudent(student: any): Promise<void> {
  const confirmed = await this.confirmService.confirm(
    `Sigur vrei să ștergi elevul "${student.fullName}"?`,
    'Confirmare ștergere'
  );

  if (!confirmed) return;

  this.students.delete(student.id).subscribe({
    next: (res: any) => {
      const success = res?.success ?? res?.isSuccess ?? false;

      if (!success) {
        this.snackbar.showError(
          res?.message || res?.errorMessage || 'Nu poți șterge cursantul. Are înscrieri active.',
          3000
        );
        return;
      }

      this.snackbar.showSuccess('Elev șters.', 1800);
      this.load();
    },
    error: (err) => {
      this.snackbar.showError(
        err?.error?.message ||
        err?.error?.errorMessage ||
        'Elevul nu a putut fi șters.',
        3000
      );
    }
  });
}

async restoreStudent(student: any): Promise<void> {
  const confirmed = await this.confirmService.confirm(
    `Sigur vrei să restaurezi elevul "${student.fullName}"?`,
    'Confirmare restaurare'
  );

  if (!confirmed) return;

  this.students.restore(student.id).subscribe({
    next: () => {
      this.snackbar.showSuccess('Elev restaurat cu succes.', 1800);
      this.load();
    },
    error: (err) => {
      this.snackbar.showError(
        this.getErrorMessage(err, 'Elevul nu a putut fi restaurat.'),
        3000
      );
    }
  });
}

private getErrorMessage(err: any, fallback: string): string {
  return err?.error?.message ||
         err?.error?.errorMessage ||
         err?.error?.title ||
         fallback;
}
}
