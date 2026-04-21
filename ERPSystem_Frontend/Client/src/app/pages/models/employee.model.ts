export interface Employee {

  id: string

  userId?: string

  firstName?: string
  lastName?: string

  jobTitle: string

  hireDate: string

  terminationDate?: string

  employmentStatus?: string

  salary?: number

  contractType?: string
}

export interface HrDashboard {

  totalEmployees: number

  activeEmployees: number

  terminatedEmployees: number

  newHiresThisMonth: number
}

export interface SimpleUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  totalPages: number;
  page: number;
  pageSize: number;
}