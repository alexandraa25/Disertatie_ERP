import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

import { AddEmployeesComponent } from '../add-employees/add-employees.component';
import { EmployeeService } from '../../services/employee.service';
import { Employee, HrDashboard } from '../../models/employee.model';

@Component({
  selector: 'app-hr-employees',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AddEmployeesComponent],
  templateUrl: './hr-employees.component.html',
  styleUrls: ['./hr-employees.component.css']
})
export class HrEmployeesComponent implements OnInit {
  employees: Employee[] = [];
  dashboard!: HrDashboard;

  searchText = '';
  private searchChanged = new Subject<string>();

  statusFilter = '';
  contractFilter = '';

  sortBy = 'hireDate';
  sortDirection: 'asc' | 'desc' = 'desc';

  page = 1;
  pageSize = 5;
  totalCount = 0;
  totalPages = 0;

  loading = false;
  showCreate = false;

  constructor(
    private service: EmployeeService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadEmployees();
    this.loadDashboard();

    this.searchChanged
      .pipe(
        debounceTime(400),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.page = 1;
        this.loadEmployees();
      });
  }

  loadDashboard(): void {
    this.service.getDashboard().subscribe({
      next: (res: any) => {
        if (!res.isSuccess) {
          alert(res.error?.errorMessage);
          return;
        }

        this.dashboard = res.value;
      },
      error: () => {
        alert('Eroare la încărcarea dashboard-ului.');
      }
    });
  }

  loadEmployees(): void {
    this.loading = true;

    const params = {
      search: this.searchText || '',
      employmentStatus: this.statusFilter || '',
      contractType: this.contractFilter || '',
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      page: this.page,
      pageSize: this.pageSize
    };

    this.service.getEmployees(params).subscribe({
      next: (res: any) => {
        this.loading = false;

        if (!res.isSuccess) {
          alert(res.error?.errorMessage);
          return;
        }

        this.employees = res.value.items ?? [];
        this.totalCount = res.value.totalCount ?? 0;
        this.totalPages = res.value.totalPages ?? 0;
      },
      error: () => {
        this.loading = false;
        alert('Eroare la încărcarea angajaților.');
      }
    });
  }

  openCreate(): void {
    this.showCreate = true;
  }

  cancelCreate(): void {
    this.showCreate = false;
  }

  onEmployeeCreated(): void {
    this.showCreate = false;
    this.page = 1;
    this.loadEmployees();
    this.loadDashboard();
  }

  onSearchChange(): void {
    this.searchChanged.next(this.searchText);
  }

  onFiltersChanged(): void {
    this.page = 1;
    this.loadEmployees();
  }

  setQuickFilter(type: string): void {
    this.statusFilter = type;
    this.page = 1;
    this.loadEmployees();
  }

  sort(column: string): void {
    if (this.sortBy === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDirection = 'asc';
    }

    this.page = 1;
    this.loadEmployees();
  }

  changePage(newPage: number): void {
    if (newPage < 1 || newPage > this.totalPages) {
      return;
    }

    this.page = newPage;
    this.loadEmployees();
  }

  terminateEmployee(employee: Employee): void {
    const body = {
      terminationDate: new Date()
    };

    this.service.terminateEmployee(employee.id, body).subscribe({
      next: () => {
        this.loadEmployees();
        this.loadDashboard();
      },
      error: () => {
        alert('Eroare la încetarea angajatului.');
      }
    });
  }

  // reactivateEmployee(employee: Employee): void {
  //   if (!this.service.reactivateEmployee) {
  //     alert('Metoda de reactivare nu este încă implementată în service.');
  //     return;
  //   }

  //   this.service.reactivateEmployee(employee.id).subscribe({
  //     next: () => {
  //       this.loadEmployees();
  //       this.loadDashboard();
  //     },
  //     error: () => {
  //       alert('Eroare la reactivarea angajatului.');
  //     }
  //   });
  // }

  viewDetails(employee: Employee): void {
    if (!employee?.id) {
      return;
    }

    this.router.navigate(['/employee', employee.id]);
  }

  exportCSV(): void {
    let csv = 'Nume,Functie,Status,Data angajarii,Salariu brut,Tip contract\n';

    this.employees.forEach((e) => {
      const fullName = `${e.firstName ?? ''} ${e.lastName ?? ''}`.trim();
      const hireDate = e.hireDate ? new Date(e.hireDate).toLocaleDateString('ro-RO') : '';

      csv += `"${fullName}","${e.jobTitle ?? ''}","${e.employmentStatus ?? ''}","${hireDate}","${e.salary ?? ''}","${e.contractType ?? ''}"\n`;
    });

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = 'angajati.csv';
    a.click();

    window.URL.revokeObjectURL(url);
  }

  getSortIndicator(column: string): string {
    if (this.sortBy !== column) {
      return '';
    }

    return this.sortDirection === 'asc' ? '↑' : '↓';
  }

  getSortIcon(column: string): string {
  if (this.sortBy !== column) {
    return '↕';
  }

  return this.sortDirection === 'asc' ? '↑' : '↓';
}
}