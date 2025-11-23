import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionModalCreateTemplateComponent } from './transaction-modal-create-template.component';

describe('TransactionModalCreateTemplateComponent', () => {
  let component: TransactionModalCreateTemplateComponent;
  let fixture: ComponentFixture<TransactionModalCreateTemplateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionModalCreateTemplateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionModalCreateTemplateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
