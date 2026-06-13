import { CommonModule } from '@angular/common';
import {Component, EventEmitter, Input, Output} from '@angular/core';
import { DomSanitizer, SafeResourceUrl} from '@angular/platform-browser';

@Component({
  selector: 'app-power-bi-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './power-bi-modal.component.html',
  styleUrls: ['./power-bi-modal.component.css']
})
export class PowerBiModalComponent {

  @Input() isOpen = false;
  @Input() title = '';
  @Input() reports: {
    title: string;
    url: string;
    safeUrl?: SafeResourceUrl;
  }[] = [];

  @Output() closed = new EventEmitter<void>();
  currentIndex = 0;
  
  constructor(private sanitizer: DomSanitizer) {}

  ngOnChanges() {
    this.reports = this.reports.map(r => ({
      ...r,
      safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(r.url)
    }));
  }

  close() {
    this.closed.emit();
  }

  next() {
    if (this.currentIndex < this.reports.length - 1) {
      this.currentIndex++;
    }
  }

  prev() {
    if (this.currentIndex > 0) {
      this.currentIndex--;
    }
  }
}