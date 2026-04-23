import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';

import { LeaveService } from '../../services/leave.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-employee-details',
  standalone: true,
  imports: [CommonModule,FormsModule],
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


  constructor(
    private route: ActivatedRoute,
    private employeeService: EmployeeService,
    private leaveService:LeaveService
  ) {}

  ngOnInit(): void {
  this.loadEmployee();
}

  openDocument(path: string) {
  window.open(path, '_blank');
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
}