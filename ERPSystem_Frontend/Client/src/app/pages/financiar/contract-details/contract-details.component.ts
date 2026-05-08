import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { DomSanitizer } from '@angular/platform-browser';
import { MatDialog } from '@angular/material/dialog';
import { AdminSignatureModalComponent } from '../admin-signature-modal/admin-signature-modal.component';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';

@Component({
  selector: 'app-contract-details',
  standalone: true,
  imports: [CommonModule, FormsModule, QuillModule, ConfirmCustomModalComponent],
  templateUrl: './contract-details.component.html',
  styleUrls: ['./contract-details.component.css']
})
export class ContractDetailsComponent implements OnInit {

  contract: any;
  loading = true;
  actionLoading = false;
  isEditingBody = false;
  editedBody = '';
  hasUnsavedChanges = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contractsService: ContractsService,
    private dialog: MatDialog,
    private sanitizer: DomSanitizer,
    private snackbar: SnackbarService,
    private confirmService: ConfirmService

  ) { }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadContract(id);
  }

  loadContract(id: number) {
    this.loading = true;

    this.contractsService.getById(id)
      .subscribe({
        next: (res) => {

          console.log('RESPONSE:', res);

          if (!res.isSuccess || !res.value) {
            this.snackbar.showError('Contractul nu a fost găsit.', 2500);
            this.loading = false;
            return;
          }

          this.contract = res.value;
          this.loading = false;
        },
        error: () => {
          this.snackbar.showError('Contractul nu a putut fi încărcat.', 2500);
          this.loading = false;
        }
      });
  }

  async finalize() {
    const confirmed = await this.confirmService.confirm(
      'Confirmare finalizare',
      'După finalizare, contractul devine blocat și nu mai poate fi modificat. Continui?'
    );

    if (!confirmed) return;

    this.runAction(() =>
      this.contractsService.finalize(this.contract.id)
    );
  }

  activate() {
    const dialogRef = this.dialog.open(AdminSignatureModalComponent, {
      width: '600px',
      data: {
        id: this.contract.id,
        type: 'contract'
      }
    });

    dialogRef.afterClosed().subscribe(result => {

      if (result) {
        this.loadContract(this.contract.id);
      }

    });
  }

  async cancel() {
    const confirmed = await this.confirmService.confirm(
      'Confirmare anulare',
      'Sigur vrei să anulezi acest contract?'
    );

    if (!confirmed) return;

    this.runAction(() =>
      this.contractsService.cancel(this.contract.id)
    );
  }

  private runAction(action: () => any) {
    if (this.actionLoading) return;

    this.actionLoading = true;

    action().subscribe({
      next: (res: any) => {
        this.actionLoading = false;

        if (res?.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Acțiunea nu a putut fi finalizată.', 2500);
          return;
        }

        this.snackbar.showSuccess('Acțiune realizată cu succes.', 1800);
        this.loadContract(this.contract.id);
      },
      error: () => {
        this.actionLoading = false;
        this.snackbar.showError('A apărut o eroare.', 2500);
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Draft': return 'badge draft';
      case 'Finalized': return 'badge finalized';
      case 'SentToClient': return 'badge sent';
      case 'SignedByClient': return 'badge signed-client';
      case 'Active': return 'badge active';
      case 'Completed': return 'badge completed';
      case 'Expired': return 'badge expired';
      case 'Cancelled': return 'badge cancelled';
      default: return 'badge';
    }
  }

  startEditBody() {
    this.isEditingBody = true;
    this.editedBody = this.formatForEditor(this.contract.contractBody);
    this.hasUnsavedChanges = false;
  }

  formatForEditor(html: string) {
    return html || '';
  }

  async cancelEditBody() {
    if (this.hasUnsavedChanges) {
      const confirmed = await this.confirmService.confirm(
        'Modificări nesalvate',
        'Ai modificări nesalvate. Sigur vrei să le pierzi?'
      );

      if (!confirmed) return;
    }

    this.isEditingBody = false;
    this.editedBody = '';
    this.hasUnsavedChanges = false;
  }

  saveBody() {

    const cleaned = this.cleanHtml(this.editedBody);
    this.actionLoading = true;

    this.contractsService.updateBody(this.contract.id, {
      contractBody: cleaned // ✔️ DOAR asta
    }).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Textul contractului nu a putut fi salvat.', 2500);
          this.actionLoading = false;
          return;
        }

        this.contract.contractBody = cleaned;
        this.isEditingBody = false;
        this.hasUnsavedChanges = false;
        this.actionLoading = false;

        this.snackbar.showSuccess('Textul contractului a fost salvat.', 1800);
      },
      error: () => {
        this.actionLoading = false;
        this.snackbar.showError('Eroare la salvarea textului contractului.', 2500);
      }
    });
  }

  cleanHtml(html: string): string {
    return html
      .replace(/<p><br><\/p>/g, '')
      .trim();
  }

  async resetBody() {

    const confirmed = await this.confirmService.confirm(
      'Confirmare resetare',
      'Se vor pierde modificările manuale. Continui?'
    );

    if (!confirmed) return;

    this.actionLoading = true;

    this.contractsService.resetBody(this.contract.id)
      .subscribe({
        next: (res: any) => {

          this.actionLoading = false;

          if (res?.isSuccess === false) {
            this.snackbar.showError(
              res.error?.errorMessage || 'Template-ul nu a putut fi resetat.',
              2500
            );
            return;
          }

          this.snackbar.showSuccess(
            'Template resetat cu succes.',
            1800
          );

          this.loadContract(this.contract.id);
        },
        error: () => {

          this.actionLoading = false;

          this.snackbar.showError(
            'Eroare la resetarea template-ului.',
            2500
          );
        }
      });
  }

  sendToClient() {
    this.runAction(() =>
      this.contractsService.send(this.contract.id)
    );
  }

  downloadPdf() {
    this.contractsService.download(this.contract.id)
      .subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);

          const a = document.createElement('a');
          a.href = url;
          a.download = `contract_${this.contract.contractNumber}.pdf`;
          a.click();

          window.URL.revokeObjectURL(url);

          this.snackbar.showSuccess('Contract descărcat cu succes.', 1800);
        },
        error: () => {
          this.snackbar.showError('Contractul nu a putut fi descărcat.', 2500);
        }
      });
  }

  quillConfig = {
    toolbar: [
      ['bold', 'italic', 'underline'],
      [{ 'header': [1, 2, 3, false] }],
      [{ 'list': 'ordered' }, { 'list': 'bullet' }],
      [{ 'align': [] }],
      ['clean']
    ]
  };

  async complete() {
    const confirmed = await this.confirmService.confirm(
      'Confirmare finalizare contract',
      'Sigur vrei să marchezi contractul ca finalizat?'
    );

    if (!confirmed) return;

    this.runAction(() =>
      this.contractsService.complete(this.contract.id)
    );
  }

  restoreTemplateFromHtml(html: string): string {
    if (!html) return '';

    return html
      .replace(/<span[^>]*>(.*?)<\/span>/g, (_, value) => {
        if (value === this.contract.companyName) return '{{CompanyName}}';
        if (value === this.contract.total?.toString()) return '{{Total}}';
        if (value === this.contract.contractNumber) return '{{ContractNumber}}';

        return value; // fallback
      });
  }

  canDeactivate(): boolean {
    if (this.hasUnsavedChanges) {
      return confirm('Ai modificări nesalvate. Sigur vrei să pleci?');
    }
    return true;
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: any) {
    if (this.hasUnsavedChanges) {
      $event.returnValue = true;
    }
  }

  goBack() {
    this.router.navigate(['/students']);
  }

  editContract() {
    this.router.navigate(['/contracts/edit', this.contract.id]);
  }

  get safeBody() {
    return this.sanitizer.bypassSecurityTrustHtml(this.contract.contractBody);
  }
}