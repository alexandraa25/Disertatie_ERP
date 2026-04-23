import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { EmployeeService } from '../../services/employee.service';
import { CommonModule } from '@angular/common';
import { SimpleUser } from '../../models/employee.model';

@Component({
  selector: 'app-add-employees',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './add-employees.component.html',
  styleUrl: './add-employees.component.css'
})
export class AddEmployeesComponent implements OnInit {

  @Output() close = new EventEmitter<void>();
  @Output() created = new EventEmitter<void>();

  createForm: FormGroup;

  activeTab = 'user';
  mode = 'existing';

  steps = ['User', 'Job', 'Contact', 'Address', 'Bank', 'Documents'];
  currentStep = 0;
  submitted = false;

  selectedFiles: { file: File; documentType: string; customType?: string; }[] = [];
  fileErrors: string[] = [];
  isDragging = false;

  documentType: string = 'Contract';
  customDocumentType: string = '';

  readonly maxFileSize = 1 * 1024 * 1024;
  readonly allowedExtensions: string[] = ['doc', 'docx', 'pdf', 'txt', 'jpg', 'jpeg', 'png', 'ppt', 'pptx'];

  users: SimpleUser[] = [];
  selectedUser: any = null;


  constructor(private fb: FormBuilder, private employeeService: EmployeeService) {
    this.createForm = this.fb.group({
      mode: ['existing'],

      userId: [''],
      firstName: [''],
      lastName: [''],
      email: [''],

      jobTitle: [''],
      hireDate: [''],
      salary: [''],
      contractType: [''],
      notes: [''],

      phoneNumber: [''],
      emergencyContactName: [''],
      emergencyContactPhone: [''],

      street: [''],
      city: [''],
      country: [''],
      postalCode: [''],

      iban: [''],
      bankName: ['']
    });
  }


  ngOnInit() {
    this.loadUsers();

    this.createForm.get('userId')?.valueChanges.subscribe(id => {
      if (!this.users || this.users.length === 0) return;

      this.selectedUser = this.users.find(u => u.id === id);
    });
  }


  loadUsers() {
    this.employeeService.getUsers()
      .subscribe(res => {

        if (!res.isSuccess) {
          alert(res.error?.errorMessage);
          return;
        }

        this.users = res.value;
      });
  }

  next() {
    this.submitted = true;

    if (!this.validateStep()) return;

    this.currentStep++;
    this.submitted = false;
  }

  prev() {
    this.currentStep--;
  }

  setMode(mode: string) {
    this.mode = mode;

    this.selectedUser = null;

    this.createForm.patchValue({
      userId: null,
      firstName: '',
      lastName: '',
      email: ''
    });
  }

  validateStep(): boolean {

    // STEP 1
    if (this.currentStep === 0) {
      if (this.mode === 'existing') {
        return !!this.createForm.value.userId;
      }

      return (
        this.createForm.value.firstName &&
        this.createForm.value.lastName &&
        this.createForm.value.email
      );
    }

    // STEP 2
    if (this.currentStep === 1) {
      return (
        this.createForm.value.jobTitle &&
        this.createForm.value.hireDate &&
        this.createForm.value.contractType
      );
    }

    return true;
  }


  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (!files || files.length === 0) {
      return;
    }

    this.processFiles(Array.from(files));
  }

  onFileSelected(event: Event): void {
  const input = event.target as HTMLInputElement;

  if (!input.files || input.files.length === 0) return;

  this.processFiles(Array.from(input.files));

  input.value = '';
}

  onDocTypeChange() {
    if (this.documentType !== 'custom') {
      this.customDocumentType = '';
    }
  }

  getFinalDocType(item: any): string {
    return item.documentType === 'custom'
      ? item.customType
      : item.documentType;
  }

  processFiles(files: File[]): void {
    this.fileErrors = [];

    files.forEach((file: File) => {
      const extension = this.getFileExtension(file.name);

      if (!this.allowedExtensions.includes(extension)) {
        this.fileErrors.push(`Fișierul "${file.name}" nu are un format permis.`);
        return;
      }

      if (file.size > this.maxFileSize) {
        this.fileErrors.push(`Fișierul "${file.name}" depășește limita de 1 MB.`);
        return;
      }

      const exists = this.selectedFiles.some(
        x => x.file.name === file.name && x.file.size === file.size
      );

      if (exists) {
        this.fileErrors.push(`Fișier duplicat: "${file.name}"`);
        return;
      }

      this.selectedFiles.push({
  file,
  documentType: this.documentType,
  customType: this.customDocumentType
});
    });
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  getFileExtension(fileName: string): string {
    return fileName.split('.').pop()?.toLowerCase() || '';
  }

  formatFileSize(size: number): string {
    if (size < 1024) {
      return `${size} B`;
    }

    if (size < 1024 * 1024) {
      return `${(size / 1024).toFixed(1)} KB`;
    }

    return `${(size / (1024 * 1024)).toFixed(2)} MB`;
  }



  saveEmployee() {
    if (!this.createForm.valid) {
      return;
    }

    const formValue = this.createForm.value;
    const formData = new FormData();

    formData.append('mode', this.mode || '');
    formData.append('userId', formValue.userId || '');
    formData.append('firstName', formValue.firstName || '');
    formData.append('lastName', formValue.lastName || '');
    formData.append('email', formValue.email || '');
    formData.append('hireDate', new Date(formValue.hireDate).toISOString());
    formData.append('jobTitle', formValue.jobTitle || '');
    formData.append('salary', String(formValue.salary ?? '0'));
    formData.append('contractType', formValue.contractType || '');
    formData.append('notes', formValue.notes || '');
    formData.append('phoneNumber', formValue.phoneNumber || '');
    formData.append('emergencyContactName', formValue.emergencyContactName || '');
    formData.append('emergencyContactPhone', formValue.emergencyContactPhone || '');
    formData.append('street', formValue.street || '');
    formData.append('city', formValue.city || '');
    formData.append('country', formValue.country || '');
    formData.append('postalCode', formValue.postalCode || '');
    formData.append('iban', formValue.iban || '');
    formData.append('bankName', formValue.bankName || '');

    // 🔥 DOCUMENTE + TYPE PER FILE
    for (const item of this.selectedFiles) {

      const finalType =
        item.documentType === 'custom'
          ? item.customType || ''
          : item.documentType;

      formData.append('Files', item.file, item.file.name);
      formData.append('DocumentTypes', finalType);
    }

    this.employeeService.createEmployee(formData).subscribe({
      next: (res: any) => {
        if (!res?.isSuccess) {
          alert(res?.error?.errorMessage || 'Eroare la salvare');
          return;
        }

        this.finish();
      },
      error: (err) => {
        console.error(err);
        alert('Eroare la salvare');
      }
    });
  }

  finish() {
    this.createForm.reset();
    this.selectedFiles = [];
    this.currentStep = 0;
    this.selectedUser = null;

    this.created.emit();
    this.close.emit();
  }

  closeModal() {
    this.close.emit();
  }


}