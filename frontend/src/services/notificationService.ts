import api from './api';
import {
  ApiResponse,
  UserNotificationPreference,
  UpdateNotificationPreferenceRequest,
  BulkUpdateNotificationPreferencesRequest,
  NotificationTypeInfo,
} from '../types';

export const notificationService = {
  // User preferences
  getMyPreferences: async (): Promise<ApiResponse<UserNotificationPreference[]>> => {
    const response = await api.get<ApiResponse<UserNotificationPreference[]>>('/notifications/preferences');
    return response.data;
  },

  updatePreference: async (request: UpdateNotificationPreferenceRequest): Promise<ApiResponse<UserNotificationPreference>> => {
    const response = await api.put<ApiResponse<UserNotificationPreference>>('/notifications/preferences', request);
    return response.data;
  },

  updatePreferencesBulk: async (request: BulkUpdateNotificationPreferencesRequest): Promise<ApiResponse<UserNotificationPreference[]>> => {
    const response = await api.put<ApiResponse<UserNotificationPreference[]>>('/notifications/preferences/bulk', request);
    return response.data;
  },

  getNotificationTypes: async (): Promise<ApiResponse<NotificationTypeInfo[]>> => {
    const response = await api.get<ApiResponse<NotificationTypeInfo[]>>('/notifications/types');
    return response.data;
  },
};
