import api from './api';
import {
  ApiResponse,
  PagedResponse,
  Supplier,
  CreateSupplierRequest,
  UpdateSupplierRequest,
  SupplierFilter,
} from '../types';

export const supplierService = {
  getSuppliers: async (filter: SupplierFilter = {}): Promise<PagedResponse<Supplier>> => {
    const params = new URLSearchParams();

    if (filter.search) params.append('search', filter.search);
    if (filter.isActive !== undefined) params.append('isActive', filter.isActive.toString());
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params.append('sortDescending', filter.sortDescending.toString());

    const response = await api.get<PagedResponse<Supplier>>(`/suppliers?${params.toString()}`);
    return response.data;
  },

  getSupplier: async (id: number): Promise<ApiResponse<Supplier>> => {
    const response = await api.get<ApiResponse<Supplier>>(`/suppliers/${id}`);
    return response.data;
  },

  createSupplier: async (supplier: CreateSupplierRequest): Promise<ApiResponse<Supplier>> => {
    const response = await api.post<ApiResponse<Supplier>>('/suppliers', supplier);
    return response.data;
  },

  updateSupplier: async (id: number, supplier: UpdateSupplierRequest): Promise<ApiResponse<Supplier>> => {
    const response = await api.put<ApiResponse<Supplier>>(`/suppliers/${id}`, supplier);
    return response.data;
  },

  deleteSupplier: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/suppliers/${id}`);
    return response.data;
  },
};
