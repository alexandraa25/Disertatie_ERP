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
import { AdditionalActService } from '../../services/additional-act.service';
import { AdditionalActListDto } from '../../models/additional-act.model';
import { PaymentsService } from '../../services/payments.service';
import { ConfirmService } from '../../services/confirm.service';
import { PayModalComponent } from '../../financiar/pay-modal/pay-modal.component';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-details',
  standalone: true,
  imports: [CommonModule,  FormsModule],
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
  contractsList: any[] = [];
  contract: any;

  acts: AdditionalActListDto[] = [];

  activityLogs: ActivityLog[] = [];

  installments: any[] = [];
  payments: any[] = [];

  total = 0;
  paid = 0;
  remaining = 0;

  objectKeys = Object.keys;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private students: StudentsService,
    private course: CoursesService,
    private contracts: ContractsService,
    private payment: PaymentsService,
    private additionalActService: AdditionalActService,
    private dialog: MatDialog,
    private activityService: ActivityLogService, 
    private confirmService: ConfirmService
  ) { }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.students.get(id).subscribe({
      next: (res) => {
        this.student = res;
        this.loading = false;

        if (this.student?.id) {
          this.loadContractsList();
        }

           if (this.contractsList?.length && !this.contract) {
    this.contract = this.contractsList[0];
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

        this.courses = all.filter(x => x.isActive);

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

  async deactivateEnrollment(course: any) {

  const ok = await this.confirmService.confirm(
    `Elimini studentul din cursul "${course.courseName}"?`
  );

  if (!ok) return;

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

  loadContractsList() {
    this.contracts.listContracts(this.student.id)
      .subscribe((res: any) => {

        this.contractsList = res?.value ?? [];

        // 🔥 select automat contract
        this.contract =
          this.contractsList.find(c => c.status === 'Active') ||
          this.contractsList[0] ||
          null;

        if (this.contract?.id) {
          this.loadContractDetails(this.contract.id);
        }
      });
  }

  loadContractDetails(id: number) {
    this.contracts.getById(id)
      .subscribe((res: any) => {

        this.contract = res?.value ?? null;

        if (this.contract?.id) {
          this.loadActs();
          this.loadFinancial();
        }
      });
  }

  selectContract(c: any) {
    this.contract = c; // instant UI
    this.loadContractDetails(c.id); // refresh real
  }


  onSelectContract(contractId: number) {
  const selected = this.contractsList.find(c => c.id == contractId);
  if (selected) {
    this.selectContract(selected);
  }
}
  get sortedContracts() {
  if (!this.contractsList) return [];

  return [
    this.contract,
    ...this.contractsList.filter(c => c.id !== this.contract?.id)
  ];
}

  get activeContracts() {
    return this.contractsList.filter(c =>
      ['Active', 'Draft', 'Finalized'].includes(c.status)
    );
  }

  get inactiveContracts() {
    return this.contractsList.filter(c =>
      ['Expired', 'Cancelled', 'Completed'].includes(c.status)
    );
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
        this.loadContractsList();
      }
    });

  }

  sendContract() {
    this.contracts.send(this.contract.id)
      .subscribe(() => {
        this.loadContractsList();
      });

  }

  activateContract() {

    const dialogRef = this.dialog.open(AdminSignatureModalComponent, {
      width: '600px',
      data: this.contract.id
    });

    dialogRef.afterClosed().subscribe(result => {

      if (result) {
        this.loadContractsList();
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

  get daysLeft(): number {
    if (!this.contract?.endDate) return 0;

    const end = new Date(this.contract.endDate);
    const today = new Date();

    return Math.ceil((end.getTime() - today.getTime()) / (1000 * 3600 * 24));
  }

  async suspend() {
  if (!(await this.confirmService.confirm('Suspendăm contractul?'))) return;

  this.contracts.suspend(this.contract.id)
    .subscribe(() => this.loadContractsList());
}

 async resume() {
  if (!(await this.confirmService.confirm('Reluăm contractul?'))) return;

  this.contracts.resume(this.contract.id)
    .subscribe(() => this.loadContractsList());
}

 async complete() {
  if (!(await this.confirmService.confirm('Finalizăm contractul?'))) return;

  this.contracts.complete(this.contract.id)
    .subscribe(() => this.loadContractsList());
}

 async cancelContract() {
  if (!(await this.confirmService.confirm('Anulăm contractul?'))) return;

  this.contracts.cancel(this.contract.id)
    .subscribe(() => this.loadContractsList());
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

  createAct() {
    this.router.navigate([`/contracts/${this.contract.id}/additional-act`]);
  }

  loadActs() {
    if (!this.contract?.id) return;

    this.additionalActService.getByContract(this.contract.id)
      .subscribe((res: any) => {
        this.acts = res.value;
      });
  }
  openAct(id: number) {
    this.router.navigate(['/additional-act', id]);
  }

  loadFinancial() {
    if (!this.contract) return;

    this.payment.getInstallments(this.contract.id)
      .subscribe((res: any) => {
        this.installments = res;
        this.calculateSummary();
      });

    this.payment.getPayments(this.contract.id)
      .subscribe((res: any) => {
        this.payments = res;
      });
  }

  calculateSummary() {
    this.total = this.installments.reduce((s, i) => s + i.amount, 0);
    this.paid = this.installments.reduce((s, i) => s + i.paidAmount, 0);
    this.remaining = this.total - this.paid;
  }

  getInstallmentStatus(i: any) {
    if (i.paidAmount === 0) return 'Neplătit';
    if (i.paidAmount < i.amount) return 'Parțial';
    return 'Plătit';
  }

  openPayModal(i: any) {

  const remaining = i.amount - i.paidAmount;

  const dialogRef = this.dialog.open(PayModalComponent, {
   width: '550px',
  maxHeight: '90vh',
  panelClass: 'custom-dialog',
    data: { remaining }
  });

  dialogRef.afterClosed().subscribe(result => {
    if (!result) return;

    console.log(i);

    console.log({
  InstallmentId: i.id,
  Amount: result.amount,
  Method: result.method,
  Notes: result.notes,
  Reference: result.reference
});

    this.payment.payInstallment({
      InstallmentId: i.id,
  Amount: result.amount,
  Method: result.method,
  Notes: result.notes,
  Reference: result.reference
    }).subscribe({
      next: () => this.loadFinancial(),
      error: () => alert('Eroare la plată')
    });
  });
}

  getInstallmentClass(i: any) {

    const overdue =
      i.paidAmount < i.amount &&
      new Date(i.dueDate) < new Date();

    if (overdue) return 'danger';

    if (i.paidAmount < i.amount) return 'warning';

    return 'success';
  }

  loadActivity() {
    if (!this.student?.id) return;

    this.activityService
      .getActivity('Student', this.student.id)
      .subscribe((res: ActivityLog[]) => {
        this.activityLogs = res;
      });
  }

}

