import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
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


  constructor(
    private fb: FormBuilder,
    private contractsService: ContractsService,
    private studentsService: StudentsService,
    private route: ActivatedRoute, 
    private router: Router
  ) {}

  ngOnInit(): void {
    this.buildForm();

    const id = this.route.snapshot.queryParamMap.get('studentId');

    if (id) {
      this.studentId = Number(id);
      this.prefillFromStudent(this.studentId);
    }
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

  this.contractForm.get('isUnlimited')?.valueChanges
    .subscribe(value => {

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
      this.contractForm.updateValueAndValidity();
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
  // PREFILL DATA
  // ===============================
  prefillFromStudent(studentId: number) {

  // 🔵 1️⃣ Aducem datele elevului
this.studentsService.getById(studentId)
  .subscribe(res => {

    if (!res?.isSuccess || !res.value) return;

    const student = res.value;

    this.selectedStudent = student;

    const primary = student.guardians?.find(
      (g: any) => g.isPrimaryContact
    );

   if (primary) {

  this.selectedGuardian = null; // 🔥 reset

  setTimeout(() => {
    this.selectedGuardian = { ...primary };

    this.contractForm.patchValue({
      guardianId: primary.id
    });

  }); }
}); 

  // select student automat
  this.contractForm.patchValue({
    studentIds: [studentId]
  });

  // 🔵 2️⃣ Aducem cursuri
this.studentsService.getStudentCourses(studentId)
  .subscribe(res => {

    this.studentCourses = res.items ?? [];

    // 🔥 calcul corect
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

  this.isSubmitting = true;

  const formValue = this.contractForm.getRawValue();

  const dto = {
    guardianId: formValue.guardianId,
    studentIds: formValue.studentIds,
    courseSessionIds: formValue.courseSessionIds,
    startDate: new Date(formValue.startDate).toISOString(),
    endDate: formValue.isUnlimited || !formValue.endDate
      ? null
      : new Date(formValue.endDate).toISOString(),
    isUnlimited: formValue.isUnlimited,
    installments: formValue.installments,
    discounts: formValue.discounts
  };

 this.contractsService.create(dto)
  .subscribe({
    next: (res) => {

      this.isSubmitting = false;

      const contractId = res?.value?.id;

      if (contractId) {
        console.log(contractId);
        // 🔥 Redirect corect
        this.router.navigate(['/contracts', contractId]);

      }

    },
    error: err => {

      this.isSubmitting = false;
      console.error(err);
    }
  });
    
}
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