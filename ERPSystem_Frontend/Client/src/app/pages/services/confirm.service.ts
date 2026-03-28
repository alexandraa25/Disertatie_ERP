import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ConfirmService {

  private resolver!: (value: boolean) => void;

  isOpen = false;
  message = '';
  title = 'Confirmare';

  confirm(message: string, title: string = 'Confirmare'): Promise<boolean> {
    this.message = message;
    this.title = title;
    this.isOpen = true;

    return new Promise<boolean>((resolve) => {
      this.resolver = resolve;
    });
  }

  resolve(result: boolean) {
    this.isOpen = false;
    this.resolver(result);
  }
}