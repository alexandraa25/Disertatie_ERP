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
import { ActivityLog } from '../../models/activity-log.model';
import { ActivityLogService } from '../../services/activity-log.service';
import { CoursesService } from '../../services/courses.service';

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

  courses: StudentCourseDetailsDto[] = [];
  inactiveCourses: StudentCourseDetailsDto[] = [];
  totalAmount = 0;
  coursesLoaded = false;
  contract: any = null;

  activityLogs: ActivityLog[] = [];

   objectKeys = Object.keys;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private students: StudentsService,
    private course: CoursesService,
    private contracts: ContractsService,
    private dialog: MatDialog,
    private activityService: ActivityLogService
  ) { }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.students.get(id).subscribe({
      next: (res) => {
        this.student = res;
        this.loading = false;

        if (this.student?.id) {
          this.loadContract(); 
        }

         this.loadActivity();
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

  onTabChange(tab: string) {
    this.activeTab = tab;

    if (tab === 'Cursuri' && !this.coursesLoaded) {
      this.loadCourses();
    }
  }

  loadCourses() {
  this.students.getStudentCourses(this.student.id)
    .subscribe(res => {

      const all = res.items;

      // 🟢 ACTIVE (ce aveai înainte)
      this.courses = all.filter(x => x.isActive);

      // 🔴 INACTIVE (noi)
      this.inactiveCourses = all.filter(x => !x.isActive);

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

deactivateEnrollment(course: any) {

  if (!confirm(`Elimini studentul din cursul "${course.courseName}"?`)) return;

 this.course.setEnrollmentActive(
  course.courseId,
  course.sessionId,
  this.student.id,
  false
).subscribe({
  next: () => this.loadCourses(),
  error: () => alert('Eroare la scoaterea din curs.')
});
} 

  loadContract() {
    this.contracts.getLatestByStudent(this.student.id)
      .subscribe((res: { value: null; }) => {
        this.contract = res?.value ?? null;
        console.log(this.contract);
      });
     
  }

 get contractAction(): string {

  if (!this.contract) return 'create';

  switch (this.contract.status) {

    case 'Draft':
      return 'edit';

    case 'Finalized':
      return 'send';

    case 'SentToClient':
      return 'waiting';

    case 'SignedByClient':
      return 'sign-admin';

    case 'Active':
      return 'view';

    case 'Suspended':
      return 'resume';

    case 'Completed':
    case 'Expired':
    case 'Cancelled':
      return 'create';

    default:
      return 'create';
  }
}

 getContractButtonText(): string {

  switch (this.contractAction) {

    case 'create':
      return '➕ Creează contract';

    case 'edit':
      return '✏️ Editează / Finalizează';

    case 'send':
      return '📤 Trimite clientului';

    case 'waiting':
      return '⏳ Așteaptă semnarea';

    case 'sign-admin':
      return '✍️ Semnează (Admin)';

    case 'view':
      return '📄 Vizualizează';

    case 'resume':
      return '▶️ Reia contract';

    default:
      return '➕ Creează contract';
  }
}

 handleContractAction(): void {

  switch (this.contractAction) {

    case 'create':
      this.createContract();
      break;

    case 'edit':
      this.openContract(this.contract.id); // sau edit
      break;

    case 'send':
      this.sendContract();
      break;

    case 'sign-admin':
      this.activateContract();
      break;

    case 'view':
      this.openContract(this.contract.id);
      break;

    case 'waiting':
      // nu face nimic sau show mesaj
      alert('Contractul a fost trimis și așteaptă semnarea clientului');
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
      data: { id }  
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadContract();
      }
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

  isExpired(): boolean {
  return !!this.contract?.endDate &&
         new Date(this.contract.endDate) < new Date();
}

isExpiringSoon(): boolean {
  if (!this.contract?.endDate) return false;

  const end = new Date(this.contract.endDate);
  const today = new Date();

  const diff = (end.getTime() - today.getTime()) / (1000 * 3600 * 24);

  return diff > 0 && diff <= 7; // 7 zile
}


suspend() {
  if (!confirm('Suspendăm contractul?')) return;

  this.contract.suspend(this.contract.id)
    .subscribe(() => this.loadContract());
}

resume() {
  this.contract.resume(this.contract.id)
    .subscribe(() => this.loadContract());
}

complete() {
  if (!confirm('Finalizezi contractul?')) return;

  this.contract.complete(this.contract.id)
    .subscribe(() => this.loadContract());
}

cancelContract() {
  if (!confirm('Anulezi contractul?')) return;

  this.contract.cancel(this.contract.id)
    .subscribe(() => this.loadContract());
}


get daysLeft(): number {
  if (!this.contract?.endDate) return 0;

  const end = new Date(this.contract.endDate);
  const today = new Date();

  return Math.ceil((end.getTime() - today.getTime()) / (1000 * 3600 * 24));
}

  downloadPdf(): void {
  if (!this.contract?.id) return;

  this.loading = true;

  this.contracts.download(this.contract.id).subscribe({
    next: (blob: Blob) => {

      const url = window.URL.createObjectURL(blob);

      const fileName =
        this.contract?.contractNumber
          ? `contract_${this.contract.contractNumber}.pdf`
          : 'contract.pdf';

      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();

      window.URL.revokeObjectURL(url);
      this.loading = false;
    },
    error: () => {
      this.loading = false;
      alert('Eroare la descărcare PDF');
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
        this.loadCourses(); 
      }
    });

  }
loadActivity() {
  if (!this.student?.id) return;

  this.activityService
    .getActivity('Student', this.student.id)
    .subscribe((res: ActivityLog[]) => {
      this.activityLogs = res;
    });
}

  createAct() {
  this.router.navigate([`/contracts/${this.contract.id}/additional-act`]);
}


}

