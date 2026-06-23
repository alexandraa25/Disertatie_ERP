import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../../services/leave.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-all-leaves',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './all-leaves.component.html',
  styleUrl: './all-leaves.component.css'
})
export class AllLeavesComponent implements OnInit {

  leaves: any[] = [];
  total = 0;
  searchTerm = '';
  selected: Set<string> = new Set();

  sort = { column: 'startDate', direction: 'desc'};

  filters = {status: '', leaveType: '', page: 1, pageSize: 10};

  loading = false;
  private searchTimeout: any;

  showRejectModal = false;
  selectedLeave: any = null;
  rejectReason = '';

  startDate = '';
  endDate = '';
  conflicts: any[] = [];
  showConflictsModal = false;
  loadingConflicts = false;
  checkedConflicts = false;

  canApprove = false;

  constructor(private leaveService: LeaveService, private snackbar: SnackbarService, private auth: AuthService) {
    this.canApprove = this.auth.hasRole(['Admin', 'HR', 'Manager']);
  }

  ngOnInit() {
    this.loadLeaves();
  }

 loadLeaves() {
  this.loading = true;

  const params = {
    ...this.filters,
    search: this.searchTerm,
    sortBy: this.sort.column,
    sortOrder: this.sort.direction
  };

  this.leaveService.getAllLeaves(params).subscribe({
    next: (res: any) => {
      this.leaves = res.value.data;
      this.total = res.value.total;
      this.loading = false;
    },
    error: () => {
      this.loading = false;
      this.snackbar.showError('Eroare la încărcarea concediilor.', 2500);
    }
  });
}

  onSearchChange() {
    this.filters.page = 1;

    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.loadLeaves();
    }, 400);
  }

  onFilterChange() {
    this.filters.page = 1;
    this.loadLeaves();
  }

  sortBy(column: string) {
    if (this.sort.column === column) {
      this.sort.direction = this.sort.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.sort.column = column;
      this.sort.direction = 'asc';
    }

    this.loadLeaves();
  }

  toggleSelection(id: string) {
    if (this.selected.has(id)) {
      this.selected.delete(id);
    } else {
      this.selected.add(id);
    }
  }

  changePage(p: number) {
    this.filters.page = p;
    this.loadLeaves();
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Pending':
        return 'În așteptare';
      case 'Approved':
        return 'Aprobat';
      case 'Rejected':
        return 'Respins';
        case 'Cancelled':
  return 'Anulat';
      default:
        return status;
    }
  }

  getLeaveTypeLabel(type: string): string {
    switch (type) {
      case 'Vacation':
        return 'Concediu de odihnă';
      case 'Sick':
        return 'Concediu medical';
      case 'Unpaid':
        return 'Concediu fără plată';
      default:
        return type;
    }
  }

  openConflictsModal() {
    this.showConflictsModal = true;
    this.checkedConflicts = false;
    this.conflicts = [];
  }

  closeConflictsModal() {
    this.showConflictsModal = false;
    this.startDate = '';
    this.endDate = '';
    this.conflicts = [];
    this.checkedConflicts = false;
  }

  checkConflicts() {
    if (!this.startDate || !this.endDate) {
      this.snackbar.showError('Selectează perioada.', 2200);
      return;
    }

    this.loadingConflicts = true;
    this.checkedConflicts = false;
    this.conflicts = [];

    this.leaveService.getConflicts(this.startDate, this.endDate).subscribe({
      next: (res) => {
        this.conflicts = res.value || [];
        this.loadingConflicts = false;
        this.checkedConflicts = true;

        if (this.conflicts.length) {
          this.snackbar.showError('Au fost găsite conflicte în perioada selectată.', 2500);
        } else {
          this.snackbar.showSuccess('Nu există conflicte în perioada selectată.', 1800);
        }
      },
      error: () => {
        this.loadingConflicts = false;
        this.checkedConflicts = true;
        this.conflicts = [];

        this.snackbar.showError('Eroare la verificarea conflictelor.', 2500);
      }
    });
  }

  get totalPages(): number {
    return Math.ceil(this.total / this.filters.pageSize);
  }

  approveLeave(id: string) {
    this.leaveService.approve(id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Concediul a fost aprobat.', 1800);
        this.loadLeaves();
      },
      error: () => {
        this.snackbar.showError('Concediul nu a putut fi aprobat.', 2500);
      }
    });
  }

  openRejectModal(leave: any) {
    this.selectedLeave = leave;
    this.rejectReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal() {
    this.showRejectModal = false;
    this.selectedLeave = null;
    this.rejectReason = '';
  }

  confirmReject() {
    if (!this.selectedLeave) return;

    if (!this.rejectReason || !this.rejectReason.trim()) {
      this.snackbar.showError('Completează motivul respingerii.', 2200);
      return;
    }

    this.leaveService.reject(this.selectedLeave.id, this.rejectReason.trim()).subscribe({
      next: () => {
        this.snackbar.showSuccess('Concediul a fost respins.', 1800);
        this.closeRejectModal();
        this.loadLeaves();
      },
      error: () => {
        this.snackbar.showError('A apărut o eroare la respingerea concediului.', 2500);
      }
    });
  }

}