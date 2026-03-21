import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { StudentsService } from '../../services/students.service';
import { MatDialog } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentCourseDetailsDto } from '../../models/student.model';

@Component({
  selector: 'app-aditional-act',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './aditional-act.component.html',
  styleUrls: ['./aditional-act.component.css']
})
export class AditionalActComponent implements OnInit {

  selectedTypes: string[] = [];
  selectedCourseId: number | null = null;
  newEndDate: string | null = null;
  newPrice: number | null = null;
  description: string = '';
  contract: any;

  allCourses: StudentCourseDetailsDto[] = [];
  availableCourses: StudentCourseDetailsDto[] = [];
  inactiveCourses: StudentCourseDetailsDto[] = [];
  studentCourses: any[] = [];

  contractId!: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contractsService: ContractsService,
    private studentsService: StudentsService,
    private dialog: MatDialog
  ) { }

  ngOnInit() {
    this.contractId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadCourses();
    this.loadContract();
  }

  onTypeChange() {
    this.selectedCourseId = null;
    this.newEndDate = null;
    this.newPrice = null;
  }

  toggleType(type: string) {
    const index = this.selectedTypes.indexOf(type);

    if (index > -1) {
      this.selectedTypes.splice(index, 1);

      // 🔥 reset valori când debifezi
      if (type === 'AddCourse' || type === 'RemoveCourse') {
        this.selectedCourseId = null;
      }

      if (type === 'ExtendPeriod') {
        this.newEndDate = null;
      }

      if (type === 'ChangePrice') {
        this.newPrice = null;
      }

    } else {
      this.selectedTypes.push(type);
    }
  }

  loadContract() {
    this.contractsService.getById(this.contractId).subscribe(res => {
      this.contract = res.value;
    });
  }
get priceDiff(): number | null {
  if (!this.newPrice || !this.contract?.totalAmount) return null;
  return this.newPrice - this.contract.totalAmount;
}
  loadCourses() {
    this.studentsService.getStudentCoursesByContract(this.contractId) // ⚠️ vezi mai jos
      .subscribe(res => {

        console.log(res); // 🔍 vezi structura

        this.allCourses = res.value.items; // 🔥 FIX IMPORTANT

        this.availableCourses = this.allCourses.filter(c =>
          c.isActive && !c.contractId
        );

        this.inactiveCourses = this.allCourses.filter(c =>
          c.isActive && c.contractId === this.contractId
        );
      });
  }

  save() {

    if (!this.selectedTypes || this.selectedTypes.length === 0) {
      alert('Selectează cel puțin un tip');
      return;
    }

    // 🔥 VALIDĂRI PE FIECARE TIP SELECTAT
    if (
      (this.selectedTypes.includes('AddCourse') ||
        this.selectedTypes.includes('RemoveCourse')) &&
      !this.selectedCourseId
    ) {
      alert('Selectează curs');
      return;
    }

    if (this.selectedTypes.includes('ExtendPeriod') && !this.newEndDate) {
      alert('Selectează dată');
      return;
    }

    if (
      this.selectedTypes.includes('ChangePrice') &&
      (this.newPrice === null || this.newPrice <= 0)
    ) {
      alert('Introdu preț valid');
      return;
    }

    // 🔥 DTO
    const dto: any = {
      types: this.selectedTypes,
      description: this.description || ''
    };

    if (
      this.selectedTypes.includes('AddCourse') ||
      this.selectedTypes.includes('RemoveCourse')
    ) {
      dto.courseSessionIds = [this.selectedCourseId];
    }


    if (this.selectedTypes.includes('ExtendPeriod')) {
      dto.newEndDate = this.newEndDate;
    }


    if (this.selectedTypes.includes('ChangePrice')) {
      dto.newPrice = this.newPrice;
    }


    this.contractsService.createAct(this.contractId, dto).subscribe({
      next: () => {
        alert('Act creat!');
        this.goBack();
      },
      error: err => {
        console.error(err);
        alert('Eroare la creare');
      }
    });
  }

  goBack() {
    this.router.navigate(['/students']);
  }


}

