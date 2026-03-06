import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-custom-modal',
  standalone: true,
  templateUrl: './confirm-custom-modal.component.html',
  styleUrls: ['./confirm-custom-modal.component.css'],
  imports: [CommonModule]
})
export class ConfirmCustomModalComponent {
  
  @Input() title: string = 'Confirm action';
  @Input() message: string = '';
  @Input() confirmText: string = 'Confirm';
  @Input() cancelText: string = 'Cancel';
  @Input() copyText: string = '';

  @Output() confirmed = new EventEmitter<boolean>();

  isOpen = false;

  open() {
    this.isOpen = true;
  }

  close(result: boolean) {
    this.isOpen = false;
    this.confirmed.emit(result);
  }

  copyToClipboard() {
  navigator.clipboard.writeText(this.copyText);
}
}
