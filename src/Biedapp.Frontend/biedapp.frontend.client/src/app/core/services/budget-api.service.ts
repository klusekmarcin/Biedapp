import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Transaction,
  CreateTransactionRequest,
  UpdateTransactionRequest,
  TransactionType
} from '../models/transaction.model';
import { BudgetSummary } from '../models/budget-summary.model';
import { CategorySummary } from '../models/category-summary.model';

@Injectable({
  providedIn: 'root'
})
export class BudgetApiService {
  private readonly baseUrl = `${environment.apiUrl}/budget`;

  constructor(private http: HttpClient) {}

  getTransactions(
    fromDate?: Date,
    toDate?: Date,
    category?: string,
    type?: TransactionType,
    limit?: number
  ): Observable<Transaction[]> {
    let params = new HttpParams();

    if (fromDate) {
      params = params.set('fromDate', fromDate.toISOString());
    }
    if (toDate) {
      params = params.set('toDate', toDate.toISOString());
    }
    if (category) {
      params = params.set('category', category);
    }
    if (type !== undefined) {
      params = params.set('type', type.toString());
    }
    if (limit) {
      params = params.set('limit', limit.toString());
    }

    return this.http.get<Transaction[]>(`${this.baseUrl}/transactions`, { params });
  }

  getTransactionById(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.baseUrl}/transactions/${id}`);
  }

  addTransaction(request: CreateTransactionRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/transactions`, request);
  }

  updateTransaction(id: string, request: UpdateTransactionRequest): Observable<any> {
    return this.http.put(`${this.baseUrl}/transactions/${id}`, request);
  }

  deleteTransaction(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/transactions/${id}`);
  }

  getBudgetSummary(): Observable<BudgetSummary> {
    return this.http.get<BudgetSummary>(`${this.baseUrl}/summary`);
  }

  getCategorySummary(type?: TransactionType): Observable<CategorySummary[]> {
    let params = new HttpParams();
    if (type !== undefined) {
      params = params.set('type', type.toString());
    }
    return this.http.get<CategorySummary[]>(`${this.baseUrl}/categories/summary`, { params });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  getMonthlyData(year: number, month: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/monthly/${year}/${month}`);
  }

  getCurrentMonthSummary(): Observable<BudgetSummary> {
    return this.http.get<BudgetSummary>(`${this.baseUrl}/summary/current-month`);
  }
  
  getMonthlySummary(year: number, month: number): Observable<BudgetSummary> {
    return this.http.get<BudgetSummary>(`${this.baseUrl}/summary/${year}/${month}`);
  }
  
  exportData(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export`, { responseType: 'blob' });
  }
  
  clearAllData(): Observable<any> {
    return this.http.delete(`${this.baseUrl}/clear-all`);
  }
}