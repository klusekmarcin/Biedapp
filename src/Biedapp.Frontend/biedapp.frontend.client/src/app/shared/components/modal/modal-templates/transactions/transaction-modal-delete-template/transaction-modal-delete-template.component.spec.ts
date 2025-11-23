import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionModalDeleteTemplateComponent } from './transaction-modal-delete-template.component';

describe('TransactionModalDeleteTemplateComponent', () => {
  let component: TransactionModalDeleteTemplateComponent;
  let fixture: ComponentFixture<TransactionModalDeleteTemplateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionModalDeleteTemplateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionModalDeleteTemplateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
