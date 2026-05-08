import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
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
import { ConfirmCustomModalComponent } from '../../../components/confirm-custom-modal/confirm-custom-modal.component';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { PayModalComponent } from '../../financiar/pay-modal/pay-modal.component';
import { FormsModule } from '@angular/forms';
import { FeedbackService } from '../../services/feedback.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-student-details',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmCustomModalComponent],
  templateUrl: './student-details.component.html',
  styleUrls: ['./student-details.component.css']
})
export class StudentDetailsComponent implements OnInit, OnDestroy {

  student!: StudentDetailsDto;
  loading = true;

  tabs = ['Informații', 'Cursuri', 'Financiar', 'Istoric', 'Evaluări profesor'];
  activeTab = 'Informații';

  studentTab: 'evaluations' | 'analytics' = 'evaluations';

  studentAnalytics: any = null;
  loadingStudentAnalytics = false;
  studentChart: any;

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

  studentEvaluations: any[] = [];
  loadingEvaluations = false;

  coursesPage = 1;
  inactiveCoursesPage = 1;
  installmentsPage = 1;
  paymentsPage = 1;
  activityPage = 1;
  evaluationsPage = 1;

  pageSize = 10;

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
    private confirmService: ConfirmService,
    private feedbackService: FeedbackService,
    private snackbar: SnackbarService,
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
        this.snackbar.showError('Nu am putut încărca elevul.', 2500);
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

