import { Component, ViewChild, ElementRef, AfterViewInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ContractsService } from '../../services/contracts.service';
import { AdditionalActService } from '../../services/additional-act.service';
import { CommonModule } from '@angular/common';

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

  constructor(
    private contracts: ContractsService,
    private additionalActs: AdditionalActService,
    private dialogRef: MatDialogRef<AdminSignatureModalComponent>,
   @Inject(MAT_DIALOG_DATA) public data: { id: number, type: 'contract' | 'act' },
  ) {}

 ngAfterViewInit() {
  const canvas = this.canvas.nativeElement;

  canvas.width = canvas.offsetWidth;
  canvas.height = 220;

  this.ctx = canvas.getContext('2d')!;

  this.ctx.lineWidth = 2;
  this.ctx.lineCap = 'round';
  this.ctx.strokeStyle = '#111'; // 🔥 după setarea width
}

  startDrawing(event: MouseEvent) {
  this.drawing = true;

  const rect = this.canvas.nativeElement.getBoundingClientRect();

  const x = event.clientX - rect.left;
  const y = event.clientY - rect.top;

  this.ctx.beginPath();
  this.ctx.moveTo(x, y);
}

  draw(event: MouseEvent) {
  if (!this.drawing) return;

  const rect = this.canvas.nativeElement.getBoundingClientRect();

  const x = event.clientX - rect.left;
  const y = event.clientY - rect.top;

  this.ctx.lineTo(x, y);
  this.ctx.stroke();
}

  stopDrawing() {
    this.drawing = false;
  }

  clear() {

    const canvas = this.canvas.nativeElement;

    this.ctx.clearRect(0, 0, canvas.width, canvas.height);
  }

  sign() {

  const canvas = this.canvas.nativeElement;
  const signature = canvas.toDataURL();

  this.loading = true;

  let request$;

  if (this.data.type === 'act') {
    request$ = this.additionalActs.adminSignAct(this.data.id, signature);
  } else {
    request$ = this.contracts.adminSign(this.data.id, signature);
  }

  request$.subscribe({
    next: () => {

      this.loading = false;
      this.signed = true; // 🔥 asta lipsea

      setTimeout(() => {
        this.dialogRef.close(true);
      }, 800);

    },
    error: () => {
      this.loading = false;
    }
  });
}

hasSignature(): boolean {
  const canvas = this.canvas.nativeElement;
  const blank = document.createElement('canvas');

  blank.width = canvas.width;
  blank.height = canvas.height;

  return canvas.toDataURL() !== blank.toDataURL();
}
}
