import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionModalEditTemplateComponent } from './transaction-modal-edit-template.component';

describe('TransactionModalEditTemplateComponent', () => {
  let component: TransactionModalEditTemplateComponent;
  let fixture: ComponentFixture<TransactionModalEditTemplateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionModalEditTemplateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionModalEditTemplateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
