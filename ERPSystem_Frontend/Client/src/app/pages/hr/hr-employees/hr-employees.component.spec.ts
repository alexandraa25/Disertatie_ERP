import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HrEmployeesComponent } from './hr-employees.component';

describe('HrEmployeesComponent', () => {
  let component: HrEmployeesComponent;
  let fixture: ComponentFixture<HrEmployeesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HrEmployeesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HrEmployeesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
