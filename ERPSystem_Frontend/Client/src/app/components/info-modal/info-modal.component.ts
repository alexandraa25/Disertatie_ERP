import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-info-modal',
  standalone: true,
  templateUrl: './info-modal.component.html',
  styleUrls: ['./info-modal.component.css'],
  imports: [CommonModule]
})
export class InfoModalComponent {

  title = '';
  message = '';
  copyText = '';

  isOpen = false;

  @Output() closed = new EventEmitter<void>();

  open(title: string, message: string, copyText: string = '') {
    this.title = title;
    this.message = message;
    this.copyText = copyText;
    this.isOpen = true;
  }

  close() {
    this.isOpen = false;
    this.closed.emit();
  }

  copy() {
    if (this.copyText) {
      navigator.clipboard.writeText(this.copyText);
    }
  }
}