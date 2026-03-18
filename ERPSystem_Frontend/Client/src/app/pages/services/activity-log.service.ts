import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { ActivityFilterOptions, ActivityFilters, ActivityLog } from "../models/activity-log.model";


@Injectable({
  providedIn: 'root'
})
export class ActivityLogService {

    private apiUrl = 'https://localhost:7195';

  constructor(private http: HttpClient) {}

  getActivity(entity: string, id: number) {
    return this.http.get<ActivityLog[]>(
      `${this.apiUrl}/activity?entity=${entity}&id=${id}`
    );
  }

getFilters() {
  return this.http.get<ActivityFilterOptions>(
    `${this.apiUrl}/fillter`
  );
}

getAllActivity(filters: ActivityFilters) {
  const params: any = { ...filters };

 if (params.from) {
  params.from = new Date(params.from).toISOString();
} else {
  delete params.from;
}

if (params.to) {
  params.to = new Date(params.to).toISOString();
} else {
  delete params.to;
}

  return this.http.get<any>(
   `${this.apiUrl}/activity/all`,
    { params }
  );
}


}