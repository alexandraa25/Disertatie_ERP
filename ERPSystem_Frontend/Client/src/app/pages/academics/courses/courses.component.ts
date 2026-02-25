import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CoursesService } from '../../services/courses.service';
import { CourseListItemDto } from '../../models/course.model';
import { MatDialog } from '@angular/material/dialog';
import { CourseFormComponent } from '../course-form/course-form.component';
import { Router } from '@angular/router';

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
 // dialog: any;

  constructor(private courses: CoursesService, private dialog : MatDialog, private router: Router) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
  this.loading = true;
  this.courses.list(this.q).subscribe({
    next: (res: any) => {
      const data = res?.value ?? res?.data ?? res;
      this.items = Array.isArray(data) ? data : [];
      this.loading = false;
    },
    error: () => { this.loading = false; alert('Eroare la încărcare cursuri'); }
  });
}

openCreate() {
  const dialogRef = this.dialog.open(CourseFormComponent, {
    width: '720px',
    maxWidth: '92vw',
    panelClass: 'student-dialog'
  });

  dialogRef.afterClosed().subscribe((result: any) => {
    if (result) {
      this.load(); // reîncarcă lista
    }
  });
}

openEdit(id: number) {

  if (this.dialog.openDialogs.length > 0) {
    return;
  }

  const dialogRef = this.dialog.open(CourseFormComponent, {
    width: '720px',
    maxWidth: '92vw',
    panelClass: 'student-dialog',
    data: { id }
  });

  dialogRef.afterClosed().subscribe((result: any) => {
    if (result) {
      this.load();
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
    sessions: [] // nu modificăm sesiunile aici
  };

  this.courses.update(course.id, dto).subscribe({
    next: () => this.load(),
    error: () => alert('Eroare la actualizare')
  });
}
}
