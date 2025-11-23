export enum TransactionType {
    Income = "Income",
    Expense = "Expense",
  }
  
  export interface Transaction {
    id: string;
    amount: number;
    currency: string;
    category: string;
    description: string;
    type: TransactionType;
    transactionDate: Date;
    typeDisplay?: string;
    amountDisplay?: string;
  }
  
  export interface CreateTransactionRequest {
    amount: number;
    currency: string;
    category: string;
    description: string;
    type: TransactionType;
    transactionDate: Date;
  }
  
  export interface UpdateTransactionRequest {
    id: string;
    amount: number;
    currency: string;
    category: string;
    description: string;
    type: TransactionType;
    transactionDate: Date;
  }