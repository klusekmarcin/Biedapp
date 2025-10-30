import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CategorySummary } from '../models/category-summary.model';

@Injectable({
  providedIn: 'root'
})
export class StatisticsApiService {
  private readonly baseUrl = `${environment.apiUrl}/statistics`;

  constructor(private http: HttpClient) {}

  getBalanceTrend(): Observable<{ [key: string]: number }> {
    return this.http.get<{ [key: string]: number }>(`${this.baseUrl}/trends/balance`);
  }

  getTopExpenses(top: number = 5): Observable<CategorySummary[]> {
    const params = new HttpParams().set('top', top.toString());
    return this.http.get<CategorySummary[]>(`${this.baseUrl}/top-expenses`, { params });
  }

  getAverageExpenses(months: number = 6): Observable<any> {
    const params = new HttpParams().set('months', months.toString());
    return this.http.get(`${this.baseUrl}/average-expenses`, { params });
  }
}