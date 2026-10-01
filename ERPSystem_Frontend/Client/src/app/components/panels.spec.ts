import { TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { ProfileNotificationsComponent } from '../pages/account/profile-notifications/profile-notifications.component';
import { StudentFeedbackPanelComponent } from '../pages/academics/student-feedback-panel/student-feedback-panel.component';
import { StudentActivityPanelComponent } from '../pages/academics/student-activity-panel/student-activity-panel.component';
import { PowerBiModalComponent } from '../pages/dashboard-analysis/power-bi-modal/power-bi-modal.component';

describe('Notification settings panel', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [ProfileNotificationsComponent] }));

  it('renders loading without reading a missing form', () => {
    const fixture = TestBed.createComponent(ProfileNotificationsComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Se încarcă notificările');
  });

  it('edits the shared form, emits save and disables duplicate submissions', () => {
    const fixture = TestBed.createComponent(ProfileNotificationsComponent);
    const settings = new FormArray([new FormGroup({
      eventType: new FormControl('Leave'), channel: new FormControl(1),
      enabled: new FormControl(false), digest: new FormControl(1)
    })]);
    fixture.componentRef.setInput('notificationForm', new FormGroup({ settings }));
    fixture.componentRef.setInput('loadingNotifications', false);
    fixture.componentRef.setInput('labels', { Leave: 'Concedii' });
    const save = spyOn(fixture.componentInstance.save, 'emit');
    fixture.detectChanges();
    const checkbox: HTMLInputElement = fixture.nativeElement.querySelector('input[type=checkbox]');
    checkbox.click();
    expect(settings.at(0).value.enabled).toBeTrue();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    expect(save).toHaveBeenCalledTimes(1);
    fixture.componentRef.setInput('savingNotifications', true);
    fixture.detectChanges();
    expect(button.disabled).toBeTrue();
    button.click();
    expect(save).toHaveBeenCalledTimes(1);
  });
});

describe('Student feedback panel', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [StudentFeedbackPanelComponent] }));

  it('shows an empty state without evaluations', () => {
    const fixture = TestBed.createComponent(StudentFeedbackPanelComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nu există evaluări');
  });

  it('paginates evaluations and delegates tab changes to the parent', () => {
    const fixture = TestBed.createComponent(StudentFeedbackPanelComponent);
    fixture.componentRef.setInput('studentEvaluations', [
      { teacherName: 'Profesor A', rating: 5 }, { teacherName: 'Profesor B', rating: 4 }
    ]);
    fixture.componentRef.setInput('pageSize', 1);
    const page = spyOn(fixture.componentInstance.evaluationsPageChange, 'emit');
    const tab = spyOn(fixture.componentInstance.tabChange, 'emit');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Profesor A');
    expect(fixture.nativeElement.textContent).not.toContain('Profesor B');
    fixture.nativeElement.querySelector('.pagination button:last-child').click();
    expect(page).toHaveBeenCalledWith(2);
    fixture.componentRef.setInput('evaluationsPage', 2);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Profesor B');
    fixture.nativeElement.querySelector('.tabs button:last-child').click();
    expect(tab).toHaveBeenCalledWith('analytics');
  });
});

describe('Student activity panel', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [StudentActivityPanelComponent] }));

  it('renders an empty activity list', () => {
    const fixture = TestBed.createComponent(StudentActivityPanelComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nu există activitate');
  });

  it('renders property changes when a description is unavailable', () => {
    const fixture = TestBed.createComponent(StudentActivityPanelComponent);
    fixture.componentRef.setInput('activityLogs', [{
      entityType: 'Student', entityId: 1, action: 'Update', createdAtUtc: '2026-01-01T00:00:00Z',
      oldValues: { Name: 'Nume vechi' }, newValues: { Name: 'Nume nou' }
    }]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nume vechi');
    expect(fixture.nativeElement.textContent).toContain('Nume nou');
    expect(fixture.nativeElement.querySelector('.log-update')).not.toBeNull();
  });
});

describe('Power BI report selection', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [PowerBiModalComponent] }));

  it('does not render an iframe for an empty report list', () => {
    const fixture = TestBed.createComponent(PowerBiModalComponent);
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('iframe')).toBeNull();
  });

  it('resets an invalid selection when reports are replaced', () => {
    const fixture = TestBed.createComponent(PowerBiModalComponent);
    fixture.componentInstance.currentIndex = 3;
    fixture.componentRef.setInput('reports', [{ title: 'Raport', url: 'https://example.test/report' }]);
    fixture.detectChanges();
    expect(fixture.componentInstance.currentIndex).toBe(0);
    expect(fixture.componentInstance.currentReport?.title).toBe('Raport');
  });
});
