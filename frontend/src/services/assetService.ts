import api from './api';
import {
  ApiResponse,
  Asset,
  AssetCategory,
  AssetFilter,
  AssetLocation,
  CreateAssetRequest,
  PagedResponse,
  UpdateAssetRequest,
} from '../types';

export const assetService = {
  getAssets: async (filter: AssetFilter = {}): Promise<PagedResponse<Asset>> => {
    const params = new URLSearchParams();

    if (filter.search) params.append('search', filter.search);
    if (filter.categoryId) params.append('categoryId', filter.categoryId.toString());
    if (filter.locationId) params.append('locationId', filter.locationId.toString());
    if (filter.status) params.append('status', filter.status);
    if (filter.criticality) params.append('criticality', filter.criticality);
    if (filter.assignedTo) params.append('assignedTo', filter.assignedTo.toString());
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params.append('sortDescending', filter.sortDescending.toString());

    const response = await api.get<PagedResponse<Asset>>(`/assets?${params.toString()}`);
    return response.data;
  },

  getAsset: async (id: number): Promise<ApiResponse<Asset>> => {
    const response = await api.get<ApiResponse<Asset>>(`/assets/${id}`);
    return response.data;
  },

  createAsset: async (asset: CreateAssetRequest): Promise<ApiResponse<Asset>> => {
    const response = await api.post<ApiResponse<Asset>>('/assets', asset);
    return response.data;
  },

  updateAsset: async (id: number, asset: UpdateAssetRequest): Promise<ApiResponse<Asset>> => {
    const response = await api.put<ApiResponse<Asset>>(`/assets/${id}`, asset);
    return response.data;
  },

  deleteAsset: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/assets/${id}`);
    return response.data;
  },

  // Categories
  getCategories: async (includeInactive = false): Promise<ApiResponse<AssetCategory[]>> => {
    const response = await api.get<ApiResponse<AssetCategory[]>>(
      `/assetcategories?includeInactive=${includeInactive}`
    );
    return response.data;
  },

  getCategory: async (id: number): Promise<ApiResponse<AssetCategory>> => {
    const response = await api.get<ApiResponse<AssetCategory>>(`/assetcategories/${id}`);
    return response.data;
  },

  createCategory: async (category: Partial<AssetCategory>): Promise<ApiResponse<AssetCategory>> => {
    const response = await api.post<ApiResponse<AssetCategory>>('/assetcategories', category);
    return response.data;
  },

  // Locations
  getLocations: async (includeInactive = false): Promise<ApiResponse<AssetLocation[]>> => {
    const response = await api.get<ApiResponse<AssetLocation[]>>(
      `/assetlocations?includeInactive=${includeInactive}`
    );
    return response.data;
  },

  getLocationTree: async (): Promise<ApiResponse<AssetLocation[]>> => {
    const response = await api.get<ApiResponse<AssetLocation[]>>('/assetlocations/tree');
    return response.data;
  },

  getLocation: async (id: number): Promise<ApiResponse<AssetLocation>> => {
    const response = await api.get<ApiResponse<AssetLocation>>(`/assetlocations/${id}`);
    return response.data;
  },

  createLocation: async (location: Partial<AssetLocation>): Promise<ApiResponse<AssetLocation>> => {
    const response = await api.post<ApiResponse<AssetLocation>>('/assetlocations', location);
    return response.data;
  },
};
