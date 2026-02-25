import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
    providedIn: 'root'
})
export class SnackbarService {

    constructor(private snackBar: MatSnackBar) { }
    showError(message: string, duration: number = 2000) {
        this.snackBar.open(message, '', {   // 🔥 fără buton
            duration,
            horizontalPosition: 'center',
            verticalPosition: 'bottom',
            panelClass: ['snackbar-error']
        });
    }

    showSuccess(message: string, duration: number = 3000) {
        this.snackBar.open(message, 'OK', {
            duration,
            horizontalPosition: 'center',
            verticalPosition: 'bottom',
            panelClass: ['snackbar-success']
        });
    }

}
