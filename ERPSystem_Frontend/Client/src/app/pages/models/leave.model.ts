export interface Leave {
  id: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  days: number;
}

export interface LeavesResponse {
  leaves: Leave[];
  vacation: {
    total: number;
    used: number;
    remaining: number;
    carryOver: number;
  };
  medical: {
    used: number;
  };
}

export interface CreateLeaveDto {
  startDate: string;
  endDate: string;
  leaveType: string;
}