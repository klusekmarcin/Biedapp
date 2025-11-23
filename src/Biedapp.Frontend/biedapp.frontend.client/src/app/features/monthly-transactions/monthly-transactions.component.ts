import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BudgetApiService } from '../../core/services/budget-api.service';
import { Transaction, TransactionType, CreateTransactionRequest, UpdateTransactionRequest } from '../../core/models/transaction.model';
import { BudgetSummary } from '../../core/models/budget-summary.model';
import { CurrencyFormatPipe } from '../../shared/pipes/currency-format.pipe';
import { Title } from '@angular/platform-browser';
import { ModalService, ModalTemplate } from '../../core/services/modal.service';

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
    transactionDate: new Date()
  };

  constructor(
    private budgetApi: BudgetApiService,
    private modalService: ModalService,
    title: Title) {
      title.setTitle("Monthly transactions - Biedapp");
    }

  ngOnInit(): void {
    this.loadMonthlyTransactions();
    this.loadCategories();
  }

  loadMonthlyTransactions(): void {
    this.loading = true;

    const year = this.currentYear;
    const month = this.currentMonth;

    this.loadMonthData(year, month).then(budget => {
      this.monthlyGroups = [budget];
      this.loading = false;
    });
  }

  private async loadMonthData(year: number, month: number): Promise<MonthlyGroup> {
    var startDate = new Date(year, month - 1, 2);
    var endDate = new Date(year, month, 1);

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
      monthName: startDate.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' }),
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

  openCreateTransactionModal(): void {
    this.modalService.open({
      templateName: ModalTemplate.TRANSACTION_CREATE,
      title: 'Create Transaction',
      submitButtonConfig: {
        show: true,
        customText: 'Create'
      },
      resetButtonConfig: {
        show: true,
      },
    }).subscribe(result => {
      if(result.action === 'submit') {
        this.createTransaction(result.data);
      }
    });
  }

  openEditTransactionModal(transaction: Transaction): void {
    this.modalService.open({
      templateName: ModalTemplate.TRANSACTION_EDIT,
      title: 'Edit Transaction',
      submitButtonConfig: {
        show: true,
        customText: 'Update'
      },
      resetButtonConfig: {
        show: true,
      },
      data: transaction
    }).subscribe(result => {
      if(result.action === 'submit') {
        this.updateTransaction(result.data);
      }
    });
  }

  openDeleteTransactionModal(transaction: Transaction): void {
    this.modalService.open({
      templateName: ModalTemplate.TRANSACTION_DELETE,
      title: 'Delete Transaction',
      submitButtonConfig: {
        show: true,
        customText: 'Delete',
        customBackgroundColor: 'red',
        customColor: 'white'
      },
      data: transaction
    }).subscribe(result => {
      if(result.action === 'submit') {
        this.deleteTransaction(result.data);
      }
    });
    
  }

  createTransaction(createTransaction: CreateTransactionRequest): void {
    if(!createTransaction) return;

    this.budgetApi.addTransaction(createTransaction).subscribe({
      next: () => {
        this.loadMonthlyTransactions();
      },
      error: (err) => console.error('Error adding transaction:', err)
    });
  }

  updateTransaction(updateTransaction: UpdateTransactionRequest): void {
    if(!updateTransaction) return;

    this.budgetApi.updateTransaction(updateTransaction.id, updateTransaction).subscribe({
      next: () => {
        this.loadMonthlyTransactions();
      },
      error: (err) => console.error('Error updating transaction:', err)
    });
  }

  deleteTransaction(id: string): void {
    if (confirm('Are you sure you want to delete this transaction?')) {
      this.budgetApi.deleteTransaction(id).subscribe({
        next: () => this.loadMonthlyTransactions(),
        error: (err) => console.error('Error deleting transaction:', err)
      });
    }
  }

  getTypeClass(type: TransactionType): string {
    return type === TransactionType.Income ? 'income' : 'expense';
  }

  getTypeSign(type: TransactionType): string {
    return type === TransactionType.Income ? '+' : '-';
  }
}
