import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CoursesService } from '../../services/courses.service';
import {
  TeacherOptionDto,
  CreateCourseDto,
  UpdateCourseDto,
  CourseSessionUpsertDto
} from '../../models/course.model';

@Component({
  selector: 'app-course-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule],
  templateUrl: './course-form.component.html',
  styleUrls: ['./course-form.component.css']
})
export class CourseFormComponent implements OnInit {

  loading = true;
  saving = false;

  courseId: number | null = null;
  isEdit = false;

  teachers: TeacherOptionDto[] = [];
  form!: FormGroup;

  days = [
    { id: 1, name: 'Luni' },
    { id: 2, name: 'Marți' },
    { id: 3, name: 'Miercuri' },
    { id: 4, name: 'Joi' },
    { id: 5, name: 'Vineri' },
    { id: 6, name: 'Sâmbătă' },
    { id: 7, name: 'Duminică' },
  ];

  constructor(
    private fb: FormBuilder,
    private courses: CoursesService,
    private dialogRef: MatDialogRef<CourseFormComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { id?: number }
  ) { }

  ngOnInit(): void {
    this.buildForm();

    // load teachers
    this.courses.teachers().subscribe({
      next: (res: any) => {
        this.teachers = res?.value ?? res ?? [];
      },
      error: () => alert('Nu pot încărca lista de profesori.')
    });

    // edit mode
    if (this.data && this.data.id !== undefined) {
      this.isEdit = true;
      this.courseId = this.data.id;
      this.loadCourse(this.courseId);
    } else {
      this.addSession(); // minim o sesiune la create
      this.loading = false;
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: [''],
      price: [null],
      //teacherUserId: ['', Validators.required],
      isActive: [true],
      sessions: this.fb.array([])
    });
  }

  private loadCourse(id: number): void {
    this.courses.get(id).subscribe({
      next: (res: any) => {

        const c = res?.value ?? res;

        this.form.patchValue({
          name: c.name,
          description: c.description ?? '',
          price: c.price ?? null,
          teacherUserId: c.teacherUserId,
          isActive: c.isActive
        });

        this.sessions.clear();

        for (const s of c.sessions) {
          this.sessions.push(this.createSessionGroup({
            id: s.id,
            dayOfWeek: s.dayOfWeek,
            startTime: s.startTime,
            endTime: s.endTime
          }));
        }

        if (this.sessions.length === 0) this.addSession();

        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Nu am putut încărca cursul.');
        this.dialogRef.close();
      }
    });
  }

  get sessions(): FormArray {
    return this.form.get('sessions') as FormArray;
  }

  createSessionGroup(data?: Partial<CourseSessionUpsertDto>): FormGroup {
    return this.fb.group({
      id: [data?.id ?? null],
      dayOfWeek: [
        data?.dayOfWeek ?? 1,
        [Validators.required, Validators.min(1), Validators.max(7)]
      ],
      startTime: [data?.startTime ?? '18:00', Validators.required],
      endTime: [data?.endTime ?? '19:00', Validators.required],
      teacherUserId: [data?.teacherUserId ?? '', Validators.required], 
      capacity: [data?.capacity ?? null], 
      unlimited: [data?.capacity ? false : true]   
    });
  }

  addSession(): void {
    this.sessions.push(this.createSessionGroup());
  }

  removeSession(index: number): void {
    if (this.sessions.length <= 1) return;
    this.sessions.removeAt(index);
  }

  save(): void {

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // 🔹 validare interval orar
    for (const g of this.sessions.controls) {
      const start = g.value.startTime as string;
      const end = g.value.endTime as string;

      if (start >= end) {
        alert('Interval orar invalid: ora de final trebuie să fie după ora de început.');
        return;
      }
    }

    /// 🔹 validare duplicate identice
    if (!this.validateDuplicateSessions()) {
      alert('Există sesiuni duplicate pentru același profesor și interval.');
      return;
    }

    // 🔹 validare suprapunere locală profesor
    if (this.hasLocalTeacherOverlap()) {
      alert('Profesorul are sesiuni suprapuse în acest curs.');
      return;
    }

    this.saving = true;

    const sessionsPayload: CourseSessionUpsertDto[] =
      this.sessions.value.map((x: any) => ({
        id: x.id ?? null,
        dayOfWeek: Number(x.dayOfWeek),
        startTime: x.startTime,
        endTime: x.endTime,
        teacherUserId: x.teacherUserId, 
        capacity: x.unlimited ? null : Number(x.capacity)
      }));

    if (!this.isEdit) {

      const dto: CreateCourseDto = {
        name: this.form.value.name,
        description: this.form.value.description || null,
        price: this.form.value.price ?? null,
        sessions: sessionsPayload
      };

      this.courses.create(dto).subscribe({
        next: () => {
          this.saving = false;
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.saving = false;

          const message = err?.error?.message ?? '';

          if (message.includes('suprapus')) {
            alert('Profesorul este deja programat în alt curs la același interval.');
          } else {
            alert('Eroare la creare curs.');
          }
        }
      });

    } else {

      const dto: UpdateCourseDto = {
        name: this.form.value.name,
        description: this.form.value.description || null,
        price: this.form.value.price ?? null,
        isActive: !!this.form.value.isActive,
        sessions: sessionsPayload
      };

      this.courses.update(this.courseId!, dto).subscribe({
        next: () => {
          this.saving = false;
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.saving = false;

          const message = err?.error?.message ?? '';

          if (message.includes('suprapus')) {
            alert('Profesorul este deja programat în alt curs la același interval.');
          } else {
            alert('Eroare la actualizare curs.');
          }
        }
      });
    }
  }

  cancel(): void {
    this.dialogRef.close();
  }

  c(name: string) {
    return this.form.controls[name];
  }

  private validateDuplicateSessions(): boolean {

    const seen = new Set<string>();

    for (const g of this.sessions.controls) {

      const day = g.value.dayOfWeek;
      const start = g.value.startTime;
      const end = g.value.endTime;
      const teacher = g.value.teacherUserId;

      const key = `${day}-${start}-${end}-${teacher}`;

      if (seen.has(key)) {
        return false; // duplicat găsit
      }

      seen.add(key);
    }

    return true;
  }

  private hasLocalTeacherOverlap(): boolean {

    const sessions = this.sessions.value;

    for (let i = 0; i < sessions.length; i++) {
      for (let j = i + 1; j < sessions.length; j++) {

        const a = sessions[i];
        const b = sessions[j];

        if (
          a.teacherUserId === b.teacherUserId &&
          a.dayOfWeek === b.dayOfWeek
        ) {
          if (a.startTime < b.endTime && b.startTime < a.endTime) {
            return true;
          }
        }
      }
    }

    return false;
  }

  onToggleUnlimited(index: number): void {

  const group = this.sessions.at(index);

  if (group.value.unlimited) {
    group.patchValue({ capacity: null });
  } else {
    group.patchValue({ capacity: 1 });
  }
}
}