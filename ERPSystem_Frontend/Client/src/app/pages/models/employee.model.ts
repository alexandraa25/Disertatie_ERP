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