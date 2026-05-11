import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { StudentsService } from '../../services/students.service';
import { CreateStudentDto, UpdateStudentDto } from '../../models/student.model';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule
  ],
  templateUrl: './student-form.component.html',
  styleUrls: ['./student-form.component.css']
})
export class StudentFormComponent implements OnInit {

  loading = true;
  saving = false;
  isMinor = false;

  studentId: number | null = null;
  isEdit = false;

  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private students: StudentsService,
    private snackbar: SnackbarService,
    private dialogRef: MatDialogRef<StudentFormComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { id?: number }
  ) { }

  ngOnInit(): void {

    this.form = this.fb.group({
      fullName: [
        '',
        [
          Validators.required,
          Validators.maxLength(120),
          Validators.pattern(/^[a-zA-ZăĂâÂîÎșȘțȚ\s'-]+$/)
        ]
      ],
      firstName: [
        '',
        [
          Validators.maxLength(80),
          Validators.pattern(/^[a-zA-ZăĂâÂîÎșȘțȚ\s'-]*$/)
        ]
      ],
      lastName: [
        '',
        [
          Validators.maxLength(80),
          Validators.pattern(/^[a-zA-ZăĂâÂîÎșȘțȚ\s'-]*$/)
        ]
      ],
      email: ['', [Validators.email, Validators.maxLength(120)]],
      phone: [
        '',
        [
          Validators.pattern(/^[0-9+\s()-]{7,20}$/)
        ]
      ],
      address: ['', [Validators.maxLength(250)]],
      dateOfBirth: ['', [this.birthDateValidator.bind(this)]],
      isActive: [true],
      guardians: this.fb.array([])
    });

    if (this.data?.id) {
      this.studentId = this.data.id;
      this.isEdit = true;

      this.students.get(this.studentId).subscribe({
        next: (s) => {

          this.form.patchValue({
            fullName: s.fullName ?? '',
            firstName: s.firstName ?? '',
            lastName: s.lastName ?? '',
            email: s.email ?? '',
            phone: s.phone ?? '',
            address: s.address ?? '',
            dateOfBirth: s.dateOfBirth
              ? s.dateOfBirth.substring(0, 10)
              : '',
            isActive: s.isActive ?? true
          });

          if (s.dateOfBirth) {
            const birth = this.parseDateOnly(s.dateOfBirth.substring(0, 10));
            const today = new Date();
            let age = today.getFullYear() - birth.getFullYear();

            const m = today.getMonth() - birth.getMonth();
            if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
              age--;
            }

            this.isMinor = age < 18;
          }

          if (s.guardians && s.guardians.length > 0) {

            this.guardians.clear();

            s.guardians.forEach((g: any) => {
              this.guardians.push(this.createGuardianGroup(g));
            });
          }

          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.snackbar.showError(
            this.getErrorMessage(err, 'Nu am putut încărca elevul.'),
            2500
          );
          this.dialogRef.close();
        }
      });

    } else {
      this.loading = false;
    }

    this.form.get('dateOfBirth')?.valueChanges.subscribe(value => {
      if (!value) {
        this.isMinor = false;
        return;
      }

      const birth = this.parseDateOnly(value);
      const age = this.calculateAge(birth);

      this.isMinor = age < 18;



      if (this.isMinor && this.guardians.length === 0) {
        this.addGuardian();
      }

      if (!this.isMinor) {
        this.guardians.clear();
      }
    });
  }

  save(): void {
    const primaryCount = this.guardians.controls.filter(
      g => g.value.isPrimaryContact
    ).length;

    if (primaryCount > 1) {
      this.snackbar.showError(
        'Poate exista un singur contact principal.',
        2500
      );
      return;
    }


    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackbar.showError('Completează corect câmpurile obligatorii.', 2500);
      return;
    }

    if (this.isMinor && this.guardians.length === 0) {
      this.snackbar.showError('Elevul minor trebuie să aibă cel puțin un părinte.', 2500);
      return;
    }

    if (this.isMinor && this.guardians.invalid) {
      this.guardians.markAllAsTouched();
      this.snackbar.showError('Completează corect datele părintelui/tutorelui.', 2500);
      return;
    }

    if (primaryCount > 1) {
      this.snackbar.showError('Poate exista un singur contact principal.', 2500);
      return;
    }


    this.saving = true;

    if (!this.isEdit) {
      const dto: CreateStudentDto = {
        fullName: this.form.value.fullName!,
        firstName: this.form.value.firstName || null,
        lastName: this.form.value.lastName || null,
        email: this.form.value.email || null,
        phone: this.form.value.phone || null,
        address: this.form.value.address || null,
        dateOfBirth: this.form.value.dateOfBirth || null,
        guardians: this.isMinor ? this.guardians.value : null
      };

      this.students.create(dto).subscribe({
        next: (res) => {
          this.saving = false;
          this.snackbar.showSuccess('Elev creat cu succes.', 1800);
          this.dialogRef.close(res);
        },
        error: (err) => {
          this.saving = false;
          this.snackbar.showError(
            this.getErrorMessage(err, 'Eroare la creare elev.'),
            2500
          );
        }
      });

    } else {
      const dto: UpdateStudentDto = {
        fullName: this.form.value.fullName!,
        firstName: this.form.value.firstName || null,
        lastName: this.form.value.lastName || null,
        email: this.form.value.email || null,
        phone: this.form.value.phone || null,
        address: this.form.value.address || null,
        dateOfBirth: this.form.value.dateOfBirth || null,
        isActive: !!this.form.value.isActive,
        guardians: this.isMinor ? this.guardians.value : null
      };

      this.students.update(this.studentId!, dto).subscribe({
        next: () => {
          this.saving = false;
          this.snackbar.showSuccess('Elev actualizat cu succes.', 1800);
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.saving = false;
          this.snackbar.showError(
            this.getErrorMessage(err, 'Eroare la actualizare elev.'),
            2500
          );
        }
      });
    }
  }

  cancel(): void {
    this.dialogRef.close();
  }

  get guardians(): FormArray {
    return this.form.get('guardians') as FormArray;
  }

  addGuardian(): void {
    this.guardians.push(this.createGuardianGroup());
  }
  removeGuardian(index: number) {
    this.guardians.removeAt(index);
  }

  private createGuardianGroup(g?: any): FormGroup {
    return this.fb.group({
      firstName: [
        g?.firstName ?? '',
        [
          Validators.required,
          Validators.maxLength(80),
          Validators.pattern(/^[a-zA-ZăĂâÂîÎșȘțȚ\s'-]+$/)
        ]
      ],
      lastName: [
        g?.lastName ?? '',
        [
          Validators.required,
          Validators.maxLength(80),
          Validators.pattern(/^[a-zA-ZăĂâÂîÎșȘțȚ\s'-]+$/)
        ]
      ],
      email: [
        g?.email ?? '',
        [Validators.required, Validators.email, Validators.maxLength(120)]
      ],
      phone: [
        g?.phone ?? '',
        [Validators.required, Validators.pattern(/^[0-9+\s()-]{7,20}$/)]
      ],
      relationshipType: [g?.relationshipType ?? 'Mama', Validators.required],
      isPrimaryContact: [g?.isPrimaryContact ?? false]
    });
  }

  private getErrorMessage(err: any, fallback: string): string {
    return err?.error?.message ||
      err?.error?.errorMessage ||
      err?.error?.title ||
      fallback;
  }

  private birthDateValidator(control: any) {
    if (!control.value) return null;

    const birth = new Date(control.value);
    const today = new Date();

    if (birth > today) {
      return { futureDate: true };
    }

    const age = this.calculateAge(birth);

    if (age > 100) {
      return { tooOld: true };
    }

    return null;
  }

  private calculateAge(birth: Date): number {
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();

    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
      age--;
    }

    return age;
  }

  canShowPrimaryContact(index: number): boolean {
    return this.guardians.controls.every((g, i) =>
      i === index || !g.value.isPrimaryContact
    );
  }

  onPrimaryContactChange(index: number): void {
    const current = this.guardians.at(index);

    if (!current.value.isPrimaryContact) return;

    this.guardians.controls.forEach((g, i) => {
      if (i !== index) {
        g.patchValue({ isPrimaryContact: false }, { emitEvent: false });
      }
    });
  }

  private parseDateOnly(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
}
