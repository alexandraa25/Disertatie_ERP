import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';

import { LeaveService } from '../../services/leave.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityLogService} from '../../services/activity-log.service'
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { ConfirmService } from '../../services/confirm.service';

@Component({
  selector: 'app-employee-details',
  standalone: true,
  imports: [CommonModule,FormsModule, ConfirmCustomModalComponent],
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


  constructor(
    private route: ActivatedRoute,
    private employeeService: EmployeeService,
    private leaveService:LeaveService, 
    private activityLogService:ActivityLogService, 
    private snackbar: SnackbarService, private confirmService: ConfirmService
  ) {}

  ngOnInit(): void {
  this.loadEmployee();
}



approveLeave(id: string) {
  this.leaveService.approve(id).subscribe(() => {
    this.loadEmployee();
  });
}

rejectLeave(leave: any) {
  const reason = prompt('Motiv respingere:');

  if (!reason) return;

  this.leaveService.reject(leave.id, reason).subscribe(() => {
    this.loadEmployee();
  });
}


loadEmployee() {
  const id = this.route.snapshot.paramMap.get('id');

  if (!id) {
    console.error('ID lipsă din URL');
    return;
  }
 
  this.loading = true;

  this.employeeService.getEmployeeById(id).subscribe({
    next: (res) => {
      this.employee = res.value;
      this.loading = false;
    },
    error: () => {
      this.loading = false;
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
        alert(res.error?.errorMessage);
        return;
      }

      this.employee = structuredClone(this.editModel);
      this.isEditMode = false;
    }
  });
  console.log('EDIT MODEL:', this.editModel);
}

onFileSelected(event: Event): void {
  const input = event.target as HTMLInputElement;

  if (!input.files || input.files.length === 0) return;

  this.selectedFiles = Array.from(input.files); // 🔥 IMPORTANT

  input.value = ''; 
}

removeFile(index: number): void {
  this.selectedFiles.splice(index, 1);

  // 🔥 forță refresh UI
  this.selectedFiles = [...this.selectedFiles];
}

uploadDocuments() {
  if (!this.selectedFiles.length) return;

  const formData = new FormData();

  const docType =
    this.documentType === 'custom'
      ? this.customDocumentType
      : this.documentType;

  this.selectedFiles.forEach(file => {
    formData.append('Files', file);
    formData.append('DocumentTypes', docType); // 🔥 paralel
  });

  formData.append('EmployeeId', this.employee.id);

  this.employeeService.uploadEmployeeDocuments(formData).subscribe({
    next: (res) => {
      if (!res.isSuccess) {
        alert(res.error?.errorMessage);
        return;
      }

      this.selectedFiles = [];
      this.loadEmployee();
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
      alert('Documentul nu a putut fi deschis.');
    }
  });
}
downloadDocument(doc: any): void {
  this.employeeService.downloadDocument(doc.id).subscribe({
    next: (blob) => {
      const url = window.URL.createObjectURL(blob);

      const a = document.createElement('a');
      a.href = url;
      a.download = doc.fileName; // 🔥 numele real
      a.click();

      window.URL.revokeObjectURL(url);
    },
    error: () => {
      alert('Documentul nu a putut fi descărcat.');
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
        this.activityLoaded = true;
      },
      error: () => {
        console.error('Nu s-a putut încărca istoricul');
      }
    });
}
}