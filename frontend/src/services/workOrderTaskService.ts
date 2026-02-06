import api from './api';
import {
  ApiResponse,
  ApplyTemplateRequest,
  CompleteTaskRequest,
  CreateWorkOrderTaskRequest,
  ReorderTasksRequest,
  UpdateWorkOrderTaskRequest,
  WorkOrderTask,
  WorkOrderTaskSummary,
} from '../types';

export const workOrderTaskService = {
  // Get all tasks for a work order
  getTasks: async (workOrderId: number): Promise<ApiResponse<WorkOrderTask[]>> => {
    const response = await api.get<ApiResponse<WorkOrderTask[]>>(
      `/work-orders/${workOrderId}/tasks`
    );
    return response.data;
  },

  // Get task completion summary
  getTaskSummary: async (workOrderId: number): Promise<ApiResponse<WorkOrderTaskSummary>> => {
    const response = await api.get<ApiResponse<WorkOrderTaskSummary>>(
      `/work-orders/${workOrderId}/tasks/summary`
    );
    return response.data;
  },

  // Create a new task
  createTask: async (
    workOrderId: number,
    request: CreateWorkOrderTaskRequest
  ): Promise<ApiResponse<WorkOrderTask>> => {
    const response = await api.post<ApiResponse<WorkOrderTask>>(
      `/work-orders/${workOrderId}/tasks`,
      request
    );
    return response.data;
  },

  // Update a task
  updateTask: async (
    workOrderId: number,
    taskId: number,
    request: UpdateWorkOrderTaskRequest
  ): Promise<ApiResponse<WorkOrderTask>> => {
    const response = await api.put<ApiResponse<WorkOrderTask>>(
      `/work-orders/${workOrderId}/tasks/${taskId}`,
      request
    );
    return response.data;
  },

  // Delete a task
  deleteTask: async (workOrderId: number, taskId: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(
      `/work-orders/${workOrderId}/tasks/${taskId}`
    );
    return response.data;
  },

  // Toggle task completion
  toggleTaskCompletion: async (
    workOrderId: number,
    taskId: number,
    request?: CompleteTaskRequest
  ): Promise<ApiResponse<WorkOrderTask>> => {
    const response = await api.post<ApiResponse<WorkOrderTask>>(
      `/work-orders/${workOrderId}/tasks/${taskId}/complete`,
      request || {}
    );
    return response.data;
  },

  // Reorder tasks
  reorderTasks: async (
    workOrderId: number,
    request: ReorderTasksRequest
  ): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(
      `/work-orders/${workOrderId}/tasks/reorder`,
      request
    );
    return response.data;
  },

  // Apply a template to work order
  applyTemplate: async (
    workOrderId: number,
    request: ApplyTemplateRequest
  ): Promise<ApiResponse<WorkOrderTask[]>> => {
    const response = await api.post<ApiResponse<WorkOrderTask[]>>(
      `/work-orders/${workOrderId}/tasks/apply-template`,
      request
    );
    return response.data;
  },
};
