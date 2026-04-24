import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { ActivityFilterOptions, ActivityFilters, ActivityLog } from "../models/activity-log.model";
import { HttpParams } from '@angular/common/http';


@Injectable({
  providedIn: 'root'
})
export class ActivityLogService {

    private apiUrl = 'https://localhost:7195';

  constructor(private http: HttpClient) {}

  getActivity(entity: string, id: string) {
  return this.http.get<ActivityLog[]>(
    `${this.apiUrl}/activity?entity=${entity}&id=${id}`
  );
}

  
getFilters() {
  return this.http.get<ActivityFilterOptions>(
    `${this.apiUrl}/fillter`
  );
}

getAllActivity(filters: any) {
  let params = new HttpParams()
    .set('page', filters.page)
    .set('pageSize', filters.pageSize);

  // 🔥 entity list
  filters.entity?.forEach((e: string) => {
    params = params.append('entity', e);
  });

  // 🔥 action list
  filters.action?.forEach((a: string) => {
    params = params.append('action', a);
  });

  // 🔥 users list
  filters.performedBy?.forEach((u: string) => {
    params = params.append('performedBy', u);
  });

  if (filters.from) {
    const from = new Date(filters.from);
    from.setHours(0,0,0,0);
    params = params.set('from', from.toISOString());
  }

  if (filters.to) {
    const to = new Date(filters.to);
    to.setHours(23,59,59,999);
    params = params.set('to', to.toISOString());
  }

  return this.http.get<any>(
    `${this.apiUrl}/activity/all`,
    { params }
  );
}

}