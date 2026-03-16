import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminSignatureModalComponent } from './admin-signature-modal.component';

describe('AdminSignatureModalComponent', () => {
  let component: AdminSignatureModalComponent;
  let fixture: ComponentFixture<AdminSignatureModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSignatureModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminSignatureModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
