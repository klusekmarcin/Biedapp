import { Component, OnInit } from '@angular/core';
import { CreateTransactionRequest, TransactionType } from '../../../../../../core/models/transaction.model';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { formatDateToDateOnly } from '../../../../../../core/utils/date-utils';

@Component({
  selector: 'app-transaction-modal-create-template',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './transaction-modal-create-template.component.html',
  styleUrl: './transaction-modal-create-template.component.scss'
})
export class TransactionModalCreateTemplateComponent {
  TransactionType = TransactionType;
  
  formData: CreateTransactionRequest = {
    amount: 0,
    currency: 'PLN',
    category: '',
    description: '',
    type: TransactionType.Expense,
    transactionDate: new Date()
  };

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

  getFormData(): CreateTransactionRequest {
    return {
      ...this.formData,
      amount: Number(this.formData.amount)
    };
  }

  resetForm(): void {
    this.formData = {
      amount: 0,
      currency: 'PLN',
      category: '',
      description: '',
      type: TransactionType.Expense,
      transactionDate: new Date()
    };
  }

  get transactionDateAsString(): string {
    return formatDateToDateOnly(`${this.formData.transactionDate ?? new Date()}`);
  }

  setDateFromString(newDate: string): void {
    this.formData.transactionDate = new Date(newDate);
  }
}