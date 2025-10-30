import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StatisticsApiService } from '../../core/services/statistics-api.service';
import { BudgetApiService } from '../../core/services/budget-api.service';
import { CategorySummary } from '../../core/models/category-summary.model';
import { TransactionType } from '../../core/models/transaction.model';
import { CurrencyFormatPipe } from '../../shared/pipes/currency-format.pipe';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule, CurrencyFormatPipe],
  templateUrl: './statistics.component.html',
  styleUrls: ['./statistics.component.scss']
})
export class StatisticsComponent implements OnInit {
  loading = true;
  balanceTrend: { month: string; balance: number }[] = [];
  topExpenses: CategorySummary[] = [];
  expensesByCategory: CategorySummary[] = [];
  incomeByCategory: CategorySummary[] = [];
  averageExpenses = 0;
  selectedPeriod = 6;
  Math = Math;
  selectedMonthsPeriod = 3;
  currentMonthOffset = 0;

  constructor(
    private statisticsApi: StatisticsApiService,
    private budgetApi: BudgetApiService,
    title: Title)
  {
    title.setTitle("Statistics & Analytics - Biedapp");
  }

  ngOnInit(): void {
    this.loadStatistics();
  }

  loadStatistics(): void {
    this.loading = true;

    this.statisticsApi.getBalanceTrend().subscribe({
      next: (data) => {
        this.balanceTrend = Object.entries(data).map(([month, balance]) => ({
          month,
          balance
        }));
      },
      error: (err) => console.error('Error loading balance trend:', err)
    });

    this.statisticsApi.getTopExpenses(5).subscribe({
      next: (data) => {
        this.topExpenses = data;
      },
      error: (err) => console.error('Error loading top expenses:', err)
    });

    this.budgetApi.getCategorySummary(TransactionType.Expense).subscribe({
      next: (data) => {
        this.expensesByCategory = data;
      },
      error: (err) => console.error('Error loading expense categories:', err)
    });

    this.budgetApi.getCategorySummary(TransactionType.Income).subscribe({
      next: (data) => {
        this.incomeByCategory = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading income categories:', err);
        this.loading = false;
      }
    });

    this.loadAverageExpenses();
  }

  loadAverageExpenses(): void {
    this.statisticsApi.getAverageExpenses(this.selectedPeriod).subscribe({
      next: (data) => {
        this.averageExpenses = data.averageMonthlyExpenses;
      },
      error: (err) => console.error('Error loading average expenses:', err)
    });
  }

  onPeriodChange(months: number): void {
    this.selectedPeriod = months;
    this.loadAverageExpenses();
  }

  getProgressWidth(percentage: number): string {
    return `${Math.min(percentage, 100)}%`;
  }
  
  getMaxBalance(): number {
    const balances = this.balanceTrend.map(item => Math.abs(item.balance));
    return Math.max(...balances, 1);
  }

  onMonthsPeriodChange(months: number): void {
    this.selectedMonthsPeriod = months;
    this.loadStatistics();
  }
}