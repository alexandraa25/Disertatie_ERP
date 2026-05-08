import { Component, ViewChild, ElementRef, AfterViewInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ContractsService } from '../../services/contracts.service';
import { AdditionalActService } from '../../services/additional-act.service';
import { CommonModule } from '@angular/common';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  selector: 'app-admin-signature-modal',
  templateUrl: './admin-signature-modal.component.html',
  styleUrls: ['./admin-signature-modal.component.css']
})
export class AdminSignatureModalComponent implements AfterViewInit {

  @ViewChild('canvas') canvas!: ElementRef<HTMLCanvasElement>;

  private ctx!: CanvasRenderingContext2D;

  drawing = false;
  loading = false;
  signed = false;
  hasDrawn = false;

  constructor(
    private contracts: ContractsService,
    private additionalActs: AdditionalActService,
    private snackbar: SnackbarService,
    private dialogRef: MatDialogRef<AdminSignatureModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { id: number, type: 'contract' | 'act' },
  ) { }

  ngAfterViewInit() {
    setTimeout(() => {
      const canvas = this.canvas.nativeElement;

      canvas.width = canvas.clientWidth;
      canvas.height = 220;

      this.ctx = canvas.getContext('2d')!;

      this.ctx.lineWidth = 2;
      this.ctx.lineCap = 'round';
      this.ctx.strokeStyle = '#111';
    });
  }

  startDrawing(event: PointerEvent) {
    if (!this.ctx) return;

    event.preventDefault();

    const canvas = this.canvas.nativeElement;
    const rect = canvas.getBoundingClientRect();

    this.drawing = true;
    this.hasDrawn = true;

    this.ctx.beginPath();
    this.ctx.moveTo(
      event.clientX - rect.left,
      event.clientY - rect.top
    );
  }

  draw(event: PointerEvent) {
    if (!this.drawing || !this.ctx) return;

    event.preventDefault();

    const canvas = this.canvas.nativeElement;
    const rect = canvas.getBoundingClientRect();

    this.ctx.lineTo(
      event.clientX - rect.left,
      event.clientY - rect.top
    );

    this.ctx.stroke();
  }

  stopDrawing() {
    this.drawing = false;
  }

  clear() {
    const canvas = this.canvas.nativeElement;
    this.ctx.clearRect(0, 0, canvas.width, canvas.height);
    this.hasDrawn = false;
  }

  sign() {
    if (!this.hasDrawn) {
      this.snackbar.showError('Te rugăm să adaugi semnătura.', 2200);
      return;
    }

    const canvas = this.canvas.nativeElement;
    const signature = canvas.toDataURL();

    this.loading = true;

    const request$ = this.data.type === 'act'
      ? this.additionalActs.adminSignAct(this.data.id, signature)
      : this.contracts.adminSign(this.data.id, signature);

    request$.subscribe({
      next: (res: any) => {
        this.loading = false;

        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Documentul nu a putut fi semnat.',
            2500
          );
          return;
        }

        this.signed = true;
        this.snackbar.showSuccess('Document semnat cu succes.', 1800);

        setTimeout(() => {
          this.dialogRef.close(true);
        }, 800);
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('A apărut o eroare la semnare.', 2500);
      }
    });
  }

  close() {
    this.dialogRef.close(false);
  }

}
