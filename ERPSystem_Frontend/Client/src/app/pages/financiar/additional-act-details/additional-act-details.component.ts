import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AdditionalActService } from '../../services/additional-act.service';
import { MatDialog } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { AdminSignatureModalComponent } from '../admin-signature-modal/admin-signature-modal.component';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { ConfirmService } from '../../services/confirm.service';


@Component({
  selector: 'app-additional-act-details',
  standalone: true,
  imports: [CommonModule, FormsModule, QuillModule, ConfirmCustomModalComponent],
  templateUrl: './additional-act-details.component.html',
  styleUrl: './additional-act-details.component.css'
})
export class AdditionalActDetailsComponent implements OnInit {

  act: any;
  isEditingBody = false;
  editedBody = '';
  loading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private additionalActService: AdditionalActService,
    private dialog: MatDialog,
    private snackbar: SnackbarService,
    private confirmService: ConfirmService
  ) { }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.loadAct(Number(id));
    }
  }

  loadAct(id: number) {
    this.loading = true;

    this.additionalActService.getById(id).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false || !res?.value) {
          this.snackbar.showError('Actul adițional nu a fost găsit.', 2500);
          this.loading = false;
          return;
        }

        this.act = res.value;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Actul adițional nu a putut fi încărcat.', 2500);
      }
    });
  }

  startEditBody() {
    if (this.act.status !== 'Draft') {
      this.snackbar.showError('Actul nu mai poate fi editat.', 2500);
      return;
    }

    this.isEditingBody = true;
    this.editedBody = this.act.body;
  }

  formatForEditor(text: string | null | undefined) {
    if (!text) return '';
    return text.replace(/\n/g, '<br>');
  }

  saveBody() {
    this.loading = true;

    this.additionalActService.updateBody(this.act.id, this.editedBody)
      .subscribe({
        next: (res: any) => {
          if (res?.isSuccess === false) {
            this.snackbar.showError(res.error?.errorMessage || 'Textul nu a putut fi salvat.', 2500);
            this.loading = false;
            return;
          }

          this.act.body = this.editedBody;
          this.isEditingBody = false;
          this.loading = false;

          this.snackbar.showSuccess('Textul actului a fost salvat.', 1800);
        },
        error: () => {
          this.loading = false;
          this.snackbar.showError('Eroare la salvarea textului.', 2500);
        }
      });
  }

  cancelEditBody() {
    this.isEditingBody = false;
  }

  goBack() {
    if (this.act?.studentId) {
      this.router.navigate(['/students', this.act.studentId]);
      return;
    }

    this.router.navigate(['/students']);
  }

  editAct(id: number) {
    this.router.navigate(['/additional-act/edit', id]);

  }

  async finalizeAct() {
    const confirmed = await this.confirmService.confirm(
      'Sigur finalizezi actul adițional?',
      'Confirmare'
    );

    if (!confirmed) return;

    this.loading = true;

    this.additionalActService.finalize(this.act.id).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Actul nu a putut fi finalizat.', 2500);
          this.loading = false;
          return;
        }

        this.snackbar.showSuccess('Act finalizat cu succes.', 1800);
        this.loadAct(this.act.id);
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Eroare la finalizarea actului.', 2500);
      }
    });
  }

  async sendToClient() {
    const confirmed = await this.confirmService.confirm(
      'Trimite actul adițional către client?',
      'Confirmare'
    );

    if (!confirmed) return;

    this.loading = true;

    this.additionalActService.sendToClient(this.act.id).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Actul nu a putut fi trimis.', 2500);
          this.loading = false;
          return;
        }

        this.snackbar.showSuccess('Act trimis către client.', 1800);
        this.loadAct(this.act.id);
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Eroare la trimiterea actului.', 2500);
      }
    });
  }


  getStatusName(status: number) {
    switch (status) {
      case 0: return 'Draft';
      case 1: return 'Finalized';
      case 2: return 'SentToClient';
      case 3: return 'SignedByClient';
      case 4: return 'Active';
      default: return '';
    }
  }

  openAdminSignature() {

    const dialogRef = this.dialog.open(AdminSignatureModalComponent, {
      width: '600px',
      data: {
        id: this.act.id,
        type: 'act'
      }
    });

    dialogRef.afterClosed().subscribe(result => {

      if (result) {
        this.loadAct(this.act.id); // 🔥 refresh după semnare
      }

    });
  }

  downloadAct() {
    this.additionalActService.downloadAct(this.act.id)
      .subscribe({
        next: (blob) => {
          this.additionalActService.saveFile(blob, `Act_${this.act.actNumber}.pdf`);
          this.snackbar.showSuccess('Act descărcat cu succes.', 1800);
        },
        error: () => {
          this.snackbar.showError('Actul nu a putut fi descărcat.', 2500);
        }
      });
  }

  cleanBody(html: string): string {
    if (!html) return '';

    return html
      .replace(/<p>\s*<\/p>/g, '')
      .replace(/<p><br><\/p>/g, '')
      .replace(/(<br\s*\/?>\s*){2,}/gi, '<br>')
      .replace(/\n{2,}/g, '\n')
      .trim();
  }

  async deleteAct(): Promise<void> {
    const confirmed = await this.confirmService.confirm(
      'Sigur dorești să ștergi acest act adițional?',
      'Confirmare ștergere'
    );

    if (!confirmed) return;

    this.loading = true;

    this.additionalActService.delete(this.act.id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Actul adițional a fost șters.');
        this.router.navigate(['/students', this.act.studentId]);
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Actul adițional nu a putut fi șters.');
      }
    });
  }

}