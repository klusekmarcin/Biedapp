import { CommonModule } from '@angular/common';
import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { CreateTransactionRequest, Transaction, TransactionType, UpdateTransactionRequest } from '../../core/models/transaction.model';
import { BudgetApiService } from '../../core/services/budget-api.service';
import { ModalService, ModalTemplate } from '../../core/services/modal.service';
import { CurrencyFormatPipe } from '../../shared/pipes/currency-format.pipe';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyFormatPipe],
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss']
})
export class TransactionsComponent implements OnInit {
  @ViewChild('transactionFormTemplate') transactionFormTemplate!: TemplateRef<any>;

  transactions: Transaction[] = [];
  categories: string[] = [];
  loading = false;

  constructor(
    private budgetApi: BudgetApiService,
    private modalService: ModalService,
    title: Title) {
    title.setTitle("Transactions - Biedapp");
  }

  ngOnInit(): void {
    this.loadTransactions();
    this.loadCategories();
  }

  loadTransactions(): void {
    this.loading = true;
    this.budgetApi.getTransactions().subscribe({
      next: (data) => {
        console.log(JSON.stringify(data, null, 2))
        this.transactions = data;
      },
      error: (err) => {
        console.error('Error loading transactions:', err);
      },
      complete: () => { this.loading = false; }
    });
  }

  loadCategories(): void {
    this.budgetApi.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: (err) => console.error('Error loading categories:', err)
    });
  }

  toDateString($event: any): Date {
    return new Date($event);
  }

  openAddForm(): void {
    this.modalService.open({
      templateName: ModalTemplate.TRANSACTION_CREATE,
      title: 'Add Transaction',
      submitButtonConfig: {
        show: true,
        customText: 'Create',
        customBackgroundColor: 'green',
        customColor: 'white'
      },
      resetButtonConfig: {
        show: true
      },
      size: 'md'
    }).subscribe(result => {
      if(result.action === 'submit') {
        this.createTransaction(result.data);
      }
      else if (result.action === 'close') {}
      else if(result.action === 'reset') {}
    });
  }

  editTransaction(transaction: Transaction): void {
    this.modalService.open({
      templateName: ModalTemplate.TRANSACTION_EDIT,
      title: 'Edit Transaction',
      submitButtonConfig: {
        show: true,
        customText: 'Update',
      },
      resetButtonConfig: {
        show: true
      },
      size: 'md',
      data: transaction
    }).subscribe(result => {
      if(result.action === 'submit') {
        this.updateTransaction(result.data);
      }
    });
  }

  deleteTransaction(transaction: Transaction): void {
    this.modalService.open({
      templateName: ModalTemplate.TRANSACTION_DELETE,
      title: 'Delete Transaction',
      submitButtonConfig: {
        show: true,
        customText: 'Delete',
        customBackgroundColor: 'red',
        customColor: 'white'
      },
      size: 'md',
      data: transaction
    }).subscribe(result => {
      if(result.action === 'submit'){
        if (confirm('Are you sure you want to delete this transaction?')) {
          this.budgetApi.deleteTransaction(result.data).subscribe({
            next: () => this.loadTransactions(),
            error: (err) => console.error('Error deleting transaction:', err)
          });
        }
      }
    })

    
  }

  createTransaction(transactionCreate: CreateTransactionRequest): void {
    if(!transactionCreate) return;

    this.budgetApi.addTransaction(transactionCreate).subscribe({
      next: () => {
        this.loadTransactions();
      },
      error: (err) => console.error('Error adding transaction:', err)
    });
  }

  updateTransaction(transactionUpdate: UpdateTransactionRequest): void {
    if(!transactionUpdate) return;

    this.budgetApi.updateTransaction(transactionUpdate.id, transactionUpdate).subscribe({
      next: () => {
        this.loadTransactions();
      },
      error: (err) => console.error('Error updating transaction:', err)
    });
  }

  getTypeClass(type: TransactionType): string {
    return type === TransactionType.Income ? 'income' : 'expense';
  }

  getTypeSign(type: TransactionType): string {
    return type === TransactionType.Income ? '+' : '-';
  }
}