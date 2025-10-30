export interface CategorySummary {
    category: string;
    totalAmount: number;
    transactionCount: number;
    currency: string;
    percentage: number;
    totalAmountDisplay?: string;
    percentageDisplay?: string;
  }