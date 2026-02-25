import { CommonModule, NgClass } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-popup-message',
  templateUrl: './popup-message.component.html',
  styleUrls: ['./popup-message.component.css'],
  standalone: true,
  imports: [NgClass, CommonModule],
})
export class PopupMessageComponent {
  @Input() title: string = 'Notification';
  @Input() message: string = 'This is a notification message.';
  @Input() isError: boolean = false;
  @Input() isVisible: boolean = false;

  @Output() onClose = new EventEmitter<void>();
  @Input() confirmMode = false;
  @Output() confirm = new EventEmitter<void>();

  closePopup(): void {
    this.isVisible = false;
    this.onClose.emit();
  }
}
