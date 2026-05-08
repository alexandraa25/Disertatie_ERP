export interface CreateDiscountDto {
  type: 1 | 2;
  value: number;
  reason: string;
  scope: 'Total' | 'Subscription' | 'Package';
}

export interface CreateContractDto {
  guardianId: number | null; 
  studentId: number;
  startDate: string;
  endDate?: string | null;   
  isUnlimited: boolean;
  installments: number;
  courseSessionIds: number[];
  discounts?: CreateDiscountDto[];
}

