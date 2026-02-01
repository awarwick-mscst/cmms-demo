import api from './api';
import { ApiResponse } from '../types';

export interface UserSummary {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
}

export interface UserDetail extends UserSummary {
  phone?: string;
  isLocked: boolean;
  lastLoginAt?: string;
  createdAt: string;
  roles: string[];
}

export interface Role {
  id: number;
  name: string;
  description?: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
  isActive: boolean;
  roleIds?: number[];
}

export interface UpdateUserRequest {
  email: string;
  password?: string;
  firstName: string;
  lastName: string;
  phone?: string;
  isActive: boolean;
  roleIds?: number[];
}

export const userService = {
  getUsers: async (includeInactive = false): Promise<ApiResponse<UserDetail[]>> => {
    const response = await api.get<ApiResponse<UserDetail[]>>(
      `/users?includeInactive=${includeInactive}`
    );
    return response.data;
  },

  getUser: async (id: number): Promise<ApiResponse<UserDetail>> => {
    const response = await api.get<ApiResponse<UserDetail>>(`/users/${id}`);
    return response.data;
  },

  createUser: async (request: CreateUserRequest): Promise<ApiResponse<UserDetail>> => {
    const response = await api.post<ApiResponse<UserDetail>>('/users', request);
    return response.data;
  },

  updateUser: async (id: number, request: UpdateUserRequest): Promise<ApiResponse<UserDetail>> => {
    const response = await api.put<ApiResponse<UserDetail>>(`/users/${id}`, request);
    return response.data;
  },

  deleteUser: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/users/${id}`);
    return response.data;
  },

  unlockUser: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(`/users/${id}/unlock`);
    return response.data;
  },

  resetPassword: async (id: number, newPassword: string): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(`/users/${id}/reset-password`, { newPassword });
    return response.data;
  },

  getRoles: async (): Promise<ApiResponse<Role[]>> => {
    const response = await api.get<ApiResponse<Role[]>>('/users/roles');
    return response.data;
  },

  getTechnicians: async (): Promise<ApiResponse<UserSummary[]>> => {
    const response = await api.get<ApiResponse<UserSummary[]>>('/users/technicians');
    return response.data;
  },
};
