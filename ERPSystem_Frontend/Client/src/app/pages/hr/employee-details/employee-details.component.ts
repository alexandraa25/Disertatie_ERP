import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';
import { LeaveService } from '../../services/leave.service';
import { CommonModule  } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ActivityLogService } from '../../services/activity-log.service'
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';

@Component({
  selector: 'app-employee-details',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmCustomModalComponent],
  templateUrl: './employee-details.component.html',
  styleUrls: ['./employee-details.component.css']
})
export class EmployeeDetailsComponent implements OnInit {

  employee: any;
  loading = true;
  activeTab: 'info' | 'documents' | 'leaves' | 'audit' = 'info';

  isEditMode = false;
  editModel: any = {};
  selectedFiles: File[] = [];

  documentType: string = 'Contract';
  customDocumentType: string = '';

  activityLogs: any[] = [];
  activityLoaded = false;

  leavePage = 1;
leavePageSize = 5;

auditPage = 1;
auditPageSize = 5;



  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private leaveService: LeaveService,
    private activityLogService: ActivityLogService,
    private snackbar: SnackbarService,
     private confirmService: ConfirmService, 
     
    
  ) { }

  ngOnInit(): void {
    this.loadEmployee();
  }

  approveLeave(id: string) {
    this.leaveService.approve(id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Cererea a fost aprobată.', 1800);
        this.loadEmployee();
      },
      error: () => {
        this.snackbar.showError('Cererea nu a putut fi aprobată.', 2500);
      }
    });
  }

  rejectLeave(leave: any) {
    const reason = prompt('Motiv respingere:');

    if (!reason) return;

    this.leaveService.reject(leave.id, reason).subscribe({
      next: () => {
        this.snackbar.showSuccess('Cererea a fost respinsă.', 1800);
        this.loadEmployee();
      },
      error: () => {
        this.snackbar.showError('Cererea nu a putut fi respinsă.', 2500);
      }
    });
  }


  loadEmployee() {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.snackbar.showError('ID lipsă din URL.', 2500);
      return;
    }

    this.loading = true;

    this.employeeService.getEmployeeById(id).subscribe({
      next: (res) => {
        this.employee = res.value;
        this.leavePage = 1;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.showError('Nu s-au putut încărca datele angajatului.', 2500);
      }
    });
  }

  enableEdit() {
    this.editModel = structuredClone(this.employee);

    this.editModel.hireDate = this.formatDate(this.employee.hireDate);

    this.isEditMode = true;
  }

  private formatDate(date: string | Date): string | null {
    if (!date) return null;

    return new Date(date).toISOString().split('T')[0];
  }

  cancelEdit() {
    this.isEditMode = false;
    this.editModel = structuredClone(this.employee);
  }

  save() {
    this.employeeService.updateEmployee(this.editModel).subscribe({
      next: (res) => {
        if (!res.isSuccess) {
          this.snackbar.showError(res.error?.errorMessage || 'Nu s-au putut salva modificările.', 2500);
          return;
        }

        this.employee = structuredClone(this.editModel);
        this.isEditMode = false;

        this.snackbar.showSuccess('Datele angajatului au fost actualizate.', 1800);
      },
      error: () => {
        this.snackbar.showError('Eroare la salvarea modificărilor.', 2500);
      }
    });
  }


  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) return;

    this.selectedFiles = [input.files[0]];

    input.value = '';
  }

  removeFile(): void {
    this.selectedFiles = [];
  }

  uploadDocuments(): void {
    if (!this.selectedFiles.length) {
      this.snackbar.showError('Selectează un fișier înainte de upload.', 2200);
      return;
    }

    const formData = new FormData();

    const file = this.selectedFiles[0];

    const docType =
      this.documentType === 'custom'
        ? this.customDocumentType
        : this.documentType;

    if (this.documentType === 'custom' && !this.customDocumentType?.trim()) {
      this.snackbar.showError('Introdu tipul documentului.', 2200);
      return;
    }

    formData.append('EmployeeId', this.employee.id);
    formData.append('File', file, file.name);
    formData.append('DocumentType', docType || 'Document');

    this.employeeService.uploadEmployeeDocuments(formData).subscribe({
      next: (res) => {
        if (!res.isSuccess) {
          this.snackbar.showError(res.error?.errorMessage || 'Documentul nu a putut fi încărcat.', 2500);
          return;
        }

        this.selectedFiles = [];
        this.snackbar.showSuccess('Document încărcat cu succes.', 1800);
        this.loadEmployee();
      },
      error: () => {
        this.snackbar.showError('Eroare la încărcarea documentului.', 2500);
      }
    });
  }

  onDocTypeChange() {
    this.customDocumentType = '';
  }

  getResolvedDocType(): string {
    if (this.documentType === 'custom') {
      return this.customDocumentType || 'Custom';
    }
    return this.documentType;
  }

  openDocument(doc: any): void {
    this.employeeService.viewDocument(doc.id).subscribe({
      next: (blob) => {
        const fileUrl = URL.createObjectURL(blob);
        window.open(fileUrl, '_blank');
      },
      error: () => {
        this.snackbar.showError('Documentul nu a putut fi deschis.', 2500);
      }
    });
  }

  downloadDocument(doc: any): void {
    this.employeeService.downloadDocument(doc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = doc.fileName;
        a.click();

        window.URL.revokeObjectURL(url);

        this.snackbar.showSuccess('Document descărcat.', 1800);
      },
      error: () => {
        this.snackbar.showError('Documentul nu a putut fi descărcat.', 2500);
      }
    });
  }

  async deleteDocument(doc: any): Promise<void> {

    const confirmed = await this.confirmService.confirm(
      'Confirmare ștergere',
      `Sigur vrei să ștergi documentul "${doc.fileName}"?`
    );

    if (!confirmed) return;

    this.employeeService.deleteDocument(doc.id).subscribe({
      next: (res: any) => {
        if (res.isSuccess === false) {
          this.snackbar.showError(res.error?.errorMessage || 'Nu s-a putut șterge documentul.', 2500);
          return;
        }

        this.employee.documents =
          this.employee.documents?.filter((x: any) => x.id !== doc.id) ?? [];

        this.snackbar.showSuccess('Document șters cu succes.', 1800);
      },
      error: () => {
        this.snackbar.showError('Eroare la ștergerea documentului.', 2500);
      }
    });
  }

  loadActivity(): void {
    if (this.activityLoaded) return;

    this.activityLogService
      .getActivity('Employee', this.employee.id)
      .subscribe({
        next: (res) => {
          this.activityLogs = res;
          this.auditPage = 1;
          this.activityLoaded = true;
        },
        error: () => {
          this.snackbar.showError('Nu s-a putut încărca istoricul.', 2500);
        }
      });
  }

  goBack(): void {
  this.router.navigate(['/employees']);
}


get pagedLeaves(): any[] {
  const leaves = this.employee?.leaves ?? [];
  const start = (this.leavePage - 1) * this.leavePageSize;

  return leaves.slice(start, start + this.leavePageSize);
}

get leaveTotalPages(): number {
  const total = this.employee?.leaves?.length ?? 0;
  return Math.max(1, Math.ceil(total / this.leavePageSize));
}

changeLeavePage(page: number): void {
  if (page < 1 || page > this.leaveTotalPages) return;
  this.leavePage = page;
}

get pagedActivityLogs(): any[] {
  const start = (this.auditPage - 1) * this.auditPageSize;

  return this.activityLogs.slice(start, start + this.auditPageSize);
}

get auditTotalPages(): number {
  return Math.max(1, Math.ceil(this.activityLogs.length / this.auditPageSize));
}

changeAuditPage(page: number): void {
  if (page < 1 || page > this.auditTotalPages) return;
  this.auditPage = page;
}
}