import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  selector: 'app-pay-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pay-modal.component.html',
  styleUrl: './pay-modal.component.css'
})
export class PayModalComponent {

  amount: number = 0;
  method: string = 'Cash';
  notes: string = '';
  reference: string = '';
  methods = ['Cash', 'Card', 'Transfer'];

  constructor(
    private dialogRef: MatDialogRef<PayModalComponent>,
    private snackbar: SnackbarService,
    @Inject(MAT_DIALOG_DATA) public data: {
      remaining: number
    }
  ) { }

  ngOnInit() {
    this.amount = this.data.remaining;
  }
  confirm() {

    if (!this.amount || this.amount <= 0) {
      this.snackbar.showError('Introdu o sumă validă.', 2200);
      return;
    }

    if (this.amount > this.data.remaining) {
      this.snackbar.showError(
        `Suma maximă permisă este ${this.data.remaining} RON.`,
        2500
      );
      return;
    }

    if (!this.method) {
      this.snackbar.showError('Selectează metoda de plată.', 2200);
      return;
    }

    this.dialogRef.close({
      amount: this.amount,
      method: this.method,
      notes: this.notes,
      reference: this.reference
    });
  }
  
  close() {
    this.snackbar.showError('Plata a fost anulată.', 1500);
    this.dialogRef.close(null);
  }
}
