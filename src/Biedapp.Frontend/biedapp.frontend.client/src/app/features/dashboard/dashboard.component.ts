import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BudgetApiService } from '../../core/services/budget-api.service';
import { BudgetSummary } from '../../core/models/budget-summary.model';
import { Transaction, TransactionType, CreateTransactionRequest } from '../../core/models/transaction.model';
import { CategorySummary } from '../../core/models/category-summary.model';
import { CurrencyFormatPipe } from '../../shared/pipes/currency-format.pipe';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, CurrencyFormatPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  summary: BudgetSummary | null = null;
  monthlySummary: BudgetSummary | null = null;
  recentTransactions: Transaction[] = [];
  expensesByCategory: CategorySummary[] = [];
  categories: string[] = [];
  loading = true;
  error: string | null = null;
  showAddModal = false;
  
  TransactionType = TransactionType;

  formData: CreateTransactionRequest = {
    amount: 0,
    currency: 'PLN',
    category: '',
    description: '',
    type: TransactionType.Expense,
    date: new Date()
  };

  constructor(private budgetApi: BudgetApiService,
    title: Title
  ) {
    title.setTitle("Dashboard - Biedapp");
  }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    // Overall summary
    this.budgetApi.getBudgetSummary().subscribe({
      next: (summary) => this.summary = summary,
      error: (err) => {
        console.error('Error loading summary:', err);
        this.error = 'Failed to load budget summary';
        this.loading = false;
      }
    });

    // Current month summary
    this.budgetApi.getCurrentMonthSummary().subscribe({
      next: (summary) => this.monthlySummary = summary,
      error: (err) => console.error('Error loading monthly summary:', err)
    });

    // Recent transactions
    this.budgetApi.getTransactions(undefined, undefined, undefined, undefined, 5).subscribe({
      next: (transactions) => {
        this.recentTransactions = transactions;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading transactions:', err);
        this.error = 'Failed to load recent transactions';
        this.loading = false;
      }
    });

    // Expenses by category
    this.budgetApi.getCategorySummary(TransactionType.Expense).subscribe({
      next: (data) => this.expensesByCategory = data.slice(0, 5),
      error: (err) => console.error('Error loading expenses by category:', err)
    });

    // Categories for form
    this.budgetApi.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: (err) => console.error('Error loading categories:', err)
    });
  }

  openAddModal(): void {
    this.showAddModal = true;
    this.formData = {
      amount: 0,
      currency: 'PLN',
      category: '',
      description: '',
      type: TransactionType.Expense,
      date: new Date()
    };
  }

  closeAddModal(): void {
    this.showAddModal = false;
  }

  saveTransaction(): void {
    this.budgetApi.addTransaction(this.formData).subscribe({
      next: () => {
        this.closeAddModal();
        this.loadDashboardData();
      },
      error: (err) => console.error('Error adding transaction:', err)
    });
  }

  getTransactionClass(type: TransactionType): string {
    return type === TransactionType.Income ? 'income' : 'expense';
  }

  getTransactionSign(type: TransactionType): string {
    return type === TransactionType.Income ? '+' : '-';
  }

  // Calculate percentages
  get monthlyRevenueUsedPercentage(): number {
    if (!this.monthlySummary || this.monthlySummary.totalIncome === 0) return 0;
    return Math.min((this.monthlySummary.totalExpenses / this.monthlySummary.totalIncome) * 100, 100);
  }

  get monthlyRevenueLeft(): number {
    if (!this.monthlySummary) return 0;
    return Math.max(this.monthlySummary.totalIncome - this.monthlySummary.totalExpenses, 0);
  }

  get totalRevenueUsedPercentage(): number {
    if (!this.summary || this.summary.totalIncome === 0) return 0;
    return Math.min((this.summary.totalExpenses / this.summary.totalIncome) * 100, 100);
  }

  get monthlySavings(): number {
    if (!this.summary || !this.monthlySummary) return 0;
    const monthsWithData = this.summary.transactionCount > 0 ? 
      Math.max(this.getMonthsCount(), 1) : 1;
    return this.summary.balance / monthsWithData;
  }

  private getMonthsCount(): number {
    return 1;
  }

  getProgressWidth(percentage: number): string {
    return `${Math.min(percentage, 100)}%`;
  }
}