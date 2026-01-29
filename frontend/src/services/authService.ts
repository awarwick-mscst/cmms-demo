import api from './api';
import { ApiResponse, LoginRequest, LoginResponse, User } from '../types';

export const authService = {
  login: async (credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> => {
    const response = await api.post<ApiResponse<LoginResponse>>('/auth/login', credentials);
    return response.data;
  },

  logout: async (refreshToken: string): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>('/auth/logout', { refreshToken });
    return response.data;
  },

  refreshToken: async (refreshToken: string): Promise<ApiResponse<LoginResponse>> => {
    const response = await api.post<ApiResponse<LoginResponse>>('/auth/refresh', { refreshToken });
    return response.data;
  },

  getCurrentUser: async (): Promise<ApiResponse<User>> => {
    const response = await api.get<ApiResponse<User>>('/auth/me');
    return response.data;
  },

  changePassword: async (currentPassword: string, newPassword: string): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>('/auth/change-password', {
      currentPassword,
      newPassword,
    });
    return response.data;
  },
};
