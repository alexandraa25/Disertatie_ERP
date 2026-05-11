import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatNativeDateModule } from '@angular/material/core';
import { Component } from '@angular/core';
import { LeaveService } from '../../services/leave.service';
import { Inject } from '@angular/core';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';


@Component({
  selector: 'app-create-leave-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatButtonModule, MatSelectModule],
  templateUrl: './create-leave-modal.component.html',
  styleUrl: './create-leave-modal.component.css'
})

export class CreateLeaveModalComponent {
  leaveForm: FormGroup;
  holidays: string[] = [];
  isEditMode = false;

  constructor(
    private fb: FormBuilder,
    private leaveService: LeaveService,
    private snackbar: SnackbarService,
    private dialogRef: MatDialogRef<CreateLeaveModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.leaveForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      leaveType: ['Vacation', Validators.required],
      reason: ['']
    });
  }

  ngOnInit() {
    this.loadHolidays();
    if (this.data) {
      this.isEditMode = true;

      this.leaveForm.patchValue({
        startDate: this.parseDateOnly(this.data.startDate),
        endDate: this.parseDateOnly(this.data.endDate),
        leaveType: this.data.leaveType,
        reason: this.data.reason
      });

      this.leaveForm.get('reason')?.setValidators([Validators.required]);
      this.leaveForm.get('reason')?.updateValueAndValidity();
    }
  }

  dateFilter = (date: Date | null): boolean => {
    if (!date) return false;

    const day = date.getDay();

    const localIso =
      date.getFullYear() + '-' +
      String(date.getMonth() + 1).padStart(2, '0') + '-' +
      String(date.getDate()).padStart(2, '0');

    const isWeekend = day === 0 || day === 6;
    const isHoliday = this.holidays.includes(localIso);

    return !isWeekend && !isHoliday;
  };

  dateClass = (date: Date) => {
    const day = date.getDay();

    const localIso =
      date.getFullYear() + '-' +
      String(date.getMonth() + 1).padStart(2, '0') + '-' +
      String(date.getDate()).padStart(2, '0');

    if (this.holidays.includes(localIso)) {
      return 'holiday-date';
    }

    if (day === 0 || day === 6) {
      return 'weekend-date';
    }

    return '';
  };

  loadHolidays() {
    const year = new Date().getFullYear();

    this.leaveService.getHolidays(year).subscribe(res => {
      console.log('HOLIDAYS FROM API:', res); // 🔥 debug

      this.holidays = res;
    });
  }

  close() {
    this.dialogRef.close();
  }

  submit() {

    if (this.leaveForm.invalid) {
      this.leaveForm.markAllAsTouched();

      this.snackbar.showError(
        'Completează toate câmpurile obligatorii.',
        2500
      );

      return;
    }

    const value = { ...this.leaveForm.value };

    if (value.endDate < value.startDate) {
      this.leaveForm.get('endDate')?.setErrors({
        invalidRange: true
      });

      this.leaveForm.get('endDate')?.markAsTouched();

      this.snackbar.showError(
        'Data de sfârșit trebuie să fie după data de început.',
        3000
      );

      return;
    }

    value.startDate = this.toDateOnlyString(value.startDate);
    value.endDate = this.toDateOnlyString(value.endDate);

    if (!this.isEditMode) {
      delete value.reason;
    }



    this.snackbar.showSuccess(
      this.isEditMode
        ? 'Concediul a fost actualizat.'
        : 'Cererea de concediu este pregătită.',
      1800
    );

    this.dialogRef.close(value);
  }

  hasError(controlName: string, errorName: string): boolean {
    const control = this.leaveForm.get(controlName);
    return !!control && control.hasError(errorName) && control.touched;
  }

  private toDateOnlyString(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private parseDateOnly(value: string): Date {
    const dateOnly = value.substring(0, 10);
    const [year, month, day] = dateOnly.split('-').map(Number);

    return new Date(year, month - 1, day);
  }
}
