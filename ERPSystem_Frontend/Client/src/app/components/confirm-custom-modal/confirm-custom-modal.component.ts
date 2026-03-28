import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ConfirmService } from '../../pages/services/confirm.service';

@Component({
  selector: 'app-confirm-custom-modal',
  standalone: true,
  templateUrl: './confirm-custom-modal.component.html',
  styleUrls: ['./confirm-custom-modal.component.css'],
  imports: [CommonModule]
})
export class ConfirmCustomModalComponent {

  constructor(public confirmService: ConfirmService) {}

  close(result: boolean) {
    this.confirmService.resolve(result);
  }
}