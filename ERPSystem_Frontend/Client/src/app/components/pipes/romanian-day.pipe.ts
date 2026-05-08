import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'roDay',
  standalone: true 
})
export class RomanianDayPipe implements PipeTransform {

  transform(day: any): string {
    const mapByNumber: Record<number, string> = {
      1: 'Luni',
      2: 'Marți',
      3: 'Miercuri',
      4: 'Joi',
      5: 'Vineri',
      6: 'Sâmbătă', 
      7: 'Duminică',
    };

    if (!isNaN(day)) {
      return mapByNumber[Number(day)] ?? day;
    }

    const normalized = day?.toString().toLowerCase();

    const mapByName: Record<string, string> = {
      monday: 'Luni',
      tuesday: 'Marți',
      wednesday: 'Miercuri',
      thursday: 'Joi',
      friday: 'Vineri',
      saturday: 'Sâmbătă',
      sunday: 'Duminică'
    };

    return mapByName[normalized] ?? day;
  }
}