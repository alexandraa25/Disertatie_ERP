import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { ViewChild, ElementRef } from '@angular/core';
import html2pdf from 'html2pdf.js';
import { DomSanitizer } from '@angular/platform-browser';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminSignatureModalComponent } from '../admin-signature-modal/admin-signature-modal.component';

@Component({
  selector: 'app-contract-details',
  standalone: true,
  imports: [CommonModule, FormsModule, QuillModule],
  templateUrl: './contract-details.component.html',
  styleUrls: ['./contract-details.component.css']
})
export class ContractDetailsComponent implements OnInit {

  contract: any;
  loading = true;
  actionLoading = false;
  isEditingBody = false;
  editedBody = '';
  showPreview = false;
  hasUnsavedChanges = false;

  @ViewChild('pdfContent') pdfContent!: ElementRef;
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contractsService: ContractsService,
    private dialog: MatDialog, 
    private sanitizer: DomSanitizer

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
            console.error('Contract not found');
            this.loading = false;
            return;
          }

          this.contract = res.value; // 🔥 FOARTE IMPORTANT
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.loading = false;
        }
      });
  }

  finalize() {
    if (!confirm('⚠️ După finalizare contractul devine blocat și nu mai poate fi modificat.\n\nContinui?')) {
      return;
    }

    this.runAction(() =>
      this.contractsService.finalize(this.contract.id)
    );
  }

  // sign() {
  //   this.runAction(() =>
  //     this.contractsService.sign(this.contract.id)
  //   );
  // }

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

  cancel() {
    this.runAction(() =>
      this.contractsService.cancel(this.contract.id)
    );
  }

  private runAction(action: () => any) {
    if (this.actionLoading) return;

    this.actionLoading = true;

    action().subscribe({
      next: () => {
        this.loadContract(this.contract.id);
        this.actionLoading = false;
      },
      error: () => {
        this.actionLoading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Draft': return 'badge draft';
      case 'Finalized': return 'badge finalized';
      case 'Signed': return 'badge signed';
      case 'Active': return 'badge active';
      case 'Cancelled': return 'badge cancelled';
      default: return 'badge';
    }
  }

  startEditBody() {
    this.isEditingBody = true;
    this.showPreview = false;
    this.editedBody = this.formatForEditor(this.contract.contractBody);
    this.hasUnsavedChanges = false;
  }
  formatForEditor(html: string) {
    return html || '';
  }
  cancelEditBody() {
    this.isEditingBody = false;
  }

 saveBody() {

  const cleaned = this.cleanHtml(this.editedBody);
  this.actionLoading = true;

  this.contractsService.updateBody(this.contract.id, {
    contractBody: cleaned // ✔️ DOAR asta
  }).subscribe({
    next: () => {
      this.contract.contractBody = cleaned; // 🔥 sincronizezi UI cu ce ai salvat
      this.isEditingBody = false;
      this.hasUnsavedChanges = false;
      this.actionLoading = false;
    },
    error: () => this.actionLoading = false
  });
}

cleanHtml(html: string): string {
  return html
    .replace(/<p><br><\/p>/g, '')
    .trim();
}

  resetBody() {

    if (!confirm('Se vor pierde modificările manuale. Continui?')) return;

    this.actionLoading = true;

    this.contractsService.resetBody(this.contract.id)
      .subscribe({
        next: () => {
          this.loadContract(this.contract.id);
          this.actionLoading = false;
        },
        error: () => this.actionLoading = false
      });
  }

  sendToClient() {
    this.runAction(() =>
      this.contractsService.send(this.contract.id)
    );
  }

  downloadPdf() {
    this.contractsService.download(this.contract.id)
      .subscribe((blob: Blob) => {

        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = `contract_${this.contract.contractNumber}.pdf`;
        a.click();

        window.URL.revokeObjectURL(url);
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

  previewPdf() {
    const element = this.pdfContent.nativeElement;

    html2pdf()
      .from(element)
      .set({
        margin: 10,
        filename: 'contract-preview.pdf',
        html2canvas: { scale: 2 },
        jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
      })
      .save();
  }

  suspend() {
    if (!confirm('Sigur vrei să suspendi contractul?')) return;

    this.runAction(() =>
      this.contractsService.suspend(this.contract.id)
    );
  }

  resume() {
    this.runAction(() =>
      this.contractsService.resume(this.contract.id)
    );
  }

  complete() {
    if (!confirm('Marchezi contractul ca finalizat?')) return;

    this.runAction(() =>
      this.contractsService.complete(this.contract.id)
    );
  }


  restoreTemplateFromHtml(html: string): string {
    if (!html) return '';

    return html
      .replace(/<span[^>]*>(.*?)<\/span>/g, (_, value) => {
        // map invers (simplu)
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