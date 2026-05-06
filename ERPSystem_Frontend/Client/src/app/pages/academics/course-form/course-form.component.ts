import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CoursesService } from '../../services/courses.service';
import { TeacherOptionDto, CreateCourseDto, UpdateCourseDto, CourseSessionUpsertDto, CourseSessionFormModel } from '../../models/course.model';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

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
    private snackbar: SnackbarService,
    private dialogRef: MatDialogRef<CourseFormComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { id?: number }
  ) { }

  ngOnInit(): void {
    this.buildForm();

    this.courses.teachers().subscribe({
      next: (res: any) => {
        this.teachers = res?.value ?? res ?? [];
      },
      error: () => this.snackbar.showError('Nu pot încărca lista de profesori.', 2500)
    });

    if (this.data && this.data.id !== undefined) {
      this.isEdit = true;
      this.courseId = this.data.id;
      this.loadCourse(this.courseId);
    } else {
      this.addSession();
      this.loading = false;
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: [''],
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
          isActive: c.isActive
        });

        this.sessions.clear();

        for (const s of c.sessions) {
          this.sessions.push(this.createSessionGroup({
            id: s.id,
            dayOfWeek: s.dayOfWeek,
            startTime: s.startTime,
            endTime: s.endTime,
            teacherUserId: s.teacherUserId,
            capacity: s.capacity,
            fee: s.fee,
            feeType: Number(s.feeType) as 1 | 2,
            totalSessions: s.totalSessions,
            enrolledActiveCount: s.enrolledActiveCount,
            isActive: s.isActive
          }));
        }

        if (this.sessions.length === 0) this.addSession();
        if (!c.isActive) {
          this.sessions.disable();
        }

        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Nu am putut încărca cursul.', 2500);
        this.dialogRef.close();
      }
    });
  }

  get sessions(): FormArray {
    return this.form.get('sessions') as FormArray;
  }

  createSessionGroup(data?: CourseSessionFormModel): FormGroup {
    return this.fb.group({
      id: [data?.id ?? null],

      dayOfWeek: [
        data?.dayOfWeek ?? 1,
        [Validators.required, Validators.min(1), Validators.max(7)]
      ],

      startTime: [data?.startTime ?? '18:00', Validators.required],
      endTime: [data?.endTime ?? '19:00', Validators.required],

      teacherUserId: [data?.teacherUserId ?? '', Validators.required],

      fee: [data?.fee ?? 0, [Validators.required, Validators.min(0)]],

      feeType: [data?.feeType ?? 1, Validators.required],
      totalSessions: [data?.totalSessions ?? null],

      capacity: [data?.capacity ?? null],
      unlimited: [data?.capacity == null],
      enrolledActiveCount: [data?.enrolledActiveCount ?? 0],
      isActive: [data?.isActive ?? true]
    });
  }

  addSession(): void {
    this.sessions.push(this.createSessionGroup());
  }

  removeSession(index: number): void {

    if (this.sessions.length <= 1) return;

    const session = this.sessions.at(index).value;

    if (session.enrolledActiveCount > 0) {
      this.snackbar.showError('Nu poți șterge sesiunea. Există cursanți activi.', 2500);
      return;
    }

    this.sessions.removeAt(index);
  }

  save(): void {

    if (this.isEdit && !this.form.value.isActive) {
      this.snackbar.showError('Nu poți modifica un curs inactiv.', 2500);
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const sessions = this.sessions.value;

    for (const g of this.sessions.controls) {
      const start = g.value.startTime as string;
      const end = g.value.endTime as string;

      if (start >= end) {
        this.snackbar.showError('Interval orar invalid: ora de final trebuie să fie după ora de început.', 3000);
        return;
      }
    }

    if (!this.validateDuplicateSessions()) {
      this.snackbar.showError('Există sesiuni duplicate pentru același profesor și interval.', 3000);
      return;
    }

    if (this.hasLocalTeacherOverlap()) {
      this.snackbar.showError('Profesorul are sesiuni suprapuse în acest curs.', 3000);
      return;
    }

    for (const s of sessions) {

      if (s.fee <= 0) {
        this.snackbar.showError('Prețul trebuie să fie mai mare decât 0.', 2500);
        return;
      }

      if (s.feeType === 1) {
        if (!s.totalSessions || s.totalSessions <= 0) {
          this.snackbar.showError('Completează numărul de ședințe pentru pachet fix.', 3000);
          return;
        }
      }

      if (s.feeType === 2 && s.totalSessions) {
        this.snackbar.showError('Abonamentul nu trebuie să aibă număr de ședințe.', 3000);
        return;
      }
    }

    this.saving = true;

    const sessionsPayload: CourseSessionUpsertDto[] = sessions.map((x: any) => ({

      id: x.id ?? null,
      dayOfWeek: Number(x.dayOfWeek),
      startTime: x.startTime,
      endTime: x.endTime,
      teacherUserId: x.teacherUserId,

      fee: Number(x.fee),

      feeType: Number(x.feeType),

      totalSessions: x.feeType === 1
        ? Number(x.totalSessions)
        : null,

      capacity: x.unlimited
        ? null
        : (x.capacity ? Number(x.capacity) : null)
    }));

    if (!this.isEdit) {

      const dto: CreateCourseDto = {
        name: this.form.value.name,
        description: this.form.value.description || null,
        sessions: sessionsPayload
      };

      this.courses.create(dto).subscribe({
        next: () => {
          this.saving = false;
          this.snackbar.showSuccess('Curs creat cu succes.', 1800);
          this.dialogRef.close(true);
        },
        error: (err) => this.handleError(err)
      });

    } else {

      const dto: UpdateCourseDto = {
        name: this.form.value.name,
        description: this.form.value.description || null,
        isActive: !!this.form.value.isActive,
        sessions: sessionsPayload
      };
      console.log(JSON.stringify(dto, null, 2));
      this.courses.update(this.courseId!, dto).subscribe({
        next: () => {
          this.saving = false;
          this.snackbar.showSuccess('Curs actualizat cu succes.', 1800);
          this.dialogRef.close(true);
        },
        error: (err) => this.handleError(err)
      });
    }
  }

  cancel(): void {
    this.dialogRef.close(true);
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
        return false;
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

  private handleError(err: any) {
    this.saving = false;

    const message = err?.error?.message ?? '';

    if (message.includes('suprapus')) {
      this.snackbar.showError(
        'Profesorul este deja programat în alt curs la același interval.',
        3000
      );
      return;
    }

    this.snackbar.showError(
      message || 'Eroare la salvare curs.',
      2500
    );
  }


  isDeleteDisabled(i: number): boolean {
    const session = this.sessions.at(i).value;

    return (
      this.sessions.length <= 1 ||
      (session.enrolledActiveCount ?? 0) > 0 ||
      !session.isActive ||
      !this.form.value.isActive
    );
  }

  getDeleteTooltip(i: number): string {
    const session = this.sessions.at(i).value;

    if (this.sessions.length <= 1) return 'Trebuie să existe cel puțin o sesiune';
    if ((session.enrolledActiveCount ?? 0) > 0) return 'Are cursanți activi';
    if (!session.isActive) return 'Sesiunea este inactivă';
    if (!this.form.value.isActive) return 'Cursul este inactiv';

    return '';
  }
}