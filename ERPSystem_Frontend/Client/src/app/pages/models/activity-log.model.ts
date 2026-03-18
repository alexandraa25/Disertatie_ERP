export interface ActivityLog {
  entityType: string;
  entityId: number;
  action: string;
  createdAtUtc: string;
  description?: string;
  oldValues: Record<string, any>;
  newValues: Record<string, any>;
}

export interface ActivityFilterOptions {
  entities: string[];
  actions: string[];
  users: string[];
}

export interface ActivityFilters {
  entity: string;
  action: string;
  performedBy: string;
  from: string;
  to: string;
  page: number;
  pageSize: number;
}