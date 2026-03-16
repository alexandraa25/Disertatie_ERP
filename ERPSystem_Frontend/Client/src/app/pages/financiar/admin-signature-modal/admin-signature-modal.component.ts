import { Component, ViewChild, ElementRef, AfterViewInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ContractsService } from '../../services/contracts.service';

@Component({
  selector: 'app-admin-signature-modal',
  templateUrl: './admin-signature-modal.component.html'
})
export class AdminSignatureModalComponent implements AfterViewInit {

  @ViewChild('canvas') canvas!: ElementRef<HTMLCanvasElement>;

  private ctx!: CanvasRenderingContext2D;

  drawing = false;
  loading = false;

  constructor(
    private contracts: ContractsService,
    private dialogRef: MatDialogRef<AdminSignatureModalComponent>,
    @Inject(MAT_DIALOG_DATA) public contractId: number
  ) {}

  ngAfterViewInit() {

    const canvas = this.canvas.nativeElement;

    this.ctx = canvas.getContext('2d')!;

    this.ctx.lineWidth = 2;
    this.ctx.lineCap = 'round';
  }

  startDrawing(event: MouseEvent) {
    this.drawing = true;
    this.ctx.beginPath();
    this.ctx.moveTo(event.offsetX, event.offsetY);
  }

  draw(event: MouseEvent) {

    if (!this.drawing) return;

    this.ctx.lineTo(event.offsetX, event.offsetY);
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

    this.contracts.adminSign(this.contractId, signature)
      .subscribe(() => {

        this.dialogRef.close(true);

      });

  }

}