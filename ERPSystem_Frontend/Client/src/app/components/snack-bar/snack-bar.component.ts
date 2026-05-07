import { CommonModule } from '@angular/common';
import { Component, Injectable } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})

@Component({
  selector: 'app-snack-bar',
  standalone: true,
   imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSnackBarModule 
  ],
  templateUrl: './snack-bar.component.html',
  styleUrl: './snack-bar.component.css'
})
export class SnackBarComponent {

   constructor(private snackBar: MatSnackBar) {}

  showSuccess(message: string, duration: number = 3000) {
    this.snackBar.open(message, 'OK', {
      duration,
      panelClass: ['snackbar-success']
    });
  }

  showError(message: string, duration: number = 3000) {
    console.log('Displaying error snack bar with message:', message);
    this.snackBar.open(message, 'OK', {
      duration,
      panelClass: ['snackbar-error']
    });
  }

}
