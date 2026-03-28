import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';
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
    private employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
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

  openDocument(path: string) {
  window.open(path, '_blank');
}
}