import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { AddEmployeesComponent } from '../add-employees/add-employees.component';
import { EmployeeService } from '../../services/employee.service';
import { Employee, HrDashboard } from '../../models/employee.model';
import { ConfirmService } from '../../services/confirm.service';
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';

@Component({
  selector: 'app-hr-employees',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AddEmployeesComponent, ConfirmCustomModalComponent],
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
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  loading = false;
  showCreate = false;

  terminateModalOpen = false;
  employeeToTerminate: Employee | null = null;
  terminationFile: File | null = null;

  terminationDocumentType: string = 'DecizieIncetare';
  terminationCustomType: string = '';

  constructor(
    private service: EmployeeService,
    private router: Router,
    private snackBar: SnackbarService,
    private confirmService: ConfirmService
  ) { }

  ngOnInit(): void {
    this.loadEmployees();

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
          this.snackBar.showError(
            res.error?.errorMessage || 'Eroare la încărcarea angajaților.',
            2500
          );
          return;
        }

        this.employees = res.value.items ?? [];
        this.totalCount = res.value.totalCount ?? 0;
        this.totalPages = res.value.totalPages ?? 0;
      },
      error: () => {
        this.loading = false;
        this.snackBar.showError('Eroare la încărcarea angajaților.', 2500);
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

  viewDetails(employee: Employee): void {
    if (!employee?.id) {
      return;
    }

    this.router.navigate(['/employee', employee.id]);
  }

  exportExcel(): void {
    this.service
      .exportEmployeesExcel(this.searchText, this.statusFilter, this.contractFilter)
      .subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);

          const a = document.createElement('a');
          a.href = url;
          a.download = 'angajati.xlsx';
          a.click();

          window.URL.revokeObjectURL(url);

          this.snackBar.showSuccess('Exportul Excel a fost generat.', 1800);
        },
        error: () => {
          this.snackBar.showError('Exportul Excel nu a putut fi generat.', 2500);
        }
      });
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

  terminateEmployee(employee: Employee): void {
    this.employeeToTerminate = employee;
    this.terminationFile = null;
    this.terminateModalOpen = true;
  }

  onTerminationFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) return;

    this.terminationFile = input.files[0];
  }

  confirmTerminate(): void {
    if (!this.employeeToTerminate) return;

    const formData = new FormData();

    formData.append('terminationDate', new Date().toISOString());

    const finalType =
      this.terminationDocumentType === 'custom'
        ? this.terminationCustomType || 'Unknown'
        : this.terminationDocumentType;

    if (this.terminationFile) {
      formData.append('File', this.terminationFile);
      formData.append('DocumentType', finalType);
    }

    this.service.terminateEmployee(this.employeeToTerminate.id, formData)
      .subscribe({
        next: () => {
          this.snackBar.showSuccess('Contractul angajatului a fost încetat.', 1800);
          this.loadEmployees();
          this.closeTerminateModal();
        },
        error: () => {
          this.snackBar.showError('Eroare la încetarea angajatului.', 2500);
        }
      });
  }

  closeTerminateModal(): void {
    this.terminateModalOpen = false;
    this.employeeToTerminate = null;
    this.terminationFile = null;
  }

  onTerminationDocTypeChange() {
    if (this.terminationDocumentType !== 'custom') {
      this.terminationCustomType = '';
    }
  }

}