    if (tab === 'Istoric') {
      this.loadActivity();
    }
    if (tab === 'Evaluări profesor') {
      this.loadStudentEvaluations();
    }
  }

  loadCourses(): void {
    this.students.getStudentCourses(this.student.id).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res;
        const all = data.items ?? [];

        this.courses = all.filter((x: any) => x.isActive);
        this.inactiveCourses = all.filter((x: any) => !x.isActive);
        this.totalAmount = data.totalAmount ?? 0;
        this.coursesLoaded = true;
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea cursurilor.'),
          2500
        );
      }
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

  openEnrollModal(): void {
    if (!this.student.isActive || this.student.isDeleted) {
      this.snackbar.showError(
        this.student.isDeleted
          ? 'Cursantul este arhivat și nu poate fi înscris.'
          : 'Cursantul este inactiv și nu poate fi înscris.',
        2500
      );
      return;
    }

    const ref = this.dialog.open(EnrollStudentsComponent, {
      width: '600px',
      data: { studentId: this.student.id }
    });

    ref.afterClosed().subscribe(result => {
      if (result) {
        this.snackbar.showSuccess('Lista cursurilor a fost actualizată.', 1500);
        this.loadCourses();
      }
    });
  }

  async deactivateEnrollment(course: any): Promise<void> {
    const ok = await this.confirmService.confirm(
      `Elimini cursantul din cursul "${course.courseName}"?`,
      'Confirmare'
    );

    if (!ok) return;

    this.course.setEnrollmentActive(
      course.courseId,
      course.sessionId,
      this.student.id,
      false
    ).subscribe({
      next: () => {
        this.snackbar.showSuccess('Cursantul a fost scos din curs.', 1800);
        this.loadCourses();
      },
      error: (err) => {
        this.snackbar.showError(
          err?.error?.message || 'Eroare la scoaterea din curs.',
          2500
        );
      }
    });
  }

  loadContractsList(): void {
    this.contracts.listContracts(this.student.id).subscribe({
      next: (res: any) => {
        this.contractsList = res?.value ?? [];

        this.contract =
          this.contractsList.find(c => c.status === 'Active') ||
          this.contractsList[0] ||
          null;

        if (this.contract?.id) {
          this.loadContractDetails(this.contract.id);
        }
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea contractelor.'),
          2500
        );
      }
    });
  }

  loadContractDetails(id: number): void {
    this.contracts.getById(id).subscribe({
      next: (res: any) => {
        this.contract = res?.value ?? null;

        if (this.contract?.id) {
          this.loadActs();
          this.loadFinancial();
        }
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea contractului.'),
          2500
        );
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
        this.openContract(this.contract.id); 
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
        this.snackbar.showError('Contractul a fost trimis și așteaptă semnarea clientului.', 2500);
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

  openEdit(id: number): void {
    if (this.student?.isDeleted) {
      this.snackbar.showError('Cursantul arhivat nu poate fi editat.', 2500);
      return;
    }

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
        this.snackbar.showSuccess('Datele cursantului au fost actualizate.', 1800);
        this.students.get(this.student.id).subscribe({
          next: (res) => this.student = res,
          error: () => this.loadContractsList()
        });
        this.loadContractsList();
      }
    });
  }

  async sendContract(): Promise<void> {
    const ok = await this.confirmService.confirm(
      `Trimiți contractul #${this.contract?.contractNumber} către client?`,
      'Confirmare trimitere'
    );

    if (!ok) return;

    this.contracts.send(this.contract.id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Contractul a fost trimis clientului.', 1800);
        this.loadContractsList();
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Contractul nu a putut fi trimis.'),
          2500
        );
      }
    });
  }

  activateContract() {
    const dialogRef = this.dialog.open(AdminSignatureModalComponent, {
      width: '600px',
      data: {
        id: this.contract.id,
        type: 'contract'
      }
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

  async complete(): Promise<void> {
    if (!(await this.confirmService.confirm('Finalizăm contractul?', 'Confirmare'))) return;

    this.contracts.complete(this.contract.id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Contract finalizat.', 1800);
        this.loadContractsList();
      },
      error: (err) => this.snackbar.showError(this.getErrorMessage(err, 'Contractul nu a putut fi finalizat.'), 2500)
    });
  }

  async cancelContract(): Promise<void> {
    if (!(await this.confirmService.confirm('Anulăm contractul?', 'Confirmare anulare'))) return;

    this.contracts.cancel(this.contract.id).subscribe({
      next: () => {
        this.snackbar.showSuccess('Contract anulat.', 1800);
        this.loadContractsList();
      },
      error: (err) => this.snackbar.showError(this.getErrorMessage(err, 'Contractul nu a putut fi anulat.'), 2500)
    });
  }

  downloadPdf(): void {
    if (!this.contract?.id) {
      this.snackbar.showError('Nu există contract selectat.', 2500);
      return;
    }

    this.loading = true;

    this.contracts.download(this.contract.id).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);

        const fileName = this.contract?.contractNumber
          ? `contract_${this.contract.contractNumber}.pdf`
          : 'contract.pdf';

        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();

        window.URL.revokeObjectURL(url);
        this.loading = false;

        this.snackbar.showSuccess('PDF descărcat.', 1500);
      },
      error: (err) => {
        this.loading = false;
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la descărcare PDF.'),
          2500
        );
      }
    });
  }

  createAct() {
    this.router.navigate([`/contracts/${this.contract.id}/additional-act`]);
  }

  loadActs(): void {
    if (!this.contract?.id) return;

    this.additionalActService.getByContract(this.contract.id).subscribe({
      next: (res: any) => {
        this.acts = res?.value ?? res ?? [];
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea actelor adiționale.'),
          2500
        );
      }
    });
  }

  openAct(id: number) {
    this.router.navigate(['/additional-act', id]);
  }

  loadFinancial(): void {
    if (!this.contract) return;

    this.payment.getInstallments(this.contract.id).subscribe({
      next: (res: any) => {
        this.installments = res?.value ?? res ?? [];
        this.calculateSummary();
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea ratelor.'),
          2500
        );
      }
    });

    this.payment.getPayments(this.contract.id).subscribe({
      next: (res: any) => {
        this.payments = res?.value ?? res ?? [];
      },
      error: (err) => {
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea plăților.'),
          2500
        );
      }
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
        next: () => {
          this.snackbar.showSuccess('Plata a fost înregistrată.', 1800);
          this.loadFinancial();
        },
        error: (err) => {
          this.snackbar.showError(
            this.getErrorMessage(err, 'Eroare la plată.'),
            2500
          );
        }
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

  loadActivity(): void {
    if (!this.student?.id) return;

    this.activityService
      .getActivity('Student', this.student.id.toString())
      .subscribe({
        next: (res: ActivityLog[]) => {
          this.activityLogs = res ?? [];
        },
        error: (err) => {
          this.snackbar.showError(
            this.getErrorMessage(err, 'Eroare la încărcarea istoricului.'),
            2500
          );
        }
      });
  }

  loadStudentEvaluations(): void {
    if (!this.student?.id) return;

    this.loadingEvaluations = true;

    this.feedbackService.getStudentEvaluations(this.student.id).subscribe({
      next: (res: any) => {
        const data = res?.value ?? res?.data ?? res;
        this.studentEvaluations = Array.isArray(data) ? data : [];
        this.loadingEvaluations = false;
      },
      error: (err) => {
        this.loadingEvaluations = false;
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea evaluărilor.'),
          2500
        );
      }
    });
  }

  loadStudentAnalytics(): void {
    if (!this.student?.id) return;

    this.loadingStudentAnalytics = true;

    this.feedbackService.getStudentAnalytics(this.student.id).subscribe({
      next: (res: any) => {
        this.studentAnalytics = res?.value ?? res?.data ?? res;
        this.loadingStudentAnalytics = false;

        setTimeout(() => this.createStudentChart(), 0);
      },
      error: (err) => {
        this.loadingStudentAnalytics = false;
        this.snackbar.showError(
          this.getErrorMessage(err, 'Eroare la încărcarea analizei AI.'),
          2500
        );
      }
    });
  }

  setStudentTab(tab: 'evaluations' | 'analytics') {
    this.studentTab = tab;

    if (tab === 'evaluations') {
      this.destroyStudentChart();
      return;
    }

    if (!this.studentAnalytics) {
      this.loadStudentAnalytics();
      return;
    }

    setTimeout(() => this.createStudentChart(), 0);
  }

  createStudentChart(): void {
    if (!this.studentAnalytics?.trend?.length) return;

    const canvas = document.getElementById('studentTrendChart') as HTMLCanvasElement;
    if (!canvas) return;

    this.destroyStudentChart();

    this.studentChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels: this.studentAnalytics.trend.map((x: any) => x.month),
        datasets: [
          {
            label: 'Rating',
            data: this.studentAnalytics.trend.map((x: any) => x.averageRating),
            tension: 0.3
          },
          {
            label: 'Pozitiv %',
            data: this.studentAnalytics.trend.map((x: any) => x.positivePercent),
            tension: 0.3
          },
          {
            label: 'Negativ %',
            data: this.studentAnalytics.trend.map((x: any) => x.negativePercent),
            tension: 0.3
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false
      }
    });
  }

  ngOnDestroy(): void {
    this.destroyStudentChart();
  }

  destroyStudentChart(): void {
    if (this.studentChart) {
      this.studentChart.destroy();
      this.studentChart = null;
    }
  }

  getRiskLabel(level: string): string {
    switch (level) {
      case 'high': return 'Ridicat';
      case 'medium': return 'Mediu';
      case 'low': return 'Scăzut';
      default: return 'Necunoscut';
    }
  }

  private getErrorMessage(err: any, fallback: string): string {
    return err?.error?.message ||
      err?.error?.errorMessage ||
      err?.error?.title ||
      fallback;
  }

  get pagedCourses() {
    return this.paginate(this.courses, this.coursesPage);
  }

  get pagedInactiveCourses() {
    return this.paginate(this.inactiveCourses, this.inactiveCoursesPage);
  }

  get pagedInstallments() {
    return this.paginate(this.installments, this.installmentsPage);
  }

  get pagedPayments() {
    return this.paginate(this.payments, this.paymentsPage);
  }

  get pagedActivityLogs() {
    return this.paginate(this.activityLogs, this.activityPage);
  }

  get pagedStudentEvaluations() {
    return this.paginate(this.studentEvaluations, this.evaluationsPage);
  }

  totalPages(list: any[]): number {
    return Math.max(1, Math.ceil((list?.length ?? 0) / this.pageSize));
  }

  private paginate(list: any[], page: number): any[] {
    const start = (page - 1) * this.pageSize;
    return (list ?? []).slice(start, start + this.pageSize);
  }

}

