import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
  ValidatorFn,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
import { ContractsService } from '../../services/contracts.service';
import { StudentsService } from '../../services/students.service';
import { StudentCourseDetailsDto } from '../../models/student.model';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-create-contract',
  templateUrl: './create-contract.component.html',
  styleUrl: './create-contract.component.css',
  imports: [ReactiveFormsModule, CommonModule],
  standalone: true
})
export class CreateContractComponent implements OnInit {

  contractForm!: FormGroup;

  studentCourses: StudentCourseDetailsDto[] = [];
  totalAmount = 0;
  studentId!: number;
  isSubmitting = false;

  selectedStudent: any;
  selectedGuardian: any;

  contractId!: number;
  isEdit = false;

  constructor(
    private fb: FormBuilder,
    private contractsService: ContractsService,
    private studentsService: StudentsService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  // ===============================
  // INIT
  // ===============================
  ngOnInit(): void {
    this.buildForm();

    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.contractId = Number(id);
      this.isEdit = true;
      this.loadContract(this.contractId);
      return;
    }

    const studentId = this.route.snapshot.queryParamMap.get('studentId');

    if (studentId) {
      this.studentId = Number(studentId);
      this.prefillFromStudent(this.studentId);
    }
  }

  loadContract(id: number) {
    this.contractsService.getById(id).subscribe(res => {

      if (!res?.value) return;

      const c = res.value;

      this.contractForm.patchValue({
        startDate: c.startDate,
        endDate: c.endDate,
        isUnlimited: c.isUnlimited,
        installments: c.installments,
        courseSessionIds: c.courses.map((x: any) => x.courseSessionId)
      });

      this.totalAmount = c.totalAmount;

      this.discounts.clear();

      c.discounts.forEach((d: any) => {
        this.discounts.push(this.fb.group({
          type: d.type,
          value: d.value,
          reason: d.reason
        }));
      });
    });
  }

  // ===============================
  // FORM
  // ===============================
  buildForm() {
    this.contractForm = this.fb.group({
      guardianId: [null],
      studentIds: [[], Validators.required],
      courseSessionIds: [[], Validators.required],
      startDate: [null, Validators.required],
      endDate: [null],
      isUnlimited: [false],
      installments: [1, [Validators.required, Validators.min(1)]],
      discounts: this.fb.array([])
    }, {
      validators: this.dateRangeValidator()
    });

    this.contractForm.get('isUnlimited')?.valueChanges.subscribe(value => {
      const endDateControl = this.contractForm.get('endDate');

      if (value) {
        endDateControl?.setValue(null);
        endDateControl?.clearValidators();
        endDateControl?.disable();
      } else {
        endDateControl?.setValidators([Validators.required]);
        endDateControl?.enable();
      }

      endDateControl?.updateValueAndValidity();
    });
  }

  get discounts(): FormArray {
    return this.contractForm.get('discounts') as FormArray;
  }

  addDiscount() {
    this.discounts.push(this.fb.group({
      type: ['Percentage'],
      value: [0, Validators.required],
      reason: ['']
    }));
  }

  removeDiscount(index: number) {
    this.discounts.removeAt(index);
  }

  calculateTotal(): number {
    let total = this.totalAmount;

    this.discounts.controls.forEach(ctrl => {
      const type = ctrl.value.type;
      const value = Number(ctrl.value.value);

      if (type === 'Percentage') {
        total -= total * (value / 100);
      } else {
        total -= value;
      }
    });

    return total < 0 ? 0 : total;
  }

  // ===============================
  // PREFILL
  // ===============================
  prefillFromStudent(studentId: number) {

    // 1️⃣ student
    this.studentsService.getById(studentId).subscribe(res => {

      if (!res?.isSuccess || !res.value) return;

      const student = res.value;
      this.selectedStudent = student;

      const primary = student.guardians?.find(
        (g: any) => g.isPrimaryContact
      );

      if (primary) {
        this.selectedGuardian = { ...primary };

        this.contractForm.patchValue({
          guardianId: primary.id
        });
      }
    });

    this.contractForm.patchValue({
      studentIds: [studentId]
    });

    // 2️⃣ courses
    this.studentsService.getStudentCourses(studentId).subscribe(res => {

      this.studentCourses = res.items ?? [];

      if (!this.studentCourses.length) {
        alert('Elevul nu are cursuri asignate');
      }

      this.totalAmount = this.studentCourses.reduce((sum, course) => {
        return sum + (course.price ?? 0);
      }, 0);

      const sessionIds = this.studentCourses.map(x => x.sessionId);

      this.contractForm.patchValue({
        courseSessionIds: sessionIds
      });

    });
  }

  // ===============================
  // SUBMIT
  // ===============================
  submit() {

    if (this.contractForm.invalid) {
      this.contractForm.markAllAsTouched();
      return;
    }

    if (this.isSubmitting) return;

    const formValue = this.contractForm.getRawValue();

    const baseDto = {
      startDate: formValue.startDate,
      endDate: formValue.isUnlimited ? null : formValue.endDate,
      isUnlimited: formValue.isUnlimited,
      installments: formValue.installments,
      courseSessionIds: formValue.courseSessionIds,
      discounts: formValue.discounts
    };

    this.isSubmitting = true;

    if (this.isEdit) {

      this.contractsService.update(this.contractId, baseDto).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.router.navigate(['/contracts', this.contractId]);
        },
        error: () => this.isSubmitting = false
      });

    } else {

      const createDto = {
        ...baseDto,
        guardianId: formValue.guardianId,
        studentIds: formValue.studentIds
      };

      this.contractsService.create(createDto).subscribe({
        next: (res: any) => {

          this.isSubmitting = false;

          const id = res?.value?.id || res?.value?.existingContractId;

          if (id) {
            this.router.navigate(['/contracts', id]);
          }
        },
        error: () => this.isSubmitting = false
      });
    }
  }

  get submitText() {
    return this.isEdit ? '💾 Salvează modificări' : '➕ Creează contract';
  }

  // ===============================
  // VALIDATOR
  // ===============================
  private dateRangeValidator(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {

      const start = group.get('startDate')?.value;
      const end = group.get('endDate')?.value;
      const isUnlimited = group.get('isUnlimited')?.value;

      if (!start) return null;

      if (!isUnlimited && !end) {
        return { endRequired: true };
      }

      if (!isUnlimited && end && new Date(end) <= new Date(start)) {
        return { invalidRange: true };
      }

      return null;
    };
  }
}