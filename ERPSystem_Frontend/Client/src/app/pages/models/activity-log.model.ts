export interface ActivityLog {
  entityType: string;
  entityId: number;
  action: string;
  createdAtUtc: string;
  description?: string;
  performedBy?: string;
  performedByName?: string;
  oldValues: Record<string, any>;
  newValues: Record<string, any>;
}

export interface UserOption {
  email: string;
  fullName: string;
}

export interface ActivityFilterOptions {
  entities: string[];
  actions: string[];
  users: UserOption[];
}

export interface ActivityFilters {
  entity: string[];
  action: string[];
  performedBy: string[];
  from: Date | null;
  to: Date | null;
  page: number;
  pageSize: number;
}