import api from './api';
import {
  ApiResponse,
  StorageLocation,
  CreateStorageLocationRequest,
  UpdateStorageLocationRequest,
} from '../types';

export const storageLocationService = {
  getLocations: async (includeInactive = false): Promise<ApiResponse<StorageLocation[]>> => {
    const response = await api.get<ApiResponse<StorageLocation[]>>(
      `/storage-locations?includeInactive=${includeInactive}`
    );
    return response.data;
  },

  getLocationsFlat: async (includeInactive = false): Promise<ApiResponse<StorageLocation[]>> => {
    const response = await api.get<ApiResponse<StorageLocation[]>>(
      `/storage-locations/flat?includeInactive=${includeInactive}`
    );
    return response.data;
  },

  getLocation: async (id: number): Promise<ApiResponse<StorageLocation>> => {
    const response = await api.get<ApiResponse<StorageLocation>>(`/storage-locations/${id}`);
    return response.data;
  },

  createLocation: async (location: CreateStorageLocationRequest): Promise<ApiResponse<StorageLocation>> => {
    const response = await api.post<ApiResponse<StorageLocation>>('/storage-locations', location);
    return response.data;
  },

  updateLocation: async (id: number, location: UpdateStorageLocationRequest): Promise<ApiResponse<StorageLocation>> => {
    const response = await api.put<ApiResponse<StorageLocation>>(`/storage-locations/${id}`, location);
    return response.data;
  },

  deleteLocation: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/storage-locations/${id}`);
    return response.data;
  },
};
