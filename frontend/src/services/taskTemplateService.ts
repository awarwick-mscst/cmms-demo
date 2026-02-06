import api from './api';
import {
  ApiResponse,
  CreateWorkOrderTaskTemplateRequest,
  PagedResponse,
  TaskTemplateDropdown,
  TaskTemplateFilter,
  UpdateWorkOrderTaskTemplateRequest,
  WorkOrderTaskTemplate,
  WorkOrderTaskTemplateSummary,
} from '../types';

export const taskTemplateService = {
  // Get paginated list of templates
  getTemplates: async (
    filter: TaskTemplateFilter = {}
  ): Promise<PagedResponse<WorkOrderTaskTemplateSummary>> => {
    const params = new URLSearchParams();

    if (filter.search) params.append('search', filter.search);
    if (filter.isActive !== undefined) params.append('isActive', filter.isActive.toString());
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined)
      params.append('sortDescending', filter.sortDescending.toString());

    const response = await api.get<PagedResponse<WorkOrderTaskTemplateSummary>>(
      `/work-order-task-templates?${params.toString()}`
    );
    return response.data;
  },

  // Get active templates for dropdowns
  getActiveTemplates: async (): Promise<ApiResponse<TaskTemplateDropdown[]>> => {
    const response = await api.get<ApiResponse<TaskTemplateDropdown[]>>(
      '/work-order-task-templates/active'
    );
    return response.data;
  },

  // Get a single template with items
  getTemplate: async (id: number): Promise<ApiResponse<WorkOrderTaskTemplate>> => {
    const response = await api.get<ApiResponse<WorkOrderTaskTemplate>>(
      `/work-order-task-templates/${id}`
    );
    return response.data;
  },

  // Create a new template
  createTemplate: async (
    request: CreateWorkOrderTaskTemplateRequest
  ): Promise<ApiResponse<WorkOrderTaskTemplate>> => {
    const response = await api.post<ApiResponse<WorkOrderTaskTemplate>>(
      '/work-order-task-templates',
      request
    );
    return response.data;
  },

  // Update an existing template
  updateTemplate: async (
    id: number,
    request: UpdateWorkOrderTaskTemplateRequest
  ): Promise<ApiResponse<WorkOrderTaskTemplate>> => {
    const response = await api.put<ApiResponse<WorkOrderTaskTemplate>>(
      `/work-order-task-templates/${id}`,
      request
    );
    return response.data;
  },

  // Delete a template
  deleteTemplate: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/work-order-task-templates/${id}`);
    return response.data;
  },
};
