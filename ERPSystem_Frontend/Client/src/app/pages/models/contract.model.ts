export interface CreateDiscountDto {
  type: 1 | 2;
  value: number;
  reason: string;
  scope: 'Total' | 'Subscription' | 'Package'; // 🔥 ADD
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

