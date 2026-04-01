import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';

import { LeaveService } from '../../services/leave.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-employee-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './employee-details.component.html',
  styleUrls: ['./employee-details.component.css']
})
export class EmployeeDetailsComponent implements OnInit {

  employee: any;
  loading = true;
  activeTab: 'info' | 'documents' | 'leaves' | 'audit' = 'info';

  constructor(
    private route: ActivatedRoute,
    private employeeService: EmployeeService,
    private leaveService:LeaveService
  ) {}

  ngOnInit(): void {
  this.loadEmployee();
}

  openDocument(path: string) {
  window.open(path, '_blank');
}

approveLeave(id: string) {
  this.leaveService.approve(id).subscribe(() => {
    this.loadEmployee();
  });
}

rejectLeave(leave: any) {
  const reason = prompt('Motiv respingere:');

  if (!reason) return;

  this.leaveService.reject(leave.id, reason).subscribe(() => {
    this.loadEmployee();
  });
}


loadEmployee() {
  const id = this.route.snapshot.paramMap.get('id');

  if (!id) {
    console.error('ID lipsă din URL');
    return;
  }

  this.loading = true;

  this.employeeService.getEmployeeById(id).subscribe({
    next: (res) => {
      this.employee = res.value;
      this.loading = false;
    },
    error: () => {
      this.loading = false;
    }
  });
}
}