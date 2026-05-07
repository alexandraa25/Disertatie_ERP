import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { EmployeeService } from '../../services/employee.service';
import { CommonModule } from '@angular/common';
import { SimpleUser } from '../../models/employee.model';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

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


  constructor(private fb: FormBuilder, private employeeService: EmployeeService, private snackbar: SnackbarService) {
    this.createForm = this.fb.group({
      mode: ['existing'],

      userId: [''],
      firstName: [''],
      lastName: [''],
      email: ['', [Validators.email]],

      jobTitle: ['', [Validators.required]],
      hireDate: ['', [Validators.required]],
      salary: [null, [Validators.min(0)]],
      contractType: ['', [Validators.required]],
      notes: ['', [Validators.maxLength(500)]],

      phoneNumber: ['', [Validators.pattern(/^[0-9+\s()-]{7,20}$/)]],
      emergencyContactName: [''],
      emergencyContactPhone: ['', [Validators.pattern(/^[0-9+\s()-]{7,20}$/)]],

      street: [''],
      city: [''],
      country: [''],
      postalCode: [''],

      iban: ['', [Validators.pattern(/^RO[a-zA-Z0-9]{22}$/)]],
      bankName: ['']
    });
  }


  ngOnInit() {
    this.loadUsers();
    this.setMode(this.mode);

    this.createForm.get('userId')?.valueChanges.subscribe(id => {
      if (!this.users || this.users.length === 0) return;

      this.selectedUser = this.users.find(u => u.id === id);
    });
  }


  loadUsers() {
    this.employeeService.getUsers()
      .subscribe({
        next: (res: any) => {
          if (!res.isSuccess) {
            this.snackbar.showError(res.error?.errorMessage || 'Eroare la încărcarea utilizatorilor.', 2500);
            return;
          }

          this.users = res.value;
        },
        error: () => {
          this.snackbar.showError('Eroare la încărcarea utilizatorilor.', 2500);
        }
      });
  }

  next() {
    this.submitted = true;

    if (!this.validateStep()) {
      this.snackbar.showError('Completează câmpurile obligatorii.', 2200);
      return;
    }

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
      userId: '',
      firstName: '',
      lastName: '',
      email: ''
    });

    if (mode === 'existing') {
      this.f['userId'].setValidators([Validators.required]);

      this.f['firstName'].clearValidators();
      this.f['lastName'].clearValidators();
      this.f['email'].setValidators([Validators.email]);
    } else {
      this.f['userId'].clearValidators();

      this.f['firstName'].setValidators([Validators.required, Validators.maxLength(100)]);
      this.f['lastName'].setValidators([Validators.required, Validators.maxLength(100)]);
      this.f['email'].setValidators([Validators.required, Validators.email]);
    }

    this.f['userId'].updateValueAndValidity();
    this.f['firstName'].updateValueAndValidity();
    this.f['lastName'].updateValueAndValidity();
    this.f['email'].updateValueAndValidity();
  }

  validateStep(): boolean {
    const stepControls: Record<number, string[]> = {
      0: this.mode === 'existing'
        ? ['userId']
        : ['firstName', 'lastName', 'email'],
      1: ['jobTitle', 'hireDate', 'salary', 'contractType', 'notes'],
      2: ['phoneNumber', 'emergencyContactName', 'emergencyContactPhone'],
      3: ['street', 'city', 'country', 'postalCode'],
      4: ['iban', 'bankName'],
      5: []
    };

    const controls = stepControls[this.currentStep] ?? [];

    controls.forEach(name => {
      this.createForm.get(name)?.markAsTouched();
      this.createForm.get(name)?.updateValueAndValidity();
    });

    return controls.every(name => this.createForm.get(name)?.valid);
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
    this.submitted = true;
console.log('SAVE EMPLOYEE');
console.log(this.selectedFiles);
    for (let i = 0; i < this.steps.length; i++) {
      this.currentStep = i;

      if (!this.validateStep()) {
        return;
      }
    }

    const invalidCustomDoc = this.selectedFiles.some(
      x => x.documentType === 'custom' && !x.customType?.trim()
    );

    if (invalidCustomDoc) {
      this.currentStep = 5;
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
    formData.append('IBAN', formValue.iban || '');
    formData.append('bankName', formValue.bankName || '');

   for (const item of this.selectedFiles) {
  const finalType =
    item.documentType === 'custom'
      ? item.customType || ''
      : item.documentType;

  formData.append('Files', item.file, item.file.name);
  formData.append('DocumentTypes', finalType);
}

console.log(formData.getAll('Files'));
console.log(formData.getAll('DocumentTypes'));

    this.employeeService.createEmployee(formData).subscribe({
      next: (res: any) => {
        if (!res?.isSuccess) {
          this.snackbar.showError(res?.error?.errorMessage || 'Eroare la salvare.', 2500);
          return;
        }

        this.snackbar.showSuccess('Angajatul a fost creat cu succes.', 1800);
        this.finish();
      },
      error: (err) => {
        console.error(err);
        this.snackbar.showError('Eroare la salvare.', 2500);
      }
    });
  }

  finish() {
    this.createForm.reset();
    this.mode = 'existing';
    this.setMode(this.mode);

    this.selectedFiles = [];
    this.currentStep = 0;
    this.selectedUser = null;
    this.submitted = false;

    this.created.emit();
    this.close.emit();
  }

  closeModal() {
    this.close.emit();
  }


  get f() {
    return this.createForm.controls;
  }

  hasError(controlName: string, errorName?: string): boolean {
    const control = this.createForm.get(controlName);

    if (!control) return false;

    if (errorName) {
      return control.hasError(errorName) && (control.touched || this.submitted);
    }

    return control.invalid && (control.touched || this.submitted);
  }

}