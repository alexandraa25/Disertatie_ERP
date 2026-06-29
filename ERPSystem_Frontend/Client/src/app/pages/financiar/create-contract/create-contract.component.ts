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
import { RomanianDayPipe } from '../../../components/pipes/romanian-day.pipe';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';

@Component({
  selector: 'app-create-contract',
  templateUrl: './create-contract.component.html',
  styleUrl: './create-contract.component.css',
  imports: [ReactiveFormsModule, CommonModule, RomanianDayPipe, ConfirmCustomModalComponent],
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

  hasSubscriptionCourses = false;
  hasFixedPackageCourses = false;
  packageAmount = 0;

  availableCampaigns: any[] = [];

  packageBase = 0;
  monthlyBase = 0;
  months = 0;
  discountTotal = 0;
  totalEstimated = 0;

  initialPaymentEstimated = 0;
  periodTotalEstimated: number | null = null;

  constructor(
    private fb: FormBuilder,
    private contractsService: ContractsService,
    private studentsService: StudentsService,
    private route: ActivatedRoute,
    private router: Router,
    private marketingService: MarketingCampaignService,
    private snackbar: SnackbarService, 
    private confirmService: ConfirmService
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
        this.contractForm.get('studentId')?.disable();
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
        price: x.price ?? x.priceSnapshot,
        feeType: x.courseFeeType ?? x.feeType
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

      this.calculatePricingPreview();

    });


  }

  buildForm() {
    this.contractForm = this.fb.group({
      guardianId: [null],
      studentId: [null, Validators.required],
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

  removeDiscount(index: number) {
    this.discounts.removeAt(index);
  }

  calculatePricingPreview() {
    this.packageBase = 0;
    this.monthlyBase = 0;

    this.packageAmount = 0;
    this.monthlyAmount = 0;
    this.subtotal = 0;
    this.finalTotal = null;
    this.discountTotal = 0;
    this.totalEstimated = 0;
    this.months = 0;

    const isUnlimited = this.contractForm.get('isUnlimited')?.value;
    const start = this.contractForm.get('startDate')?.value;
    const end = this.contractForm.get('endDate')?.value;

    const packageCourses = this.studentCourses.filter(c => Number(c.feeType) === 1);
    const subscriptionCourses = this.studentCourses.filter(c => Number(c.feeType) === 2);

    this.hasFixedPackageCourses = packageCourses.length > 0;
    this.hasSubscriptionCourses = subscriptionCourses.length > 0;

    this.packageBase = packageCourses.reduce((sum, c) => sum + (c.price ?? 0), 0);
    this.monthlyBase = subscriptionCourses.reduce((sum, c) => sum + (c.price ?? 0), 0);

    this.packageAmount = this.packageBase;
    this.monthlyAmount = this.monthlyBase;

    this.subtotal = this.packageBase + this.monthlyBase;

    if (!isUnlimited && start && end) {
      const d1 = new Date(start);
      const d2 = new Date(end);

      this.months =
        (d2.getFullYear() - d1.getFullYear()) * 12 +
        (d2.getMonth() - d1.getMonth()) + 1;
    }

    const applyDiscount = (amount: number, type: any, value: number): number => {
      if (amount <= 0 || value <= 0) return amount;

      let discounted: number;

      if (type === 'Percentage' || Number(type) === 1) {
        discounted = amount - amount * (value / 100);
      } else {
        discounted = amount - value;
      }

      const safeDiscounted = Math.max(0, discounted);

      this.discountTotal += amount - safeDiscounted;

      return safeDiscounted;
    };

    const applyDiscountByScope = (scope: any, type: any, value: number) => {
      // Manual: Total / Package / Subscription
      // Campanie: 0 = Total, 1 = Package, 2 = Subscription

      const normalizedScope =
        scope === 0 ? 'Total' :
          scope === 1 ? 'Package' :
            scope === 2 ? 'Subscription' :
              scope;

      if (normalizedScope === 'Package') {
        this.packageAmount = applyDiscount(this.packageAmount, type, value);
        return;
      }

      if (normalizedScope === 'Subscription') {
        this.monthlyAmount = applyDiscount(this.monthlyAmount, type, value);
        return;
      }

      if (normalizedScope === 'Total') {
        if (isUnlimited) {
          const isPercentage = type === 'Percentage' || Number(type) === 1;

          if (isPercentage) {
            if (this.hasFixedPackageCourses)
              this.packageAmount = applyDiscount(this.packageAmount, type, value);
            if (this.hasSubscriptionCourses)
              this.monthlyAmount = applyDiscount(this.monthlyAmount, type, value);
          } else {
            
            const unlimitedTotal = this.packageAmount + this.monthlyAmount;
            if (unlimitedTotal > 0) {
              const pkgShare = value * this.packageAmount / unlimitedTotal;
              const subShare = value * this.monthlyAmount / unlimitedTotal;
              const newPkg = Math.max(0, this.packageAmount - pkgShare);
              const newSub = Math.max(0, this.monthlyAmount - subShare);
              this.discountTotal += (this.packageAmount - newPkg) + (this.monthlyAmount - newSub);
              this.packageAmount = newPkg;
              this.monthlyAmount = newSub;
            }
          }

          return;
        }

        const currentTotal =
          this.packageAmount + this.monthlyAmount * this.months;

        const discountedTotal = applyDiscount(currentTotal, type, value);

        if (currentTotal > 0) {
          const ratio = discountedTotal / currentTotal;

          this.packageAmount = this.packageAmount * ratio;
          this.monthlyAmount = this.monthlyAmount * ratio;
        }
      }
    };

    
    this.discounts.controls.forEach(ctrl => {
      const type = ctrl.value.type;
      const value = Number(ctrl.value.value);
      const scope = ctrl.value.scope;

      applyDiscountByScope(scope, type, value);
    });

    // Campanie marketing
    const campaignId = this.contractForm.get('marketingCampaignId')?.value;

    if (campaignId) {
      const campaign = this.availableCampaigns.find(c => c.id === Number(campaignId));

      if (campaign) {
        applyDiscountByScope(
          campaign.discountScope,
          campaign.discountType,
          Number(campaign.discountValue)
        );
      }
    }

    this.packageAmount = Math.max(0, this.packageAmount);
    this.monthlyAmount = Math.max(0, this.monthlyAmount);

    if (isUnlimited) {
      this.finalTotal = this.hasFixedPackageCourses
        ? this.packageAmount
        : null;

      this.totalEstimated = this.finalTotal ?? this.monthlyAmount;
    } else {
      this.finalTotal =
        this.packageAmount + this.monthlyAmount * this.months;

      this.totalEstimated = this.finalTotal;
    }

    this.discounts.controls.forEach(ctrl => {
      const scope = ctrl.get('scope')?.value;

      if (scope === 'Package' && !this.hasFixedPackageCourses) {
        ctrl.get('scope')?.setValue('Total', { emitEvent: false });
      }

      if (scope === 'Subscription' && !this.hasSubscriptionCourses) {
        ctrl.get('scope')?.setValue('Total', { emitEvent: false });
      }
    });
  }

  prefillFromStudent(studentId: number) {

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
      studentId: studentId
    });

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
      ? `${value}%`
      : `${value} RON`;
  }

  async submit() {
  if (this.contractForm.invalid) {
    this.contractForm.markAllAsTouched();
    this.snackbar.showError('Completează câmpurile obligatorii.', 2500);
    return;
  }

  if (this.isSubmitting) return;

  const confirmed = await this.confirmService.confirm(
    this.isEdit ? 'Confirmare actualizare' : 'Confirmare creare',
    this.isEdit
      ? 'Sigur vrei să salvezi modificările contractului?'
      : 'Sigur vrei să creezi acest contract?'
  );

  if (!confirmed) return;

  const formValue = this.contractForm.getRawValue();

  const updateDto = {
    startDate: formValue.startDate,
    endDate: formValue.isUnlimited ? null : formValue.endDate,
    isUnlimited: formValue.isUnlimited,
    installments: formValue.installments,
    marketingCampaignId: formValue.marketingCampaignId,
    discounts: formValue.discounts
  };

  this.isSubmitting = true;

  if (this.isEdit) {
    this.contractsService.update(this.contractId, updateDto).subscribe({
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

    return;
  }

  const createDto = {
    ...updateDto,
    studentId: formValue.studentId,
    guardianId: formValue.guardianId,
    courseSessionIds: formValue.courseSessionIds
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
    return date.substring(0, 10);
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

  getFeeTypeLabel(type: number | string): string {
    if (Number(type) === 1) return 'Pachet fix';
    if (Number(type) === 2) return 'Abonament lunar';
    return '-';
  }
}