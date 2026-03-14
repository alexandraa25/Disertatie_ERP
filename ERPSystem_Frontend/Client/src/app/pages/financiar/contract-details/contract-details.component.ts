import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-contract-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contract-details.component.html',
styleUrls: ['./contract-details.component.css']})
export class ContractDetailsComponent implements OnInit {

  contract: any;
  loading = true;
  actionLoading = false;
  isEditingBody = false;
editedBody = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contractsService: ContractsService
   
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadContract(id);
  }

  loadContract(id: number) {
  this.loading = true;

  this.contractsService.getById(id)
    .subscribe({
      next: (res) => {

        console.log('RESPONSE:', res);

        if (!res.isSuccess || !res.value) {
          console.error('Contract not found');
          this.loading = false;
          return;
        }

        this.contract = res.value; // 🔥 FOARTE IMPORTANT
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
}

  finalize() {
    this.runAction(() =>
      this.contractsService.finalize(this.contract.id)
    );
  }

  sign() {
    this.runAction(() =>
      this.contractsService.sign(this.contract.id)
    );
  }

  activate() {
    this.runAction(() =>
      this.contractsService.activate(this.contract.id)
    );
  }

  cancel() {
    this.runAction(() =>
      this.contractsService.cancel(this.contract.id)
    );
  }

  private runAction(action: () => any) {
    if (this.actionLoading) return;

    this.actionLoading = true;

    action().subscribe({
      next: () => {
        this.loadContract(this.contract.id);
        this.actionLoading = false;
      },
      error: () => {
        this.actionLoading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Draft': return 'badge draft';
      case 'Finalized': return 'badge finalized';
      case 'Signed': return 'badge signed';
      case 'Active': return 'badge active';
      case 'Cancelled': return 'badge cancelled';
      default: return 'badge';
    }
  }

  startEditBody() {
  this.isEditingBody = true;
  this.editedBody = this.contract.contractBody;
}

cancelEditBody() {
  this.isEditingBody = false;
}

saveBody() {

  this.actionLoading = true;

  this.contractsService.updateBody(this.contract.id, {
    contractBody: this.editedBody
  }).subscribe({
    next: () => {
      this.contract.contractBody = this.editedBody;
      this.isEditingBody = false;
      this.actionLoading = false;
    },
    error: () => this.actionLoading = false
  });
}
}