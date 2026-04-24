import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CoursesService } from '../../services/courses.service';
import { CourseListItemDto } from '../../models/course.model';
import { MatDialog } from '@angular/material/dialog';
import { CourseFormComponent } from '../course-form/course-form.component';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, startWith, switchMap } from 'rxjs/operators';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';


@Component({
  selector: 'app-courses',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ConfirmCustomModalComponent],
  templateUrl: './courses.component.html',
  styleUrls: ['./courses.component.css']
})
export class CoursesComponent implements OnInit {
  q = '';
  loading = true;
  items: CourseListItemDto[] = [];
  searchSubject = new Subject<string>();

  statusFilter = '';
  deleteStatusFilter = 'notDeleted';

  constructor(private courses: CoursesService, private dialog: MatDialog, private router: Router, private snackbar: SnackbarService, private confirmService: ConfirmService) { }

  ngOnInit(): void {
    this.searchSubject.pipe(
      startWith(this.q),
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe((term) => {
      this.q = term;
      this.loadCourses();
    });
  }

  onSearch(term: string): void {
    this.searchSubject.next(term);
  }

  loadCourses(): void {
    this.loading = true;

    this.courses.list(this.q, this.statusFilter, this.deleteStatusFilter).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.items = Array.isArray(data) ? data : [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Eroare la încărcare cursuri.', 2500);
      }
    });
  }

  onStatusFilterChange() {
    this.loadCourses();
  }

  onDeleteStatusFilterChange() {
    this.loadCourses();
  }

  openCreate() {
    const dialogRef = this.dialog.open(CourseFormComponent, {
      width: '1020px',
      panelClass: 'student-dialog'
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result) {
        this.loadCourses();
      }
    });
  }

  openEdit(id: number) {

    if (this.dialog.openDialogs.length > 0) {
      return;
    }

    const dialogRef = this.dialog.open(CourseFormComponent, {
      width: '1020px',
      panelClass: 'student-dialog',
      data: { id }
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result) {
        this.loadCourses();
      }
    });
  }

  openDetails(id: number): void {
    this.router.navigate(['/courses', id]);
  }

  async toggleCourse(course: any): Promise<void> {
    if (course.isActive && this.hasActiveSessions(course)) {
      this.snackbar.showError('Nu poți dezactiva cursul. Există sesiuni active.', 2500);
      return;
    }

    const action = course.isActive ? 'dezactivezi' : 'activezi';

    const confirmed = await this.confirmService.confirm(
      'Confirmare',
      `Sigur vrei să ${action} cursul "${course.name}"?`
    );

    if (!confirmed) return;

    this.courses.toggleCourseStatus(course.id).subscribe({
      next: (res: any) => {
        course.isActive = res.value.isActive;

        this.snackbar.showSuccess(
          course.isActive ? 'Curs activat cu succes.' : 'Curs dezactivat cu succes.',
          1800
        );
      },
      error: () => {
        this.snackbar.showError('Statusul cursului nu a putut fi actualizat.', 2500);
      }
    });
  }

  hasActiveSessions(course: any): boolean {
    return course.sessions?.some((s: any) => s.isActive) ?? false;
  }

  async deleteCourse(course: any): Promise<void> {
    if (this.hasActiveSessions(course)) {
      this.snackbar.showError('Nu poți șterge cursul. Există sesiuni active.', 2500);
      return;
    }

    const confirmed = await this.confirmService.confirm(
      'Confirmare ștergere',
      `Sigur vrei să ștergi cursul "${course.name}"? Acesta va putea fi restaurat ulterior.`
    );

    if (!confirmed) return;

    this.courses.deleteCourse(course.id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Cursul a fost mutat în coș.', 1800);
        this.loadCourses();
      },
      error: () => {
        this.snackbar.showError('Cursul nu a putut fi șters.', 2500);
      }
    });
  }

  async restoreCourse(course: any): Promise<void> {
    const confirmed = await this.confirmService.confirm(
      'Confirmare restaurare',
      `Sigur vrei să restaurezi cursul "${course.name}"?`
    );

    if (!confirmed) return;

    this.courses.restoreCourse(course.id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Cursul a fost restaurat cu succes.', 1800);
        this.loadCourses();
      },
      error: () => {
        this.snackbar.showError('Cursul nu a putut fi restaurat.', 2500);
      }
    });
  }
}
