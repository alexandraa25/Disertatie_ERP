import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-aditional-act',
  standalone: true,
  imports: [],
  templateUrl: './aditional-act.component.html',
  styleUrl: './aditional-act.component.css'
})
export class AditionalActComponent {


//   contractId!: number;

// selectedType = '';
// selectedCourseId: number | null = null;
// newEndDate = '';
// priceDifference = 0;

// constructor(
//     private route: ActivatedRoute,
//     private router: Router,
//     private contractsService: ContractsService,
//     private dialog: MatDialog

//   ) { }
// ngOnInit() {
//   this.contractId = Number(this.route.snapshot.paramMap.get('id'));
// }

// onTypeChange() {
//   if (this.selectedType === 'AddCourse') {
//   //  this.loadAvailableCourses();
//   }

//   if (this.selectedType === 'RemoveCourse') {
//  //   this.loadStudentCourses();
//   }
// }

// save() {

//   const dto: any = {
//     type: this.selectedType,
//    // description: this.getDescription()
//   };

//   if (this.selectedType === 'AddCourse') {
//     dto.courseSessionIds = [this.selectedCourseId];
//   }

//   if (this.selectedType === 'RemoveCourse') {
//     dto.courseSessionIds = [this.selectedCourseId];
//   }

//   if (this.selectedType === 'ExtendPeriod') {
//     dto.newEndDate = this.newEndDate;
//   }

//   if (this.selectedType === 'ChangePrice') {
//     dto.priceDifference = this.priceDifference;
//   }

//  // this.contractService.createAct(this.contractId, dto)
//   //  .subscribe(() => {
//    //   this.goBack();
//    // });
// }

// goBack() {
//   this.router.navigate(['/students']);
// }
 }
