export enum AdditionalActType {
  AddCourse = 0,
  RemoveCourse = 1,
  ExtendPeriod = 2,
  ChangePrice = 3
}

export interface CreateAdditionalActDto {
  types: AdditionalActType[];
  courseSessionIds?: number[];
  newEndDate?: string; 
  newPrice?: number;
}

export interface AdditionalActDetailsDto {
  id: number;
  actNumber: string;
  status: string;
  description: string;
  body: string;
  createdAtUtc: string;
  contractId: number;
  items: AdditionalActItemDto[];
}

export interface AdditionalActItemDto {
  type: string;
  courseSessionId?: number;
  newValue?: string;
}

export interface UpdateAdditionalActBodyDto {
  body: string;
}

export interface AdditionalActListDto {
  id: number;
  actNumber: string;
  status: string;
  description: string;
  createdAtUtc: string;
}