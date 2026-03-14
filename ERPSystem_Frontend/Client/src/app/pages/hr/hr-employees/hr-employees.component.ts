import { Component, OnInit } from '@angular/core'
import { CommonModule } from '@angular/common'
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms'

import { EmployeeService } from '../../services/employee.service'
import { Employee, HrDashboard } from '../../models/employee.model'

@Component({
  selector: 'app-hr-employees',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
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
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {

    this.loadEmployees()

    this.service.getDashboard()
      .subscribe(data => this.dashboard = data)

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
      .subscribe(data => this.employees = data)
  }

  openCreate() {
    this.showCreate = true
  }

  cancelCreate() {
    this.showCreate = false
    this.createForm.reset()
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

}