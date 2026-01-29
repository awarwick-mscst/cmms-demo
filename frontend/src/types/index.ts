export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  isActive: boolean;
  roles: string[];
  permissions: string[];
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors: string[];
}

export interface PagedResponse<T> {
  success: boolean;
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface Asset {
  id: number;
  assetTag: string;
  name: string;
  description?: string;
  categoryId: number;
  categoryName: string;
  locationId?: number;
  locationName?: string;
  status: string;
  criticality: string;
  manufacturer?: string;
  model?: string;
  serialNumber?: string;
  barcode?: string;
  purchaseDate?: string;
  purchaseCost?: number;
  warrantyExpiry?: string;
  expectedLifeYears?: number;
  installationDate?: string;
  lastMaintenanceDate?: string;
  nextMaintenanceDate?: string;
  parentAssetId?: number;
  parentAssetName?: string;
  assignedTo?: number;
  assignedToName?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateAssetRequest {
  assetTag?: string;
  name: string;
  description?: string;
  categoryId: number;
  locationId?: number;
  status: string;
  criticality: string;
  manufacturer?: string;
  model?: string;
  serialNumber?: string;
  barcode?: string;
  purchaseDate?: string;
  purchaseCost?: number;
  warrantyExpiry?: string;
  expectedLifeYears?: number;
  installationDate?: string;
  parentAssetId?: number;
  assignedTo?: number;
  notes?: string;
}

export interface UpdateAssetRequest extends Omit<CreateAssetRequest, 'assetTag'> {
  lastMaintenanceDate?: string;
  nextMaintenanceDate?: string;
}

export interface AssetCategory {
  id: number;
  name: string;
  code: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  level: number;
  sortOrder: number;
  isActive: boolean;
  children: AssetCategory[];
}

export interface AssetLocation {
  id: number;
  name: string;
  code: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  level: number;
  fullPath?: string;
  building?: string;
  floor?: string;
  room?: string;
  isActive: boolean;
  children: AssetLocation[];
}

export interface AssetFilter {
  search?: string;
  categoryId?: number;
  locationId?: number;
  status?: string;
  criticality?: string;
  assignedTo?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export const AssetStatuses = ['Active', 'Inactive', 'InMaintenance', 'Retired', 'Disposed'] as const;
export const AssetCriticalities = ['Critical', 'High', 'Medium', 'Low'] as const;
