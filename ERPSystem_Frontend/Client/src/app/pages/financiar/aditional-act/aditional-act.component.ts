import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractsService } from '../../services/contracts.service';
import { StudentsService } from '../../services/students.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentCourseDetailsDto } from '../../models/student.model';
import { AdditionalActService } from '../../services/additional-act.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';
import { RomanianDayPipe } from '../../../components/pipes/romanian-day.pipe';

@Component({
  selector: 'app-aditional-act',
  standalone: true,
  imports: [CommonModule, FormsModule, RomanianDayPipe],
  templateUrl: './aditional-act.component.html',
  styleUrls: ['./aditional-act.component.css']
})
export class AditionalActComponent implements OnInit {

  selectedTypes: string[] = [];

  selectedAddCourseIds: number[] = [];
  selectedRemoveCourseIds: number[] = [];

  newEndDate: string | null = null;
  priceAdjustments: {
    courseSessionId: number | null;
    amount: number | null;
  }[] = [];

  description = '';
  contract: any;

  allCourses: StudentCourseDetailsDto[] = [];
  availableCourses: StudentCourseDetailsDto[] = [];
  removedCoursesFromContract: StudentCourseDetailsDto[] = [];

  actId?: number;
  isEdit = false;
  contractId!: number;
  studentId?: number;

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
      return;
    }

    if (contractId) {
      this.contractId = Number(contractId);
      this.loadContract();
      this.loadCourses();
    }
  }

  toggleType(type: string) {
    const index = this.selectedTypes.indexOf(type);

    if (index > -1) {
      this.selectedTypes.splice(index, 1);

      if (type === 'AddCourse') {
        this.selectedAddCourseIds = [];
      }

      if (type === 'RemoveCourse') {
        this.selectedRemoveCourseIds = [];
      }

      if (type === 'ExtendPeriod') {
        this.newEndDate = null;
      }

      if (type === 'AddDiscount' || type === 'IncreasePrice') {
        this.priceAdjustments = [];
      }

      return;
    }

    if (type === 'AddDiscount') {
      this.selectedTypes = this.selectedTypes.filter(t => t !== 'IncreasePrice');
      this.priceAdjustments = [];
    }

    if (type === 'IncreasePrice') {
      this.selectedTypes = this.selectedTypes.filter(t => t !== 'AddDiscount');
      this.priceAdjustments = [];
    }

    this.selectedTypes.push(type);
  }

  loadContract() {
    this.contractsService.getById(this.contractId).subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false || !res?.value) {
          this.snackbar.showError('Contractul nu a putut fi încărcat.', 2500);
          return;
        }

        this.contract = res.value;
        this.studentId =
          this.contract?.studentId ||
          this.contract?.parties?.find((p: any) => p.studentId)?.studentId;
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
      this.studentId =
        act.studentId ||
        act.parties?.find((p: any) => p.studentId)?.studentId;

      this.loadContract();
      this.loadCourses();

      this.selectedTypes = Array.from(
        new Set<string>(
          act.items.map((i: any) => this.mapEnumToString(i.type))
        )
      );

      this.selectedAddCourseIds = act.items
        .filter((i: any) =>
          this.mapEnumToString(i.type) === 'AddCourse' &&
          i.courseSessionId
        )
        .map((i: any) => Number(i.courseSessionId));

      this.selectedRemoveCourseIds = act.items
        .filter((i: any) =>
          this.mapEnumToString(i.type) === 'RemoveCourse' &&
          i.courseSessionId
        )
        .map((i: any) => Number(i.courseSessionId));

      const extend = act.items.find((i: any) =>
        this.mapEnumToString(i.type) === 'ExtendPeriod'
      );

      if (extend) {
        this.newEndDate = extend.newValue;
      }

      this.priceAdjustments = act.items
        .filter((i: any) =>
          ['AddDiscount', 'IncreasePrice'].includes(this.mapEnumToString(i.type)) &&
          i.courseSessionId
        )
        .map((i: any) => ({
          courseSessionId: Number(i.courseSessionId),
          amount: Number(i.newValue)
        }));

      this.description = act.description;
    });
  }

  loadCourses() {
    this.studentsService.getStudentCoursesByContract(this.contractId)
      .subscribe((res: any) => {
        this.allCourses = res.value.items;

        this.availableCourses = this.allCourses.filter(c =>
          c.isActive && !c.contractId
        );

        this.removedCoursesFromContract = this.allCourses.filter(c =>
          !c.isActive && Number(c.contractId) === Number(this.contractId)
        );
      });
  }

  save() {
    if (!this.selectedTypes.length) {
      this.snackbar.showError('Selectează cel puțin un tip.', 2200);
      return;
    }

    if (
      this.selectedTypes.includes('AddCourse') &&
      this.selectedAddCourseIds.length === 0
    ) {
      this.snackbar.showError('Selectează cel puțin un curs de adăugat.', 2200);
      return;
    }

    if (
      this.selectedTypes.includes('RemoveCourse') &&
      this.selectedRemoveCourseIds.length === 0
    ) {
      this.snackbar.showError('Selectează cel puțin un curs de eliminat.', 2200);
      return;
    }

    if (this.selectedTypes.includes('ExtendPeriod') && !this.newEndDate) {
      this.snackbar.showError('Selectează data nouă de final.', 2200);
      return;
    }

    if (
      (this.selectedTypes.includes('AddDiscount') ||
        this.selectedTypes.includes('IncreasePrice')) &&
      this.priceAdjustments.length === 0
    ) {
      this.snackbar.showError('Adaugă cel puțin o sesiune pentru ajustare.', 2200);
      return;
    }

    if (
      (this.selectedTypes.includes('AddDiscount') ||
        this.selectedTypes.includes('IncreasePrice')) &&
      this.priceAdjustments.some(x => !x.courseSessionId || !x.amount || x.amount <= 0)
    ) {
      this.snackbar.showError('Completează sesiunea și valoarea pentru fiecare ajustare.', 2200);
      return;
    }

    const map: any = {
      AddCourse: 0,
      RemoveCourse: 1,
      ExtendPeriod: 2,
      AddDiscount: 3,
      IncreasePrice: 4
    };

    const dto: any = {
      types: this.selectedTypes.map(t => map[t]),
      description: this.description || '',
      addCourseSessionIds: this.selectedAddCourseIds.map(Number),
      removeCourseSessionIds: this.selectedRemoveCourseIds.map(Number),
      newEndDate: this.selectedTypes.includes('ExtendPeriod')
        ? this.newEndDate
        : null,
      priceAdjustments:
        this.selectedTypes.includes('AddDiscount') ||
          this.selectedTypes.includes('IncreasePrice')
          ? this.priceAdjustments.map(x => ({
            courseSessionId: Number(x.courseSessionId),
            amount: Number(x.amount)
          }))
          : []
    };

    const request$ = this.isEdit && this.actId
      ? this.additionalActService.update(this.actId, dto)
      : this.additionalActService.create(this.contractId, dto);

    request$.subscribe({
      next: (res: any) => {
        if (res?.isSuccess === false) {
          this.snackbar.showError(
            res.error?.errorMessage || 'Actul adițional nu a putut fi salvat.',
            2500
          );
          return;
        }

        const id = this.isEdit ? this.actId : res?.value?.id;

        this.snackbar.showSuccess(
          this.isEdit
            ? 'Act adițional actualizat cu succes.'
            : 'Act adițional creat cu succes.',
          1800
        );

        if (id) {
          this.router.navigate(['/additional-act', id]);
        }
      },
      error: (err) => {
        console.error(err);
        this.snackbar.showError(
          err.error?.message || 'Eroare la salvarea actului adițional.',
          2500
        );
      }
    });
  }

  goBack() {
    if (this.studentId) {
      this.router.navigate(['/students', this.studentId]);
      return;
    }

    this.router.navigate(['/students']);
  }

  mapEnumToString(type: number | string): string {
    if (typeof type === 'string') return type;

    const map: any = {
      0: 'AddCourse',
      1: 'RemoveCourse',
      2: 'ExtendPeriod',
      3: 'AddDiscount',
      4: 'IncreasePrice'
    };

    return map[type];
  }

  addPriceAdjustment() {
    this.priceAdjustments.push({
      courseSessionId: null,
      amount: null
    });
  }

  removePriceAdjustment(index: number) {
    this.priceAdjustments.splice(index, 1);
  }

  get sessionsForAdjustment() {
    return this.allCourses.filter(c =>
      c.isActive ||
      Number(c.contractId) === Number(this.contractId) ||
      this.selectedAddCourseIds.includes(Number(c.sessionId)) ||
      this.selectedRemoveCourseIds.includes(Number(c.sessionId))
    );
  }

  getSessionById(sessionId: number | null) {
    if (!sessionId) return null;

    return this.sessionsForAdjustment.find(x =>
      Number(x.sessionId) === Number(sessionId)
    );
  }

  getAdjustmentPreview(adj: any): string | null {

    if (!adj.courseSessionId || !adj.amount) {
      return null;
    }

    const session = this.sessionsForAdjustment.find(
      x => x.sessionId === adj.courseSessionId
    );

    if (!session) {
      return null;
    }

    const isNewCourse =
      this.selectedAddCourseIds.includes(session.sessionId) &&
      !session.contractId;

    const current = Number(session.price);

    const next = this.selectedTypes.includes('AddDiscount')
      ? Math.max(0, current - Number(adj.amount))
      : current + Number(adj.amount);

    return isNewCourse
      ? `Curs nou: ${current} RON → ${next} RON`
      : `${current} RON → ${next} RON`;
  }

  toggleAddCourse(sessionId: number) {
    const exists = this.selectedAddCourseIds.includes(sessionId);

    if (exists) {
      this.selectedAddCourseIds =
        this.selectedAddCourseIds.filter(x => x !== sessionId);

      return;
    }

    this.selectedAddCourseIds.push(sessionId);
  }

  toggleRemoveCourse(sessionId: number) {
    const exists = this.selectedRemoveCourseIds.includes(sessionId);

    if (exists) {
      this.selectedRemoveCourseIds =
        this.selectedRemoveCourseIds.filter(x => x !== sessionId);

      return;
    }

    this.selectedRemoveCourseIds.push(sessionId);
  }
}