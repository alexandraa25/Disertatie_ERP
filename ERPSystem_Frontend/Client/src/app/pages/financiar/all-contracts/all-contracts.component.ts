import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ContractsService } from '../../services/contracts.service';

@Component({
  selector: 'app-all-contracts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './all-contracts.component.html',
  styleUrl: './all-contracts.component.css'
})
export class AllContractsComponent implements OnInit {
  contracts: any[] = [];
  loading = false;

  exportFrom: string | null = null;
exportTo: string | null = null;

currentPage = 1;
pageSize = 5;

  constructor(
    private contractsService: ContractsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadContracts();
  }

  loadContracts(): void {
    this.loading = true;

    this.contractsService.getContractsOverview().subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.contracts = Array.isArray(data) ? data : [];
        this.currentPage = 1;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  openContract(id: number): void {
    this.router.navigate(['/contracts', id]);
  }

  openAct(id: number): void {
    this.router.navigate(['/additional-act', id]);
  }

 exportContractsExcel(): void {
  this.contractsService.exportContractsExcel(this.exportFrom, this.exportTo)
    .subscribe((blob: Blob) => {
      this.downloadFile(blob, 'contracte_contabilitate.xlsx');
    });
}


private downloadFile(blob: Blob, fileName: string): void {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');

  a.href = url;
  a.download = fileName;
  a.click();

  window.URL.revokeObjectURL(url);
}

get totalPages(): number {
  return Math.ceil(this.contracts.length / this.pageSize) || 1;
}

get pagedContracts(): any[] {
  const start = (this.currentPage - 1) * this.pageSize;
  return this.contracts.slice(start, start + this.pageSize);
}

nextPage(): void {
  if (this.currentPage < this.totalPages) {
    this.currentPage++;
  }
}

previousPage(): void {
  if (this.currentPage > 1) {
    this.currentPage--;
  }
}
}