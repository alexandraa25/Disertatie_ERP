import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StudentsService } from '../../services/students.service';
import { StudentDetailsDto, StudentCourseDetailsDto } from '../../models/student.model';

@Component({
  selector: 'app-student-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-details.component.html',
  styleUrls: ['./student-details.component.css']
})
export class StudentDetailsComponent implements OnInit {

  student!: StudentDetailsDto;
  loading = true;

  tabs = ['Informații', 'Cursuri', 'Financiar', 'Istoric'];
  activeTab = 'Informații';

  // 🔥 NOI
  courses: StudentCourseDetailsDto[] = [];
  totalAmount = 0;
  coursesLoaded = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private students: StudentsService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.students.get(id).subscribe({
      next: (res) => {
        this.student = res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Nu am putut încărca elevul.');
        this.router.navigate(['/students']);
      }
    });
  }

  goBack() {
    this.router.navigate(['/students']);
  }

  edit() {
    this.router.navigate(['/students/edit', this.student.id]);
  }

  // 🔥 schimbare tab corectă
  onTabChange(tab: string) {
    this.activeTab = tab;

    if (tab === 'Cursuri' && !this.coursesLoaded) {
      this.loadCourses();
    }
  }

  loadCourses() {
    this.students.getStudentCourses(this.student.id)
      .subscribe(res => {
        this.courses = res.items;
        this.totalAmount = res.totalAmount;
        this.coursesLoaded = true;
      });
  }

 getRomanianDay(day: any): string {

  const mapByNumber: Record<number, string> = {
    0: 'Duminică',
    1: 'Luni',
    2: 'Marți',
    3: 'Miercuri',
    4: 'Joi',
    5: 'Vineri',
    6: 'Sâmbătă'
  };

  if (!isNaN(day)) {
    return mapByNumber[Number(day)] ?? day;
  }

  const normalized = day?.toString().toLowerCase();

  const mapByName: Record<string, string> = {
    monday: 'Luni',
    tuesday: 'Marți',
    wednesday: 'Miercuri',
    thursday: 'Joi',
    friday: 'Vineri',
    saturday: 'Sâmbătă',
    sunday: 'Duminică'
  };

  return mapByName[normalized] ?? day;
}


createContract() {
  this.router.navigate(['/create-contract'], {
    queryParams: { studentId: this.student.id }
  });
}
}