export interface CreateDiscountDto { 
  type: 'Percentage' | 'FixedAmount';
  value: number;
  reason: string;
}

export interface CreateContractDto {
  guardianId: number | null; // 🔥 important
  studentIds: number[];
  startDate: string;
  endDate?: string | null;   // 🔥 mai sigur
  isUnlimited: boolean;
  installments: number;
  courseSessionIds: number[];
  discounts?: CreateDiscountDto[];
}

