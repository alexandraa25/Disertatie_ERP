import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { ContractsService } from '../../services/contracts.service';
import { StudentsService } from '../../services/students.service';
import { StudentCourseDetailsDto } from '../../models/student.model';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MarketingCampaignService } from '../../services/marketing.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

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
  subtotal = 0;
  monthlyAmount = 0;
  finalTotal: number | null = null;
  studentId!: number;
  isSubmitting = false;

  selectedStudent: any;
  selectedGuardian: any;

  contractId!: number;
  isEdit = false;

  hasSubscription = false;
  hasPackage = false;
  packageAmount = 0;

  availableCampaigns: any[] = [];

  constructor(
    private fb: FormBuilder,
    private contractsService: ContractsService,
    private studentsService: StudentsService,
    private route: ActivatedRoute,
    private router: Router,
    private marketingService: MarketingCampaignService,
    private snackbar: SnackbarService
  ) { }

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

    this.contractForm.valueChanges.subscribe(() => {
      this.calculatePricingPreview();
    });
    this.discounts.valueChanges.subscribe(() => {
      this.validateDiscounts();

    });
  }

  loadContract(id: number) {
    this.contractsService.getById(id).subscribe(res => {

      if (!res?.value) return;

      const c = res.value;

      if (c.status !== 'Draft') {
        this.snackbar.showError('Contractul nu mai poate fi editat.', 2500);
        this.router.navigate(['/contracts', id]);
        return;
      }

      this.contractForm.patchValue({
        startDate: this.formatDate(c.startDate),
        endDate: c.endDate ? this.formatDate(c.endDate) : null,
        isUnlimited: c.isUnlimited,
        installments: c.installments,
        courseSessionIds: c.courses.map((x: any) => x.courseSessionId)
      });

      if (this.isEdit) {
        this.contractForm.get('studentIds')?.disable();
        this.contractForm.get('guardianId')?.disable();
        this.contractForm.get('courseSessionIds')?.disable();
      }

      const studentParty = c.parties.find((p: any) => p.studentId);
      const guardianParty = c.parties.find((p: any) => p.guardianId);

      if (studentParty) {
        this.selectedStudent = {
          firstName: studentParty.studentName,
          lastName: ''
        };
      }

      if (guardianParty) {
        this.selectedGuardian = {
          firstName: guardianParty.guardianName,
          lastName: ''
        };
      }

      this.studentCourses = c.courses.map((x: any) => ({
        courseName: x.courseName,
        dayOfWeek: x.sessionName,
        startTime: '',
        endTime: '',
        teacherName: '',
        price: x.priceSnapshot
      }));

      this.discounts.clear();

      c.discounts.forEach((d: any) => {
        this.discounts.push(this.fb.group({
          type: d.type,
          value: d.value,
          reason: d.reason,
          scope: d.scope || 'Total'
        }));

      });

    });

    this.calculatePricingPreview();

  }

  buildForm() {
    this.contractForm = this.fb.group({
      guardianId: [null],
      studentIds: [[], Validators.required],
      courseSessionIds: [[], Validators.required],
      marketingCampaignId: [null],
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
      value: [0, [Validators.required, Validators.min(0)]],
      reason: [''],
      scope: ['Total']
    }));
  }

  validateDiscounts() {
    this.discounts.controls.forEach(ctrl => {

      const type = ctrl.get('type')?.value;
      const valueCtrl = ctrl.get('value');

      if (!valueCtrl) return;

      if (type === 'Percentage' && valueCtrl.value > 100) {
        valueCtrl.setErrors({ ...(valueCtrl.errors || {}), max: true });
      }

    });
  }

  getDiscountImpact(ctrl: any): number {

    const type = ctrl.value.type;
    const value = Number(ctrl.value.value);
    const scope = ctrl.value.scope;

    let base = 0;

    if (scope === 'Subscription') {
      base = this.monthlyAmount;
    } else if (scope === 'Package') {
      base = this.packageAmount;

    } else {
      base = this.finalTotal ?? this.monthlyAmount;
    }

    if (!base) return 0;

    if (type === 'Percentage') {
      return base * (value / 100);
    }


    return value;
  }

  removeDiscount(index: number) {
    this.discounts.removeAt(index);
  }

  calculatePricingPreview() {

    this.subtotal = 0;
    this.monthlyAmount = 0;
    this.finalTotal = null;

    const isUnlimited = this.contractForm.get('isUnlimited')?.value;
    const start = this.contractForm.get('startDate')?.value;
    const end = this.contractForm.get('endDate')?.value;

    this.subtotal = this.studentCourses.reduce((sum, c) => {
      return sum + (c.price ?? 0);
    }, 0);

    this.monthlyAmount = this.subtotal;

    let months = 0;

    if (!isUnlimited && start && end) {
      const d1 = new Date(start);
      const d2 = new Date(end);

      months =
        (d2.getFullYear() - d1.getFullYear()) * 12 +
        (d2.getMonth() - d1.getMonth()) + 1;
    }

    this.finalTotal = isUnlimited ? null : this.monthlyAmount * months;
    this.packageAmount = this.finalTotal ?? 0;

    this.discounts.controls.forEach(ctrl => {

      const type = ctrl.value.type;
      const value = Number(ctrl.value.value);
      const scope = ctrl.value.scope;

      const apply = (amount: number) => {
        if (type === 'Percentage') {
          return amount - amount * (value / 100);
        } else {
          return amount - value;
        }
      };

      if (scope === 'Total') {

        if (this.finalTotal != null)
          this.finalTotal = apply(this.finalTotal);

        this.monthlyAmount = apply(this.monthlyAmount);

      } else if (scope === 'Subscription') {

        this.monthlyAmount = apply(this.monthlyAmount);

        if (this.finalTotal != null && !isUnlimited) {
          this.finalTotal = apply(this.finalTotal);
        }

      } else if (scope === 'Package') {

        if (this.finalTotal != null)
          this.finalTotal = apply(this.finalTotal);
      }
    });

    const campaignId = this.contractForm.get('marketingCampaignId')?.value;

    if (campaignId) {
      const campaign = this.availableCampaigns.find(c => c.id === Number(campaignId));

      if (campaign) {

        const type = campaign.discountType; // 1 = %, altfel fix
        const value = Number(campaign.discountValue);
        const scope = campaign.discountScope;

        const apply = (amount: number) => {
          if (Number(type) === 1) { // 🔥 Percentage
            return amount - amount * (value / 100);
          }

          return amount - value; // 🔥 FixedAmount
        };

        // 0 = Total
        if (scope === 0) {

          if (this.finalTotal != null)
            this.finalTotal = apply(this.finalTotal);

          this.monthlyAmount = apply(this.monthlyAmount);
        }

        // 1 = Package
        if (scope === 1) {

          if (this.finalTotal != null)
            this.finalTotal = apply(this.finalTotal);
        }

        // 2 = Subscription
        if (scope === 2) {

          this.monthlyAmount = apply(this.monthlyAmount);

          if (this.finalTotal != null && !isUnlimited) {
            this.finalTotal = apply(this.finalTotal);
          }
        }
      }
    }

    if (this.finalTotal != null && this.finalTotal < 0)
      this.finalTotal = 0;

    if (this.monthlyAmount < 0)
      this.monthlyAmount = 0;

    this.hasSubscription = this.monthlyAmount > 0;
    this.hasPackage = this.finalTotal !== null;

    this.discounts.controls.forEach(ctrl => {
      const scope = ctrl.get('scope')?.value;

      if (scope === 'Package' && !this.hasPackage) {
        ctrl.get('scope')?.setValue('Total');
      }

      if (scope === 'Subscription' && !this.hasSubscription) {
        ctrl.get('scope')?.setValue('Total');
      }
    });
  }

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

      const all = res.items ?? [];

      this.studentCourses = all.filter(x => x.isActive && !x.contractId);

      if (!this.studentCourses.length) {
        this.snackbar.showError('Elevul nu are cursuri active necontractate.', 2500);
      }

      const sessionIds = this.studentCourses.map(x => x.sessionId);

      this.contractForm.patchValue({
        courseSessionIds: sessionIds
      });

      this.loadAvailableCampaigns();

    });
  }

  loadAvailableCampaigns(): void {
    const sessionIds = this.contractForm.get('courseSessionIds')?.value ?? [];

    if (!sessionIds.length) {
      this.availableCampaigns = [];
      return;
    }

    this.marketingService.getAvailableCampaigns(sessionIds).subscribe({
  next: (res: any) => {
    this.availableCampaigns = res.isSuccess ? res.value : [];
  },
  error: () => {
    this.availableCampaigns = [];
    this.snackbar.showError('Campaniile disponibile nu au putut fi încărcate.', 2500);
  }
});
  }

  onMarketingCampaignChange(): void {
    const campaignId = this.contractForm.get('marketingCampaignId')?.value;

    if (campaignId) {
      this.discounts.clear();
    }

    this.calculatePricingPreview();
  }

  getDiscountLabel(type: number, value: number): string {
    return type === 1
      ? `${value}%`      // Percentage
      : `${value} RON`;  // Fixed
  }

  submit() {

    if (this.contractForm.invalid) {
  this.contractForm.markAllAsTouched();
  this.snackbar.showError('Completează câmpurile obligatorii.', 2500);
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
      marketingCampaignId: formValue.marketingCampaignId,
      discounts: formValue.discounts

    };

    this.isSubmitting = true;

    if (this.isEdit) {
      this.contractsService.update(this.contractId, baseDto).subscribe({
        next: (res: any) => {
          this.isSubmitting = false;

          if (res?.isSuccess === false) {
            this.snackbar.showError(
              res.error?.errorMessage || 'Contractul nu a putut fi actualizat.',
              2500
            );
            return;
          }

          this.snackbar.showSuccess('Contract actualizat cu succes.', 1800);
          this.router.navigate(['/contracts', this.contractId]);
        },
        error: () => {
          this.isSubmitting = false;
          this.snackbar.showError('Eroare la actualizarea contractului.', 2500);
        }
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

          if (res?.isSuccess === false) {
            this.snackbar.showError(
              res.error?.errorMessage || 'Contractul nu a putut fi creat.',
              2500
            );
            return;
          }

          const id = res?.value?.id || res?.value?.existingContractId;

          this.snackbar.showSuccess('Contract creat cu succes.', 1800);

          if (id) {
            this.router.navigate(['/contracts', id]);
          }
        },
        error: () => {
          this.isSubmitting = false;
          this.snackbar.showError('Eroare la crearea contractului.', 2500);
        }
      });
    }
  }

  get submitText() {
    return this.isEdit ? '💾 Salvează modificări' : '➕ Creează contract';
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

  formatDate(date: string) {
    if (!date) return null;
    return date.substring(0, 10); // 👈 MAGIC
  }

  getCampaignDiscountLabel(c: any): string {
    return c.discountType === 1
      ? `${c.discountValue}%`
      : `${c.discountValue} RON`;
  }

  getSelectedCampaign(): any | null {
    const campaignId = this.contractForm.get('marketingCampaignId')?.value;

    if (!campaignId) {
      return null;
    }

    return this.availableCampaigns.find(x => x.id === Number(campaignId)) ?? null;
  }
}