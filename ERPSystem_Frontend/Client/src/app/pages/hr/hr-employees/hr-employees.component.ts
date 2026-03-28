import { Component, OnInit } from '@angular/core'
import { CommonModule } from '@angular/common'
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms'
import { AddEmployeesComponent } from '../add-employees/add-employees.component'
import { EmployeeService } from '../../services/employee.service'
import { Employee, HrDashboard } from '../../models/employee.model'
import { Router } from '@angular/router';

@Component({
  selector: 'app-hr-employees',
  standalone: true,
  
  imports: [CommonModule, FormsModule, ReactiveFormsModule,  AddEmployeesComponent],
  templateUrl: './hr-employees.component.html',
  styleUrls: ['./hr-employees.component.css']
})
export class HrEmployeesComponent implements OnInit {

  employees: Employee[] = []

  dashboard!: HrDashboard

  searchText = ''

  createForm!: FormGroup

  showCreate = false

  constructor(
    private service: EmployeeService,
    private fb: FormBuilder, private router: Router
  ) {}

  ngOnInit(): void {

    this.loadEmployees()

    this.service.getDashboard()
  .subscribe(res => {

    if (!res.isSuccess) {
      alert(res.error?.errorMessage);
      return;
    }

    this.dashboard = res.value; // 🔥 FIX
  });

    this.createForm = this.fb.group({
      jobTitle: [''],
      hireDate: [''],
      salary: [''],
      contractType: [''],
      notes: ['']
    })
  }

  loadEmployees() {
  this.service.getEmployees()
    .subscribe(res => {

      if (!res.isSuccess) {
        alert(res.error?.errorMessage);
        return;
      }

      this.employees = res.value; // 🔥 IMPORTANT
    });
}

  openCreate() {
  this.showCreate = true;
}

cancelCreate() {
  this.showCreate = false;
}

onEmployeeCreated() {
  this.showCreate = false;
  this.loadEmployees(); // sau refresh listă
}

  saveEmployee() {

    const body = {
      jobTitle: this.createForm.value.jobTitle,
      hireDate: this.createForm.value.hireDate,
      salary: this.createForm.value.salary,
      contractType: this.createForm.value.contractType,
      notes: this.createForm.value.notes
    }

    this.service.createEmployee(body)
      .subscribe(() => {

        this.cancelCreate()

        this.loadEmployees()

      })
  }

  terminateEmployee(employee: Employee) {

    const body = {
      terminationDate: new Date()
    }

    this.service.terminateEmployee(employee.id, body)
      .subscribe(() => this.loadEmployees())
  }

  filteredEmployees() {

    if(!this.searchText) return this.employees

    return this.employees.filter(x =>
      x.firstName?.toLowerCase().includes(this.searchText.toLowerCase()) ||
      x.lastName?.toLowerCase().includes(this.searchText.toLowerCase()) ||
      x.jobTitle?.toLowerCase().includes(this.searchText.toLowerCase())
    )
  }

 
  viewDetails(employee: any) {
  if (!employee?.id) return;

  this.router.navigate(['/employee', employee.id]);

}

}