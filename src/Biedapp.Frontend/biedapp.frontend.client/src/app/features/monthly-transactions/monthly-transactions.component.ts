import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BudgetApiService } from '../../core/services/budget-api.service';
import { Transaction, TransactionType, CreateTransactionRequest } from '../../core/models/transaction.model';
import { BudgetSummary } from '../../core/models/budget-summary.model';
import { CurrencyFormatPipe } from '../../shared/pipes/currency-format.pipe';
import { Title } from '@angular/platform-browser';

interface MonthlyGroup {
  year: number;
  month: number;
  monthName: string;
  transactions: Transaction[];
  summary: {
    income: number;
    expenses: number;
    balance: number;
  };
}

@Component({
  selector: 'app-monthly-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyFormatPipe],
  templateUrl: './monthly-transactions.component.html',
  styleUrls: ['./monthly-transactions.component.scss']
})
export class MonthlyTransactionsComponent implements OnInit {
  currentDate = new Date();
  currentYear = this.currentDate.getFullYear();
  currentMonth = this.currentDate.getMonth() + 1;
  
  monthlyGroups: MonthlyGroup[] = [];
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
      title.setTitle("Monthly transactions - Biedapp");
    }

  ngOnInit(): void {
    this.loadMonthlyTransactions();
    this.loadCategories();
  }

  loadMonthlyTransactions(): void {
    this.loading = true;
    const monthsToShow = 1;
    const promises: Promise<MonthlyGroup>[] = [];

    for (let i = 0; i < monthsToShow; i++) {
      const date = new Date(this.currentYear, this.currentMonth - 1 - i, 1);
      const year = date.getFullYear();
      const month = date.getMonth() + 1;

      const promise = this.loadMonthData(year, month);
      promises.push(promise);
    }

    Promise.all(promises).then(groups => {
      this.monthlyGroups = groups;
      this.loading = false;
    });
  }

  private async loadMonthData(year: number, month: number): Promise<MonthlyGroup> {
    const startDate = new Date(year, month - 1, 1);
    const endDate = new Date(year, month, 0);

    const transactions = await this.budgetApi.getTransactions(startDate, endDate).toPromise() || [];
    
    const income = transactions
      .filter(t => t.type === TransactionType.Income)
      .reduce((sum, t) => sum + t.amount, 0);
    
    const expenses = transactions
      .filter(t => t.type === TransactionType.Expense)
      .reduce((sum, t) => sum + t.amount, 0);

    return {
      year,
      month,
      monthName: startDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' }),
      transactions,
      summary: {
        income,
        expenses,
        balance: income - expenses
      }
    };
  }

  loadCategories(): void {
    this.budgetApi.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: (err) => console.error('Error loading categories:', err)
    });
  }

  previousMonth(): void {
    if (this.currentMonth === 1) {
      this.currentMonth = 12;
      this.currentYear--;
    } else {
      this.currentMonth--;
    }
    this.loadMonthlyTransactions();
  }

  nextMonth(): void {
    const now = new Date();
    const canGoNext = this.currentYear < now.getFullYear() || 
                     (this.currentYear === now.getFullYear() && this.currentMonth < now.getMonth() + 1);
    
    if (canGoNext) {
      if (this.currentMonth === 12) {
        this.currentMonth = 1;
        this.currentYear++;
      } else {
        this.currentMonth++;
      }
      this.loadMonthlyTransactions();
    }
  }

  isCurrentMonth(): boolean {
    const now = new Date();
    return this.currentYear === now.getFullYear() && this.currentMonth === now.getMonth() + 1;
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
          this.loadMonthlyTransactions();
          this.closeForm();
        },
        error: (err) => console.error('Error updating transaction:', err)
      });
    } else {
      this.budgetApi.addTransaction(this.formData).subscribe({
        next: () => {
          this.loadMonthlyTransactions();
          this.closeForm();
        },
        error: (err) => console.error('Error adding transaction:', err)
      });
    }
  }

  deleteTransaction(id: string): void {
    if (confirm('Are you sure you want to delete this transaction?')) {
      this.budgetApi.deleteTransaction(id).subscribe({
        next: () => this.loadMonthlyTransactions(),
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