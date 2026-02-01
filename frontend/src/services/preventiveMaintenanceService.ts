import api from './api';
import {
  ApiResponse,
  CreatePreventiveMaintenanceScheduleRequest,
  GenerateWorkOrdersResult,
  PagedResponse,
  PreventiveMaintenanceSchedule,
  PreventiveMaintenanceScheduleFilter,
  PreventiveMaintenanceScheduleSummary,
  UpdatePreventiveMaintenanceScheduleRequest,
  UpcomingMaintenance,
  WorkOrder,
} from '../types';

export const preventiveMaintenanceService = {
  // CRUD operations
  getSchedules: async (
    filter: PreventiveMaintenanceScheduleFilter = {}
  ): Promise<PagedResponse<PreventiveMaintenanceScheduleSummary>> => {
    const params = new URLSearchParams();

    if (filter.search) params.append('search', filter.search);
    if (filter.assetId) params.append('assetId', filter.assetId.toString());
    if (filter.frequencyType) params.append('frequencyType', filter.frequencyType);
    if (filter.isActive !== undefined) params.append('isActive', filter.isActive.toString());
    if (filter.dueBefore) params.append('dueBefore', filter.dueBefore);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params.append('sortDescending', filter.sortDescending.toString());

    const response = await api.get<PagedResponse<PreventiveMaintenanceScheduleSummary>>(
      `/preventive-maintenance?${params.toString()}`
    );
    return response.data;
  },

  getSchedule: async (id: number): Promise<ApiResponse<PreventiveMaintenanceSchedule>> => {
    const response = await api.get<ApiResponse<PreventiveMaintenanceSchedule>>(`/preventive-maintenance/${id}`);
    return response.data;
  },

  createSchedule: async (
    schedule: CreatePreventiveMaintenanceScheduleRequest
  ): Promise<ApiResponse<PreventiveMaintenanceSchedule>> => {
    const response = await api.post<ApiResponse<PreventiveMaintenanceSchedule>>('/preventive-maintenance', schedule);
    return response.data;
  },

  updateSchedule: async (
    id: number,
    schedule: UpdatePreventiveMaintenanceScheduleRequest
  ): Promise<ApiResponse<PreventiveMaintenanceSchedule>> => {
    const response = await api.put<ApiResponse<PreventiveMaintenanceSchedule>>(
      `/preventive-maintenance/${id}`,
      schedule
    );
    return response.data;
  },

  deleteSchedule: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/preventive-maintenance/${id}`);
    return response.data;
  },

  // Work order generation
  generateDueWorkOrders: async (): Promise<ApiResponse<GenerateWorkOrdersResult>> => {
    const response = await api.post<ApiResponse<GenerateWorkOrdersResult>>('/preventive-maintenance/generate');
    return response.data;
  },

  generateWorkOrderForSchedule: async (id: number): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/preventive-maintenance/${id}/generate`);
    return response.data;
  },

  // Upcoming maintenance
  getUpcomingMaintenance: async (daysAhead = 30): Promise<ApiResponse<UpcomingMaintenance[]>> => {
    const response = await api.get<ApiResponse<UpcomingMaintenance[]>>(
      `/preventive-maintenance/upcoming?daysAhead=${daysAhead}`
    );
    return response.data;
  },
};
