import { Pipe, PipeTransform } from '@angular/core';
import { TransactionType } from '../../core/models/transaction.model';

@Pipe({
  name: 'transactionType',
  standalone: true
})
export class TransactionTypePipe implements PipeTransform {
  transform(value: TransactionType): string {
    return value === TransactionType.Income ? 'Income' : 'Expense';
  }
}