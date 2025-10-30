export interface BudgetSummary {
    totalIncome: number;
    totalExpenses: number;
    balance: number;
    currency: string;
    transactionCount: number;
    incomeCount: number;
    expenseCount: number;
    totalIncomeDisplay?: string;
    totalExpensesDisplay?: string;
    balanceDisplay?: string;
    balanceStatus?: string;
  }