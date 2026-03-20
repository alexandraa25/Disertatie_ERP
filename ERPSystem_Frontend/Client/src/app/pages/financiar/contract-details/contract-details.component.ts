import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { ViewChild, ElementRef } from '@angular/core';
import html2pdf from 'html2pdf.js';

@Component({
  selector: 'app-contract-details',
  standalone: true,
  imports: [CommonModule, FormsModule, QuillModule],
  templateUrl: './contract-details.component.html',
styleUrls: ['./contract-details.component.css']})
export class ContractDetailsComponent implements OnInit {

  contract: any;
  loading = true;
  actionLoading = false;
  isEditingBody = false;
editedBody = '';

@ViewChild('pdfContent') pdfContent!: ElementRef;
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contractsService: ContractsService
   
  ) {}

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
    this.runAction(() =>
      this.contractsService.finalize(this.contract.id)
    );
  }

  sign() {
    this.runAction(() =>
      this.contractsService.sign(this.contract.id)
    );
  }

  activate() {
    this.runAction(() =>
      this.contractsService.activate(this.contract.id)
    );
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

  this.editedBody = this.formatContractBody(
    this.contract.contractBody
  );
}

cancelEditBody() {
  this.isEditingBody = false;
}

saveBody() {
  this.actionLoading = true;

  const restoredTemplate = this.restoreTemplateFromHtml(this.editedBody);

  this.contractsService.updateBody(this.contract.id, {
    contractBody: restoredTemplate
  }).subscribe({
    next: () => {
      this.contract.contractBody = restoredTemplate;
      this.isEditingBody = false;
      this.actionLoading = false;
    },
    error: () => this.actionLoading = false
  });
}


resetBody() {

  if (!confirm('Sigur vrei reset?')) return;

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
    [{ 'list': 'ordered'}, { 'list': 'bullet' }],
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


formatContractBody(template: string): string {
  if (!template || !this.contract) return '';

  const map: any = {
    ContractNumber: this.contract.contractNumber,
    Date: this.contract.date,
    CompanyName: this.contract.companyName,
    CompanyAddress: this.contract.companyAddress,
    CompanyRegistration: this.contract.companyRegistration,
    CompanyCui: this.contract.companyCui,
    CompanyIban: this.contract.companyIban,
    CompanyBank: this.contract.companyBank,
    CompanyEmail: this.contract.companyEmail,
    CompanyPhone: this.contract.companyPhone,
    BeneficiaryName: this.contract.beneficiaryName,
    BeneficiaryAddress: this.contract.beneficiaryAddress,
    BeneficiaryEmail: this.contract.beneficiaryEmail,
    BeneficiaryPhone: this.contract.beneficiaryPhone,
    Courses: this.contract.courses,
    Students: this.contract.students,
    ContractPeriod: this.contract.contractPeriod,
    Subtotal: this.contract.subtotal,
    Discount: this.contract.discount,
    Total: this.contract.total,
    Installments: this.contract.installments
  };

  return template.replace(/{{(.*?)}}/g, (_, key) => {
    const value = map[key.trim()] ?? '';

    return `<span contenteditable="false" class="protected-field">${value}</span>`;
  });
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
}