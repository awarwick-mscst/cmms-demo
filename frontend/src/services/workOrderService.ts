import api from './api';
import {
  AddSessionNoteRequest,
  ApiResponse,
  CompleteWorkOrderRequest,
  CreateWorkOrderCommentRequest,
  CreateWorkOrderLaborRequest,
  CreateWorkOrderRequest,
  PagedResponse,
  StopSessionRequest,
  UpdateWorkOrderRequest,
  WorkOrder,
  WorkOrderComment,
  WorkOrderDashboard,
  WorkOrderFilter,
  WorkOrderHistory,
  WorkOrderLabor,
  WorkOrderLaborSummary,
  WorkOrderPart,
  WorkOrderStatusChangeRequest,
  WorkOrderSummary,
  WorkSession,
} from '../types';

export const workOrderService = {
  // CRUD operations
  getWorkOrders: async (filter: WorkOrderFilter = {}): Promise<PagedResponse<WorkOrderSummary>> => {
    const params = new URLSearchParams();

    if (filter.search) params.append('search', filter.search);
    if (filter.type) params.append('type', filter.type);
    if (filter.status) params.append('status', filter.status);
    if (filter.priority) params.append('priority', filter.priority);
    if (filter.assetId) params.append('assetId', filter.assetId.toString());
    if (filter.locationId) params.append('locationId', filter.locationId.toString());
    if (filter.assignedToId) params.append('assignedToId', filter.assignedToId.toString());
    if (filter.scheduledStartFrom) params.append('scheduledStartFrom', filter.scheduledStartFrom);
    if (filter.scheduledStartTo) params.append('scheduledStartTo', filter.scheduledStartTo);
    if (filter.createdFrom) params.append('createdFrom', filter.createdFrom);
    if (filter.createdTo) params.append('createdTo', filter.createdTo);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params.append('sortDescending', filter.sortDescending.toString());

    const response = await api.get<PagedResponse<WorkOrderSummary>>(`/work-orders?${params.toString()}`);
    return response.data;
  },

  getWorkOrder: async (id: number): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.get<ApiResponse<WorkOrder>>(`/work-orders/${id}`);
    return response.data;
  },

  createWorkOrder: async (workOrder: CreateWorkOrderRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>('/work-orders', workOrder);
    return response.data;
  },

  updateWorkOrder: async (id: number, workOrder: UpdateWorkOrderRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.put<ApiResponse<WorkOrder>>(`/work-orders/${id}`, workOrder);
    return response.data;
  },

  deleteWorkOrder: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/work-orders/${id}`);
    return response.data;
  },

  // Status transitions
  submitWorkOrder: async (id: number, request?: WorkOrderStatusChangeRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/submit`, request || {});
    return response.data;
  },

  startWorkOrder: async (id: number, request?: WorkOrderStatusChangeRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/start`, request || {});
    return response.data;
  },

  completeWorkOrder: async (id: number, request?: CompleteWorkOrderRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/complete`, request || {});
    return response.data;
  },

  holdWorkOrder: async (id: number, request?: WorkOrderStatusChangeRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/hold`, request || {});
    return response.data;
  },

  resumeWorkOrder: async (id: number, request?: WorkOrderStatusChangeRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/resume`, request || {});
    return response.data;
  },

  cancelWorkOrder: async (id: number, request?: WorkOrderStatusChangeRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/cancel`, request || {});
    return response.data;
  },

  reopenWorkOrder: async (id: number, request?: WorkOrderStatusChangeRequest): Promise<ApiResponse<WorkOrder>> => {
    const response = await api.post<ApiResponse<WorkOrder>>(`/work-orders/${id}/reopen`, request || {});
    return response.data;
  },

  // History
  getWorkOrderHistory: async (id: number): Promise<ApiResponse<WorkOrderHistory[]>> => {
    const response = await api.get<ApiResponse<WorkOrderHistory[]>>(`/work-orders/${id}/history`);
    return response.data;
  },

  // Comments
  getWorkOrderComments: async (id: number, includeInternal = false): Promise<ApiResponse<WorkOrderComment[]>> => {
    const response = await api.get<ApiResponse<WorkOrderComment[]>>(
      `/work-orders/${id}/comments?includeInternal=${includeInternal}`
    );
    return response.data;
  },

  addComment: async (id: number, request: CreateWorkOrderCommentRequest): Promise<ApiResponse<WorkOrderComment>> => {
    const response = await api.post<ApiResponse<WorkOrderComment>>(`/work-orders/${id}/comments`, request);
    return response.data;
  },

  // Labor
  getWorkOrderLabor: async (id: number): Promise<ApiResponse<WorkOrderLabor[]>> => {
    const response = await api.get<ApiResponse<WorkOrderLabor[]>>(`/work-orders/${id}/labor`);
    return response.data;
  },

  addLaborEntry: async (id: number, request: CreateWorkOrderLaborRequest): Promise<ApiResponse<WorkOrderLabor>> => {
    const response = await api.post<ApiResponse<WorkOrderLabor>>(`/work-orders/${id}/labor`, request);
    return response.data;
  },

  deleteLaborEntry: async (workOrderId: number, laborId: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/work-orders/${workOrderId}/labor/${laborId}`);
    return response.data;
  },

  getLaborSummary: async (id: number): Promise<ApiResponse<WorkOrderLaborSummary>> => {
    const response = await api.get<ApiResponse<WorkOrderLaborSummary>>(`/work-orders/${id}/labor/summary`);
    return response.data;
  },

  // Parts
  getWorkOrderParts: async (id: number): Promise<ApiResponse<WorkOrderPart[]>> => {
    const response = await api.get<ApiResponse<WorkOrderPart[]>>(`/work-orders/${id}/parts`);
    return response.data;
  },

  // Dashboard
  getDashboard: async (): Promise<ApiResponse<WorkOrderDashboard>> => {
    const response = await api.get<ApiResponse<WorkOrderDashboard>>('/work-orders/dashboard');
    return response.data;
  },

  // Work Sessions (Active Time Tracking)
  startSession: async (workOrderId: number): Promise<ApiResponse<WorkSession>> => {
    const response = await api.post<ApiResponse<WorkSession>>(`/work-orders/${workOrderId}/start-session`);
    return response.data;
  },

  stopSession: async (workOrderId: number, request?: StopSessionRequest): Promise<ApiResponse<WorkSession>> => {
    const response = await api.post<ApiResponse<WorkSession>>(`/work-orders/${workOrderId}/stop-session`, request || {});
    return response.data;
  },

  getActiveSession: async (workOrderId: number): Promise<ApiResponse<WorkSession | null>> => {
    const response = await api.get<ApiResponse<WorkSession | null>>(`/work-orders/${workOrderId}/active-session`);
    return response.data;
  },

  getMyActiveSession: async (): Promise<ApiResponse<WorkSession | null>> => {
    const response = await api.get<ApiResponse<WorkSession | null>>('/work-orders/my-active-session');
    return response.data;
  },

  addSessionNote: async (workOrderId: number, request: AddSessionNoteRequest): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(`/work-orders/${workOrderId}/session-note`, request);
    return response.data;
  },

  // Get all active sessions (for availability widget)
  getAllActiveSessions: async (): Promise<ApiResponse<WorkSession[]>> => {
    const response = await api.get<ApiResponse<WorkSession[]>>('/work-orders/active-sessions');
    return response.data;
  },
};
