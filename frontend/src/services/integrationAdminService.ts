import api from './api';
import {
  ApiResponse,
  PagedResponse,
  IntegrationSettings,
  MicrosoftGraphSettings,
  UpdateMicrosoftGraphSettingsRequest,
  TestEmailRequest,
  TestCalendarEventRequest,
  NotificationQueueItem,
  NotificationLogItem,
  NotificationStats,
  NotificationQueueFilter,
  NotificationLogFilter,
} from '../types';

export const integrationAdminService = {
  // Integration settings
  getIntegrations: async (): Promise<ApiResponse<IntegrationSettings[]>> => {
    const response = await api.get<ApiResponse<IntegrationSettings[]>>('/admin/integrations');
    return response.data;
  },

  getMicrosoftGraphSettings: async (): Promise<ApiResponse<MicrosoftGraphSettings>> => {
    const response = await api.get<ApiResponse<MicrosoftGraphSettings>>('/admin/integrations/microsoft-graph');
    return response.data;
  },

  updateMicrosoftGraphSettings: async (request: UpdateMicrosoftGraphSettingsRequest): Promise<ApiResponse<void>> => {
    const response = await api.put<ApiResponse<void>>('/admin/integrations/microsoft-graph', request);
    return response.data;
  },

  validateMicrosoftGraphSettings: async (): Promise<ApiResponse<boolean>> => {
    const response = await api.post<ApiResponse<boolean>>('/admin/integrations/microsoft-graph/validate');
    return response.data;
  },

  // Test functions
  sendTestEmail: async (request: TestEmailRequest): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>('/admin/integrations/test-email', request);
    return response.data;
  },

  createTestCalendarEvent: async (request: TestCalendarEventRequest): Promise<ApiResponse<string>> => {
    const response = await api.post<ApiResponse<string>>('/admin/integrations/test-calendar', request);
    return response.data;
  },

  sendTestTeamsNotification: async (): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>('/admin/integrations/test-teams');
    return response.data;
  },

  // Notification queue
  getNotificationQueue: async (filter: NotificationQueueFilter): Promise<ApiResponse<PagedResponse<NotificationQueueItem>>> => {
    const params = new URLSearchParams();
    if (filter.status) params.append('status', filter.status);
    if (filter.type) params.append('type', filter.type);
    if (filter.referenceType) params.append('referenceType', filter.referenceType);
    if (filter.referenceId) params.append('referenceId', filter.referenceId.toString());
    if (filter.from) params.append('from', filter.from);
    if (filter.to) params.append('to', filter.to);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const response = await api.get<ApiResponse<PagedResponse<NotificationQueueItem>>>(`/admin/integrations/queue?${params.toString()}`);
    return response.data;
  },

  retryNotification: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(`/admin/integrations/queue/${id}/retry`);
    return response.data;
  },

  deleteNotification: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/admin/integrations/queue/${id}`);
    return response.data;
  },

  // Notification logs
  getNotificationLogs: async (filter: NotificationLogFilter): Promise<ApiResponse<PagedResponse<NotificationLogItem>>> => {
    const params = new URLSearchParams();
    if (filter.success !== undefined) params.append('success', filter.success.toString());
    if (filter.type) params.append('type', filter.type);
    if (filter.channel) params.append('channel', filter.channel);
    if (filter.from) params.append('from', filter.from);
    if (filter.to) params.append('to', filter.to);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const response = await api.get<ApiResponse<PagedResponse<NotificationLogItem>>>(`/admin/integrations/logs?${params.toString()}`);
    return response.data;
  },

  // Stats
  getNotificationStats: async (): Promise<ApiResponse<NotificationStats>> => {
    const response = await api.get<ApiResponse<NotificationStats>>('/admin/integrations/stats');
    return response.data;
  },
};
