import { Pipe, PipeTransform } from '@angular/core';
import { defaultCurrencyCode } from '../clientapp.constants';

@Pipe({
  name: 'currencyFormat',
  standalone: true
})
export class CurrencyFormatPipe implements PipeTransform {
  transform(value: number, currency: string = defaultCurrencyCode): string {
    if (value === null || value === undefined) {
      return `0.00 ${currency}`;
    }
    return `${value.toFixed(2)} ${currency}`;
  }
}