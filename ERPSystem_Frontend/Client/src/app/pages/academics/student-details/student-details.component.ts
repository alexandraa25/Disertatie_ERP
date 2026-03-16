import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StudentsService } from '../../services/students.service';
import { ContractsService } from '../../services/contracts.service';
import { StudentDetailsDto, StudentCourseDetailsDto } from '../../models/student.model';
import { StudentFormComponent } from '../student-form/student-form.component';
import { MatDialog } from '@angular/material/dialog';
import { EnrollStudentsComponent } from '../enroll-students/enroll-students.component';
import { AdminSignatureModalComponent } from '../../financiar/admin-signature-modal/admin-signature-modal.component';

@Component({
  selector: 'app-student-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-details.component.html',
  styleUrls: ['./student-details.component.css']
})
export class StudentDetailsComponent implements OnInit {

  student!: StudentDetailsDto;
  loading = true;

  tabs = ['Informații', 'Cursuri', 'Financiar', 'Istoric'];
  activeTab = 'Informații';

  // 🔥 NOI
  courses: StudentCourseDetailsDto[] = [];
  totalAmount = 0;
  coursesLoaded = false;
  contract: any = null;


  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private students: StudentsService, 
    private contracts: ContractsService, 
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
  const id = Number(this.route.snapshot.paramMap.get('id'));

  this.students.get(id).subscribe({
    next: (res) => {
      this.student = res;
      this.loading = false;

      if (this.student?.id) {
        this.loadContract(); // ✅ aici
      }
    },
    error: () => {
      this.loading = false;
      alert('Nu am putut încărca elevul.');
      this.router.navigate(['/students']);
    }
  });
}
  goBack() {
    this.router.navigate(['/students']);
  }

  
  // 🔥 schimbare tab corectă
  onTabChange(tab: string) {
    this.activeTab = tab;

    if (tab === 'Cursuri' && !this.coursesLoaded) {
      this.loadCourses();
    }
  }

  loadCourses() {
    this.students.getStudentCourses(this.student.id)
      .subscribe(res => {
        this.courses = res.items;
        this.totalAmount = res.totalAmount;
        this.coursesLoaded = true;
      });
  }

 getRomanianDay(day: any): string {

  const mapByNumber: Record<number, string> = {
    0: 'Duminică',
    1: 'Luni',
    2: 'Marți',
    3: 'Miercuri',
    4: 'Joi',
    5: 'Vineri',
    6: 'Sâmbătă'
  };

  if (!isNaN(day)) {
    return mapByNumber[Number(day)] ?? day;
  }

  const normalized = day?.toString().toLowerCase();

  const mapByName: Record<string, string> = {
    monday: 'Luni',
    tuesday: 'Marți',
    wednesday: 'Miercuri',
    thursday: 'Joi',
    friday: 'Vineri',
    saturday: 'Sâmbătă',
    sunday: 'Duminică'
  };

  return mapByName[normalized] ?? day;
}


loadContract() {
  this.contracts.getLatestByStudent(this.student.id)
    .subscribe((res: { value: null; }) => {
      this.contract = res?.value ?? null;
    });
}

get contractAction(): string {

  if (!this.contract) return 'create';

  switch (this.contract.status) {

    case 'Draft':
      return 'finalize';

    case 'Finalized':
      return 'send';

    case 'SentToClient':
      return 'waiting';

    case 'SignedByClient':
      return 'activate';

    case 'Active':
      return 'view';

    case 'Cancelled':
      return 'create';

    default:
      return 'create';
  }
}

getContractButtonText(): string {

  switch (this.contractAction) {

    case 'create':
      return 'Creează contract';

    case 'finalize':
      return 'Finalizează contract';

    case 'send':
      return 'Trimite clientului';

    case 'waiting':
      return 'Așteaptă semnarea clientului';

    case 'activate':
      return 'Semneaza contract';

    case 'view':
      return 'Vizualizează contract';

    default:
      return 'Creează contract';
  }
}

handleContractAction(): void {

  switch (this.contractAction) {

    case 'create':
      this.createContract();
      break;

    case 'finalize':
      this.finalizeContract();
      break;

    case 'send':
      this.sendContract();
      break;

    case 'activate':
      this.activateContract();
      break;

    case 'view':
      this.openContract(this.contract.id);
      break;
  }
}

createContract() {
  this.router.navigate(['/create-contract'], {
    queryParams: { studentId: this.student.id }
  });
}

openContract(id: number) {
  this.router.navigate(['/contracts', id]);
}

 openEdit(id: number) {

  if (this.dialog.openDialogs.length > 0) {
    return;
  }

  const dialogRef = this.dialog.open(StudentFormComponent, {
    width: '720px',
    maxWidth: '92vw',
    panelClass: 'student-dialog',
    data: { id }   // 🔥 AICI ERA PROBLEMA
  });

  dialogRef.afterClosed().subscribe(result => {
    if (result) {
      this.loadContract();
    }
  });

  
}

finalizeContract() {

  this.contracts.finalize(this.contract.id)
    .subscribe(() => {
      this.loadContract();
    });

}

sendContract() {

  this.contracts.send(this.contract.id)
    .subscribe(() => {
      this.loadContract();
    });

}

activateContract() {

  const dialogRef = this.dialog.open(AdminSignatureModalComponent, {
    width: '600px',
    data: this.contract.id
  });

  dialogRef.afterClosed().subscribe(result => {

    if (result) {
      this.loadContract();
    }

  });

}
openEnrollModal() {

  const ref = this.dialog.open(EnrollStudentsComponent, {
    width: '600px',
    data: {
      studentId: this.student.id
    }
  });

  ref.afterClosed().subscribe(result => {
    if (result) {
      this.loadCourses(); // reîncarcă lista de cursuri
    }
  });

}
}

