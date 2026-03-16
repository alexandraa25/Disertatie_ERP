import { Component, EventEmitter, Input, Output, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompanyService } from '../../services/company.service';

@Component({
  selector: 'app-company-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './company-modal.component.html',
  styleUrl: './company-modal.component.css'
})
export class CompanyModalComponent implements AfterViewChecked {

  @Input() visible = false;
  @Input() company: any = {};

  @Output() close = new EventEmitter();
  @Output() saved = new EventEmitter();

 @ViewChild('signatureCanvas', { static: false })
canvas!: ElementRef<HTMLCanvasElement>;

private ctx!: CanvasRenderingContext2D;
drawing = false;

  loading = false;
  canvasInitialized = false;

  constructor(private service: CompanyService) {}

 onFileSelected(event: any, field: string) {

    const file = event.target.files[0];

    if (!file) return;

    const reader = new FileReader();

    reader.onload = () => {
      this.company[field] = reader.result;
    };

    reader.readAsDataURL(file);

  }

  save() {

    this.loading = true;

    this.service.save(this.company)
      .subscribe({

        next: () => {

          this.loading = false;
          this.saved.emit();
          this.close.emit();

        },
        error: () => {

          this.loading = false;

        }

      });

  }



ngAfterViewChecked() {

  if (this.visible && this.canvas && !this.canvasInitialized) {

    const canvas = this.canvas.nativeElement;

    this.ctx = canvas.getContext('2d')!;

    this.ctx.lineWidth = 2;
    this.ctx.lineCap = 'round';
    this.ctx.strokeStyle = '#000';

    this.canvasInitialized = true;
  }

}

startDrawing(event: MouseEvent){

  this.drawing = true;

  this.ctx.beginPath();

  this.ctx.moveTo(event.offsetX, event.offsetY);

}

draw(event: MouseEvent){

  if(!this.drawing) return;

  this.ctx.lineTo(event.offsetX, event.offsetY);

  this.ctx.stroke();

}

stopDrawing(){

  this.drawing = false;

}

clearSignature(){

  const canvas = this.canvas.nativeElement;

  this.ctx.clearRect(0,0,canvas.width,canvas.height);

}

saveSignature(){

  const canvas = this.canvas.nativeElement;

  this.company.signatureImage = canvas.toDataURL();

}

closeModal() {

  this.canvasInitialized = false;
  this.close.emit();

}

}