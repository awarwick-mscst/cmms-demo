import api from './api';
import {
  ApiResponse,
  PagedResponse,
  Part,
  PartDetail,
  PartStock,
  PartTransaction,
  AssetPart,
  CreatePartRequest,
  UpdatePartRequest,
  PartFilter,
  StockAdjustmentRequest,
  StockTransferRequest,
  StockReserveRequest,
  CreateAssetPartRequest,
  PartTransactionFilter,
} from '../types';

export const partService = {
  // Part CRUD
  getParts: async (filter: PartFilter = {}): Promise<PagedResponse<Part>> => {
    const params = new URLSearchParams();

    if (filter.search) params.append('search', filter.search);
    if (filter.categoryId) params.append('categoryId', filter.categoryId.toString());
    if (filter.supplierId) params.append('supplierId', filter.supplierId.toString());
    if (filter.status) params.append('status', filter.status);
    if (filter.lowStock !== undefined) params.append('lowStock', filter.lowStock.toString());
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params.append('sortDescending', filter.sortDescending.toString());

    const response = await api.get<PagedResponse<Part>>(`/parts?${params.toString()}`);
    return response.data;
  },

  getPart: async (id: number): Promise<ApiResponse<PartDetail>> => {
    const response = await api.get<ApiResponse<PartDetail>>(`/parts/${id}`);
    return response.data;
  },

  createPart: async (part: CreatePartRequest): Promise<ApiResponse<Part>> => {
    const response = await api.post<ApiResponse<Part>>('/parts', part);
    return response.data;
  },

  updatePart: async (id: number, part: UpdatePartRequest): Promise<ApiResponse<Part>> => {
    const response = await api.put<ApiResponse<Part>>(`/parts/${id}`, part);
    return response.data;
  },

  deletePart: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/parts/${id}`);
    return response.data;
  },

  // Stock Management
  getPartStock: async (partId: number): Promise<ApiResponse<PartStock[]>> => {
    const response = await api.get<ApiResponse<PartStock[]>>(`/parts/${partId}/stock`);
    return response.data;
  },

  adjustStock: async (partId: number, request: StockAdjustmentRequest): Promise<ApiResponse<PartStock>> => {
    const response = await api.post<ApiResponse<PartStock>>(`/parts/${partId}/stock/adjust`, request);
    return response.data;
  },

  transferStock: async (partId: number, request: StockTransferRequest): Promise<ApiResponse<{ fromStock: PartStock; toStock: PartStock }>> => {
    const response = await api.post<ApiResponse<{ fromStock: PartStock; toStock: PartStock }>>(`/parts/${partId}/stock/transfer`, request);
    return response.data;
  },

  reserveStock: async (partId: number, request: StockReserveRequest): Promise<ApiResponse<PartStock>> => {
    const response = await api.post<ApiResponse<PartStock>>(`/parts/${partId}/stock/reserve`, request);
    return response.data;
  },

  unreserveStock: async (partId: number, request: StockReserveRequest): Promise<ApiResponse<PartStock>> => {
    const response = await api.post<ApiResponse<PartStock>>(`/parts/${partId}/stock/unreserve`, request);
    return response.data;
  },

  // Transaction History
  getPartTransactions: async (partId: number, filter: Partial<PartTransactionFilter> = {}): Promise<PagedResponse<PartTransaction>> => {
    const params = new URLSearchParams();

    if (filter.transactionType) params.append('transactionType', filter.transactionType);
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const response = await api.get<PagedResponse<PartTransaction>>(`/parts/${partId}/transactions?${params.toString()}`);
    return response.data;
  },

  // Low Stock
  getLowStockParts: async (): Promise<ApiResponse<Part[]>> => {
    const response = await api.get<ApiResponse<Part[]>>('/parts/low-stock');
    return response.data;
  },

  // Asset Parts
  usePartOnAsset: async (partId: number, request: CreateAssetPartRequest): Promise<ApiResponse<AssetPart>> => {
    const response = await api.post<ApiResponse<AssetPart>>(`/parts/${partId}/use-on-asset`, request);
    return response.data;
  },
};
