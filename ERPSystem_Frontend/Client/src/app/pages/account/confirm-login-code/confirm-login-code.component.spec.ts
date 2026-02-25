import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfirmLoginCodeComponent } from './confirm-login-code.component';

describe('ConfirmLoginCodeComponent', () => {
  let component: ConfirmLoginCodeComponent;
  let fixture: ComponentFixture<ConfirmLoginCodeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmLoginCodeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ConfirmLoginCodeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
