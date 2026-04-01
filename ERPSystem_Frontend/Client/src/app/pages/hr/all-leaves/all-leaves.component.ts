import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../../services/leave.service';

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
sort = {
  column: 'startDate',
  direction: 'desc'
};

  // 🔥 filters
  filters = {
    status: '',
    leaveType: '',
    page: 1,
    pageSize: 10
  };

  loading = false;

  constructor(private leaveService: LeaveService) {}

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
    error: () => this.loading = false
  });
}

// 🔍 search
onSearch() {
  this.filters.page = 1;
  this.loadLeaves();
}

// 🔽 sort
sortBy(column: string) {
  if (this.sort.column === column) {
    this.sort.direction = this.sort.direction === 'asc' ? 'desc' : 'asc';
  } else {
    this.sort.column = column;
    this.sort.direction = 'asc';
  }

  this.loadLeaves();
}

// ✔ select
toggleSelection(id: string) {
  if (this.selected.has(id)) this.selected.delete(id);
  else this.selected.add(id);
}

// ✔ bulk approve
// bulkApprove() {
//   const ids = Array.from(this.selected);

//   this.leaveService.bulkApprove(ids).subscribe(() => {
//     this.selected.clear();
//     this.loadLeaves();
//   });
// }

  applyFilters() {
    this.filters.page = 1;
    this.loadLeaves();
  }

  changePage(p: number) {
    this.filters.page = p;
    this.loadLeaves();
  }

  exportExcel() {
    this.leaveService.exportExcel().subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'leaves.xlsx';
      a.click();
    });
  }
}