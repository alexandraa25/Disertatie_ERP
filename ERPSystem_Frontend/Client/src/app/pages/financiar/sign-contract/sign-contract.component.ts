import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sign-contract',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sign-contract.component.html',
  styleUrl: './sign-contract.component.css'
})
export class SignContractComponent implements OnInit {

  token!: string;

  @ViewChild('canvas') canvas!: ElementRef<HTMLCanvasElement>;

  private ctx!: CanvasRenderingContext2D;

  drawing = false;
  loading = false;
  signed = false;
  contract: any;

  constructor(
    private route: ActivatedRoute,
    private contracts: ContractsService
  ) {}

  ngOnInit(): void {

  this.token = this.route.snapshot.paramMap.get('token')!;

  this.contracts.getForSigning(this.token)
    .subscribe((res: any) => {

      if (!res?.value) return;

      this.contract = res.value;

    });

    
}

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

  this.contracts.signClient({
    token: this.token,
    signature: signature
  }).subscribe({

    next: () => {
      this.signed = true;
      this.loading = false;
    },

    error: () => {
      this.loading = false;
    }

  });
}

}