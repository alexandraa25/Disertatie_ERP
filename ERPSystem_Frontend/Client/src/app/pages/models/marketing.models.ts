export interface MarketingCampaign {
  id: number;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  discountType: number;
  discountValue: number;
  discountScope: number;
  course?: { name: string };
  courseSession?: { name: string };
}

export interface PublicResponse<T> {
  isSuccess: boolean;
  value: T;
  error?: {
    errorCode: string;
    errorMessage: string;
  };
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}