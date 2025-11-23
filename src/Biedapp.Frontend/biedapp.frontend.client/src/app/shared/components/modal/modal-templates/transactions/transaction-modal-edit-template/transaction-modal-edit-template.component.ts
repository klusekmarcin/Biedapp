import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Transaction, TransactionType, UpdateTransactionRequest } from '../../../../../../core/models/transaction.model';
import { formatDateToDateOnly } from '../../../../../../core/utils/date-utils';

@Component({
  selector: 'app-transaction-modal-edit-template',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './transaction-modal-edit-template.component.html',
  styleUrl: './transaction-modal-edit-template.component.scss'
})
export class TransactionModalEditTemplateComponent implements OnInit {
  @Input() data?: Transaction;
  
  TransactionType = TransactionType;
  
  formData: UpdateTransactionRequest = {
    id: '',
    amount: 0,
    currency: 'PLN',
    category: '',
    description: '',
    type: TransactionType.Expense,
    transactionDate: new Date()
  };

  originalData: UpdateTransactionRequest | null = null;

  categories: string[] = [
    'Food & Dining',
    'Transportation',
    'Shopping',
    'Entertainment',
    'Bills & Utilities',
    'Healthcare',
    'Salary',
    'Freelance',
    'Investment',
    'Other'
  ];

  ngOnInit(): void {
    if(this.data) {
      this.formData = {
        id: this.data.id,
        amount: this.data.amount,
        currency: this.data.currency,
        category: this.data.category,
        description: this.data.description,
        type: this.data.type,
        transactionDate: this.data.transactionDate
      };

      this.originalData = { ...this.formData };
    }
  }

  getFormData(): UpdateTransactionRequest {
    return {
      ...this.formData,
      amount: Number(this.formData.amount)
    };
  }

  resetForm(): void {
    if (this.originalData)
      this.formData = { ...this.originalData };
  }

  get transactionDateAsString(): string {
    return formatDateToDateOnly(`${this.formData.transactionDate}`);
  }

  setDateFromString(newDate: string): void {
    this.formData.transactionDate = new Date(newDate);
  }
}
