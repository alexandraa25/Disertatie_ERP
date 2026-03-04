import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog'; // 👈 adaugă MatDialogModule
import { StudentsService } from '../../services/students.service';
import { CreateStudentDto, UpdateStudentDto } from '../../models/student.model';


@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule   // 🔥 ASTA LIPSEA
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
    private dialogRef: MatDialogRef<StudentFormComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { id?: number }
  ) { }

  ngOnInit(): void {

    this.form = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(120)]],
      firstName: [''],
      lastName: [''],
      email: [''],
      phone: [''],
      address: [''],
      dateOfBirth: [''],
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
              ? new Date(s.dateOfBirth).toISOString().split('T')[0]
              : '',
            isActive: s.isActive ?? true
          });

          // 🔥 1️⃣ Detectăm dacă este minor
          if (s.dateOfBirth) {
            const birth = new Date(s.dateOfBirth);
            const today = new Date();
            let age = today.getFullYear() - birth.getFullYear();

            const m = today.getMonth() - birth.getMonth();
            if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
              age--;
            }

            this.isMinor = age < 18;
          }

          // 🔥 2️⃣ Încărcăm guardians dacă există
          if (s.guardians && s.guardians.length > 0) {

            this.guardians.clear(); // important la edit

            s.guardians.forEach((g: any) => {
              this.guardians.push(this.fb.group({
                firstName: [g.firstName, Validators.required],
                lastName: [g.lastName, Validators.required],
                email: [g.email, [Validators.required, Validators.email]],
                phone: [g.phone, Validators.required],
                relationshipType: [g.relationshipType, Validators.required],
                isPrimaryContact: [g.isPrimaryContact]
              }));
            });
          }

          this.loading = false;
        },
        error: () => {
          this.loading = false;
          alert('Nu am putut încărca elevul.');
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

      const birth = new Date(value);
      const today = new Date();
      let age = today.getFullYear() - birth.getFullYear();

      const m = today.getMonth() - birth.getMonth();
      if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
        age--;
      }

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
    if (this.isMinor && this.guardians.length === 0) {
  alert('Elevul minor trebuie să aibă cel puțin un părinte.');
  return;
}

const primaryCount = this.guardians.controls.filter(
  g => g.value.isPrimaryContact
).length;

if (primaryCount > 1) {
  alert('Poate exista un singur contact principal.');
  return;
}
    if (this.form.invalid) {
      this.form.markAllAsTouched();
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
          this.dialogRef.close(res); // trimitem înapoi studentul creat
        },
        error: () => {
          this.saving = false;
          alert('Eroare la creare elev');
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
          this.dialogRef.close(true);
        },
        error: () => {
          this.saving = false;
          alert('Eroare la actualizare elev');
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
  addGuardian() {
    const group = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      relationshipType: ['Mama', Validators.required],
      isPrimaryContact: [false]
    });

    this.guardians.push(group);
  }

  removeGuardian(index: number) {
    this.guardians.removeAt(index);
  }
}
