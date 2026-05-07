import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { StudentsService } from '../../services/students.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentCourseDetailsDto } from '../../models/student.model';
import { AdditionalActService } from '../../services/additional-act.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

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

  actId?: number;
  isEdit = false;

  contractId!: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contractsService: ContractsService,
    private additionalActService: AdditionalActService,
    private studentsService: StudentsService,
    private snackbar: SnackbarService
  ) { }

  ngOnInit() {

  const contractId = this.route.snapshot.paramMap.get('contractId');
  const actId = this.route.snapshot.paramMap.get('actId');

  if (actId) {
    this.actId = Number(actId);
    this.isEdit = true;
    this.loadAct(this.actId); 
  } 
  else if (contractId) {
    this.contractId = Number(contractId);
    this.loadContract();
    this.loadCourses();
  }
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
  this.contractsService.getById(this.contractId).subscribe({
    next: (res: any) => {
      if (res?.isSuccess === false || !res?.value) {
        this.snackbar.showError('Contractul nu a putut fi încărcat.', 2500);
        return;
      }

      this.contract = res.value;
    },
    error: () => {
      this.snackbar.showError('Eroare la încărcarea contractului.', 2500);
    }
  });
}

  loadAct(id: number) {
  this.additionalActService.getById(id).subscribe((res: any) => {

    const act = res.value;
    if (!act) return;

    this.contractId = act.contractId;
    this.contract = act.contract;

    this.loadContract();
    this.loadCourses();

    this.selectedTypes = act.items.map((i: any) =>
        this.mapEnumToString(i.type)  
       );

    const courseItem = act.items.find((i: any) => i.courseSessionId);
    if (courseItem) {
      this.selectedCourseId = courseItem.courseSessionId;
    }

    const extend = act.items.find((i: any) => i.type === 'ExtendPeriod');
    if (extend) {
      this.newEndDate = extend.newValue;
    }

    const price = act.items.find((i: any) => i.type === 'ChangePrice');
    if (price) {
      this.newPrice = Number(price.newValue);
    }

    this.description = act.description;
  });
}

  get priceDiff(): number | null {
    if (!this.newPrice || !this.contract?.totalAmount) return null;
    return this.newPrice - this.contract.totalAmount;
  }

  loadCourses() {
    this.studentsService.getStudentCoursesByContract(this.contractId) 
      .subscribe(res => {

        console.log(res); 

        this.allCourses = res.value.items; 

        this.availableCourses = this.allCourses.filter(c =>
          c.isActive && !c.contractId
        );

        this.inactiveCourses = this.allCourses.filter(c =>
          !c.isActive && c.contractId === this.contractId
        );
      });
  }

  save() {

    if (!this.selectedTypes || this.selectedTypes.length === 0) {
  this.snackbar.showError('Selectează cel puțin un tip.', 2200);
  return;
}

if (
  (this.selectedTypes.includes('AddCourse') ||
    this.selectedTypes.includes('RemoveCourse')) &&
  !this.selectedCourseId
) {
  this.snackbar.showError('Selectează cursul.', 2200);
  return;
}

if (this.selectedTypes.includes('ExtendPeriod') && !this.newEndDate) {
  this.snackbar.showError('Selectează data nouă de final.', 2200);
  return;
}

if (
  this.selectedTypes.includes('ChangePrice') &&
  (this.newPrice === null || this.newPrice <= 0)
) {
  this.snackbar.showError('Introdu un preț valid.', 2200);
  return;
}

    const map: any = {
      AddCourse: 0,
      RemoveCourse: 1,
      ExtendPeriod: 2,
      ChangePrice: 3
    };

    const dto: any = {
      types: this.selectedTypes.map(t => map[t]),
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

    if (this.isEdit && this.actId) {

     this.additionalActService.update(this.actId, dto).subscribe({
  next: (res: any) => {
    if (res?.isSuccess === false) {
      this.snackbar.showError(
        res.error?.errorMessage || 'Actul adițional nu a putut fi actualizat.',
        2500
      );
      return;
    }

    this.snackbar.showSuccess('Act adițional actualizat cu succes.', 1800);
    this.router.navigate(['/additional-act', this.actId]);
  },
  error: (err) => {
    console.error(err);
    this.snackbar.showError(
      err.error?.message || 'Eroare la actualizarea actului adițional.',
      2500
    );
  }
});

    } else {

    this.additionalActService.create(this.contractId, dto).subscribe({
  next: (res: any) => {
    if (res?.isSuccess === false) {
      this.snackbar.showError(
        res.error?.errorMessage || 'Actul adițional nu a putut fi creat.',
        2500
      );
      return;
    }

    const id = res?.value?.id;

    this.snackbar.showSuccess('Act adițional creat cu succes.', 1800);

    if (id) {
      this.router.navigate(['/additional-act', id]);
    }
  },
  error: (err) => {
    console.error(err);
    this.snackbar.showError(
      err.error?.message || 'Eroare la crearea actului adițional.',
      2500
    );
  }
});
    }
  }

  goBack() {
    this.router.navigate(['/students']);
  }

mapEnumToString(type: number): string {
  const map: any = {
    0: 'AddCourse',
    1: 'RemoveCourse',
    2: 'ExtendPeriod',
    3: 'ChangePrice'
  };
  return map[type];
}

}

