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


@Component({
  selector: 'app-courses',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './courses.component.html',
  styleUrls: ['./courses.component.css']
})
export class CoursesComponent implements OnInit {
  q = '';
  loading = true;
  items: CourseListItemDto[] = [];
  searchSubject = new Subject<string>();

  constructor(private courses: CoursesService, private dialog: MatDialog, private router: Router) { }

  ngOnInit(): void {
  this.searchSubject.pipe(
    startWith(''), // 🔥 emite automat la început
    debounceTime(300),
    distinctUntilChanged(),
    switchMap((term) => {
      this.loading = true;
      return this.courses.list(term);
    })
  ).subscribe({
    next: (res: any) => {
      const data = res?.value ?? res?.data ?? res;
      this.items = Array.isArray(data) ? data : [];
      this.loading = false;
    },
    error: () => {
      this.loading = false;
      alert('Eroare la încărcare cursuri');
    }
  });
}

  onSearch(term: string): void {
    this.searchSubject.next(term);
    if (!term.trim()) {
      this.items = [];
    }
  }

  openCreate() {
    const dialogRef = this.dialog.open(CourseFormComponent, {
      width: '1020px',
      panelClass: 'student-dialog'
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result) {
        this.ngOnInit();
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
        this.ngOnInit();
      }
    });
  }

  openDetails(id: number): void {
    this.router.navigate(['/courses', id]);
  }

  toggleCourse(course: any) {

    const confirmMsg = course.isActive
      ? 'Sigur vrei să dezactivezi cursul?'
      : 'Sigur vrei să activezi cursul?';

    if (!confirm(confirmMsg)) return;

    const dto = {
      name: course.name,
      description: course.description,
      price: course.price,
      isActive: !course.isActive,
      sessions: []
    };

    this.courses.update(course.id, dto).subscribe({
      next: () => this.ngOnInit(),
      error: () => alert('Eroare la actualizare')
    });
  }
}
