import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SignContractComponent } from './sign-contract.component';

describe('SignContractComponent', () => {
  let component: SignContractComponent;
  let fixture: ComponentFixture<SignContractComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignContractComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SignContractComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
