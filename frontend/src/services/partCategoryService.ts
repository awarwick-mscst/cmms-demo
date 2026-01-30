import api from './api';
import {
  ApiResponse,
  PartCategory,
  CreatePartCategoryRequest,
  UpdatePartCategoryRequest,
} from '../types';

export const partCategoryService = {
  getCategories: async (includeInactive = false): Promise<ApiResponse<PartCategory[]>> => {
    const response = await api.get<ApiResponse<PartCategory[]>>(
      `/part-categories?includeInactive=${includeInactive}`
    );
    return response.data;
  },

  getCategory: async (id: number): Promise<ApiResponse<PartCategory>> => {
    const response = await api.get<ApiResponse<PartCategory>>(`/part-categories/${id}`);
    return response.data;
  },

  createCategory: async (category: CreatePartCategoryRequest): Promise<ApiResponse<PartCategory>> => {
    const response = await api.post<ApiResponse<PartCategory>>('/part-categories', category);
    return response.data;
  },

  updateCategory: async (id: number, category: UpdatePartCategoryRequest): Promise<ApiResponse<PartCategory>> => {
    const response = await api.put<ApiResponse<PartCategory>>(`/part-categories/${id}`, category);
    return response.data;
  },

  deleteCategory: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/part-categories/${id}`);
    return response.data;
  },
};
