import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { EmployeeService } from '../../services/employee.service';
import { CommonModule } from '@angular/common';
import { SimpleUser } from '../../models/employee.model';

@Component({
  selector: 'app-add-employees',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule // 🔥 IMPORTANT pentru ngModel
  ],
  templateUrl: './add-employees.component.html',
  styleUrl: './add-employees.component.css'
})
export class AddEmployeesComponent implements OnInit {

  @Output() close = new EventEmitter<void>();
  @Output() created = new EventEmitter<void>();

  createForm: FormGroup;

  // 🔥 lipsă înainte
  activeTab = 'user';
  mode = 'existing';

  steps = ['User', 'Job', 'Contact', 'Address', 'Bank', 'Documents'];
  currentStep = 0;
  submitted = false;

  selectedFiles: File[] = [];

  users: SimpleUser[] = [];
  selectedUser: any = null;

  constructor(
    private fb: FormBuilder,
    private employeeService: EmployeeService
  ) {
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


  onFileSelected(event: any) {
    const files = Array.from(event.target.files) as File[];
    this.selectedFiles.push(...files);
  }

  removeFile(index: number) {
    this.selectedFiles.splice(index, 1);
  }

  saveEmployee() {
  if (this.createForm.valid) {

    const payload = {
      ...this.createForm.value,
      mode: this.mode
    };

    this.employeeService.createEmployee(payload)
      .subscribe({
        next: (res: any) => {

          if (!res.isSuccess) {
            alert(res.error?.errorMessage);
            return;
          }

          const employee = res.value; // 🔥 AICI E FIXUL

          if (this.selectedFiles.length > 0) {
            this.employeeService
              .uploadDocuments(employee.id, this.selectedFiles)
              .subscribe({
                next: (uploadRes: any) => {

                  if (!uploadRes.isSuccess) {
                    alert(uploadRes.error?.errorMessage);
                  }

                  this.finish();
                },
                error: () => {
                  alert('Upload failed');
                  this.finish();
                }
              });
          } else {
            this.finish();
          }
        }
      });
  }
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