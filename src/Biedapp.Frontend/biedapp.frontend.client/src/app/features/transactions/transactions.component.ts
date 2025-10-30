import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BudgetApiService } from '../../core/services/budget-api.service';
import { Transaction, TransactionType, CreateTransactionRequest } from '../../core/models/transaction.model';
import { CurrencyFormatPipe } from '../../shared/pipes/currency-format.pipe';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyFormatPipe],
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss']
})
export class TransactionsComponent implements OnInit {
  transactions: Transaction[] = [];
  categories: string[] = [];
  loading = false;
  showForm = false;
  editingId: string | null = null;
  
  TransactionType = TransactionType;

  formData: CreateTransactionRequest = {
    amount: 0,
    currency: 'PLN',
    category: '',
    description: '',
    type: TransactionType.Expense,
    date: new Date()
  };

  constructor(
    private budgetApi: BudgetApiService,
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
        this.transactions = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading transactions:', err);
        this.loading = false;
      }
    });
  }

  loadCategories(): void {
    this.budgetApi.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: (err) => console.error('Error loading categories:', err)
    });
  }

  openAddForm(): void {
    this.showForm = true;
    this.editingId = null;
    this.formData = {
      amount: 0,
      currency: 'PLN',
      category: '',
      description: '',
      type: TransactionType.Expense,
      date: new Date()
    };
  }

  editTransaction(transaction: Transaction): void {
    this.showForm = true;
    this.editingId = transaction.id;
    this.formData = {
      amount: transaction.amount,
      currency: transaction.currency,
      category: transaction.category,
      description: transaction.description,
      type: transaction.type,
      date: new Date(transaction.date)
    };
  }

  saveTransaction(): void {
    if (this.editingId) {
      this.budgetApi.updateTransaction(this.editingId, { ...this.formData, id: this.editingId }).subscribe({
        next: () => {
          this.loadTransactions();
          this.closeForm();
        },
        error: (err) => console.error('Error updating transaction:', err)
      });
    } else {
      this.budgetApi.addTransaction(this.formData).subscribe({
        next: () => {
          this.loadTransactions();
          this.closeForm();
        },
        error: (err) => console.error('Error adding transaction:', err)
      });
    }
  }

  deleteTransaction(id: string): void {
    if (confirm('Are you sure you want to delete this transaction?')) {
      this.budgetApi.deleteTransaction(id).subscribe({
        next: () => this.loadTransactions(),
        error: (err) => console.error('Error deleting transaction:', err)
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  getTypeClass(type: TransactionType): string {
    return type === TransactionType.Income ? 'income' : 'expense';
  }

  getTypeSign(type: TransactionType): string {
    return type === TransactionType.Income ? '+' : '-';
  }
}