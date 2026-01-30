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

// Inventory Types

export interface Supplier {
  id: number;
  name: string;
  code?: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  website?: string;
  notes?: string;
  isActive: boolean;
  partCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateSupplierRequest {
  name: string;
  code?: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  website?: string;
  notes?: string;
  isActive?: boolean;
}

export interface UpdateSupplierRequest extends CreateSupplierRequest {}

export interface SupplierFilter {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PartCategory {
  id: number;
  name: string;
  code?: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  level: number;
  sortOrder: number;
  isActive: boolean;
  partCount: number;
  children: PartCategory[];
  createdAt: string;
  updatedAt?: string;
}

export interface CreatePartCategoryRequest {
  name: string;
  code?: string;
  description?: string;
  parentId?: number;
  sortOrder?: number;
  isActive?: boolean;
}

export interface UpdatePartCategoryRequest extends CreatePartCategoryRequest {}

export interface StorageLocation {
  id: number;
  name: string;
  code?: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  level: number;
  fullPath?: string;
  building?: string;
  aisle?: string;
  rack?: string;
  shelf?: string;
  bin?: string;
  isActive: boolean;
  stockItemCount: number;
  children: StorageLocation[];
  createdAt: string;
  updatedAt?: string;
}

export interface CreateStorageLocationRequest {
  name: string;
  code?: string;
  description?: string;
  parentId?: number;
  building?: string;
  aisle?: string;
  rack?: string;
  shelf?: string;
  bin?: string;
  isActive?: boolean;
}

export interface UpdateStorageLocationRequest extends CreateStorageLocationRequest {}

export interface Part {
  id: number;
  partNumber: string;
  name: string;
  description?: string;
  categoryId?: number;
  categoryName?: string;
  supplierId?: number;
  supplierName?: string;
  unitOfMeasure: string;
  unitCost: number;
  reorderPoint: number;
  reorderQuantity: number;
  status: string;
  minStockLevel: number;
  maxStockLevel: number;
  leadTimeDays: number;
  specifications?: string;
  manufacturer?: string;
  manufacturerPartNumber?: string;
  barcode?: string;
  imageUrl?: string;
  notes?: string;
  totalQuantityOnHand: number;
  totalQuantityReserved: number;
  totalQuantityAvailable: number;
  reorderStatus: string;
  createdAt: string;
  updatedAt?: string;
}

export interface PartDetail extends Part {
  stocks: PartStock[];
  recentTransactions?: PartTransaction[];
}

export interface PartStock {
  id: number;
  partId: number;
  locationId: number;
  locationName: string;
  locationFullPath?: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  lastCountDate?: string;
  lastCountByName?: string;
  binNumber?: string;
  shelfLocation?: string;
}

export interface CreatePartRequest {
  partNumber?: string;
  name: string;
  description?: string;
  categoryId?: number;
  supplierId?: number;
  unitOfMeasure?: string;
  unitCost?: number;
  reorderPoint?: number;
  reorderQuantity?: number;
  status?: string;
  minStockLevel?: number;
  maxStockLevel?: number;
  leadTimeDays?: number;
  specifications?: string;
  manufacturer?: string;
  manufacturerPartNumber?: string;
  barcode?: string;
  imageUrl?: string;
  notes?: string;
}

export interface UpdatePartRequest extends Omit<CreatePartRequest, 'partNumber'> {}

export interface PartFilter {
  search?: string;
  categoryId?: number;
  supplierId?: number;
  status?: string;
  lowStock?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface StockAdjustmentRequest {
  locationId: number;
  transactionType: string;
  quantity: number;
  unitCost?: number;
  referenceType?: string;
  referenceId?: number;
  notes?: string;
}

export interface StockTransferRequest {
  fromLocationId: number;
  toLocationId: number;
  quantity: number;
  notes?: string;
}

export interface StockReserveRequest {
  locationId: number;
  quantity: number;
  referenceType?: string;
  referenceId?: number;
  notes?: string;
}

export interface PartTransaction {
  id: number;
  partId: number;
  partNumber: string;
  partName: string;
  locationId?: number;
  locationName?: string;
  toLocationId?: number;
  toLocationName?: string;
  transactionType: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  referenceType?: string;
  referenceId?: number;
  notes?: string;
  transactionDate: string;
  createdBy?: number;
  createdByName?: string;
  createdAt: string;
}

export interface PartTransactionFilter {
  partId?: number;
  locationId?: number;
  transactionType?: string;
  referenceType?: string;
  referenceId?: number;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface AssetPart {
  id: number;
  assetId: number;
  assetTag: string;
  assetName: string;
  partId: number;
  partNumber: string;
  partName: string;
  quantityUsed: number;
  unitCostAtTime: number;
  totalCost: number;
  usedDate: string;
  usedBy?: number;
  usedByName?: string;
  workOrderId?: number;
  notes?: string;
  createdAt: string;
}

export interface CreateAssetPartRequest {
  assetId: number;
  partId: number;
  locationId: number;
  quantityUsed: number;
  unitCostOverride?: number;
  workOrderId?: number;
  notes?: string;
}

export const PartStatuses = ['Active', 'Inactive', 'Obsolete', 'Discontinued'] as const;
export const TransactionTypes = ['Receive', 'Issue', 'Adjust', 'Transfer', 'Reserve', 'Unreserve'] as const;
export const ReorderStatuses = ['Ok', 'Low', 'Critical', 'OutOfStock'] as const;
export const UnitsOfMeasure = ['Each', 'Foot', 'Meter', 'Gallon', 'Liter', 'Pound', 'Kilogram', 'Box', 'Case', 'Roll', 'Set', 'Pair'] as const;
