import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';

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
    @Inject(MAT_DIALOG_DATA) public data: {
      remaining: number
    }
  ) {}

  ngOnInit() {
  this.amount = this.data.remaining;
}
   confirm() {

    if (!this.amount || this.amount <= 0) {
      alert('Introdu o sumă validă');
      return;
    }

    if (this.amount > this.data.remaining) {
      alert(`Maxim permis: ${this.data.remaining}`);
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
    this.dialogRef.close(null);
  }
}
