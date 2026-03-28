import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { CommonModule } from '@angular/common';
import { DomSanitizer } from '@angular/platform-browser';

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
  document: any;
  documentType: 'contract' | 'act' = 'contract';

  constructor(
    private route: ActivatedRoute,
    private contracts: ContractsService,
    private sanitizer: DomSanitizer
  ) { }

  ngOnInit(): void {

    this.token = this.route.snapshot.paramMap.get('token')!;

    this.contracts.getForSigning(this.token)
      .subscribe((res: any) => {

        this.document = res.value;

        // detectare tip (adaptare după backend)
        this.documentType = res.value.type ||
          (res.value.contractBody ? 'contract' : 'act');
      });


  }

  get safeBody() {
    const html = this.document?.contractBody || this.document?.body;
    return this.sanitizer.bypassSecurityTrustHtml(html || '');
  }

  ngAfterViewInit() {
    const canvas = this.canvas.nativeElement;

    this.ctx = canvas.getContext('2d')!;

    this.ctx.lineWidth = 2;
    this.ctx.lineCap = 'round';
    this.ctx.strokeStyle = '#000';
  }

  startDrawing(event: MouseEvent) {
    const rect = this.canvas.nativeElement.getBoundingClientRect();

    this.drawing = true;
    this.ctx.beginPath();
    this.ctx.moveTo(
      event.clientX - rect.left,
      event.clientY - rect.top
    );
  }

  draw(event: MouseEvent) {
    if (!this.drawing) return;

    const rect = this.canvas.nativeElement.getBoundingClientRect();

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

  hasSignature(): boolean {
  const canvas = this.canvas.nativeElement;
  const blank = document.createElement('canvas');

  blank.width = canvas.width;
  blank.height = canvas.height;

  return canvas.toDataURL() !== blank.toDataURL();
}

}