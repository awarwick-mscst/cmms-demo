import api from './api';
import {
  ApiResponse,
  ReorderReportItem,
  ReorderReportFilter,
  InventoryValuationReport,
  InventoryValuationFilter,
  StockMovementItem,
  StockMovementFilter,
  OverdueMaintenanceReport,
  MaintenancePerformedReport,
  MaintenancePerformedFilter,
  PMComplianceReport,
  PMComplianceFilter,
  WorkOrderSummaryReport,
  WorkOrderSummaryFilter,
  AssetMaintenanceHistoryReport,
  AssetMaintenanceHistoryFilter,
} from '../types';

export const reportService = {
  // Inventory Reports
  getReorderReport: async (filter: ReorderReportFilter = {}): Promise<ApiResponse<ReorderReportItem[]>> => {
    const params = new URLSearchParams();
    if (filter.categoryId) params.append('categoryId', filter.categoryId.toString());
    if (filter.supplierId) params.append('supplierId', filter.supplierId.toString());
    if (filter.status) params.append('status', filter.status);

    const response = await api.get<ApiResponse<ReorderReportItem[]>>(`/reports/reorder?${params.toString()}`);
    return response.data;
  },

  exportReorderReport: async (filter: ReorderReportFilter = {}): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.categoryId) params.append('categoryId', filter.categoryId.toString());
    if (filter.supplierId) params.append('supplierId', filter.supplierId.toString());
    if (filter.status) params.append('status', filter.status);
    params.append('format', 'csv');

    const response = await api.get(`/reports/reorder?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  getInventoryValuationReport: async (filter: InventoryValuationFilter = {}): Promise<ApiResponse<InventoryValuationReport>> => {
    const params = new URLSearchParams();
    if (filter.categoryId) params.append('categoryId', filter.categoryId.toString());
    if (filter.locationId) params.append('locationId', filter.locationId.toString());

    const response = await api.get<ApiResponse<InventoryValuationReport>>(`/reports/inventory-valuation?${params.toString()}`);
    return response.data;
  },

  exportInventoryValuationReport: async (filter: InventoryValuationFilter = {}): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.categoryId) params.append('categoryId', filter.categoryId.toString());
    if (filter.locationId) params.append('locationId', filter.locationId.toString());
    params.append('format', 'csv');

    const response = await api.get(`/reports/inventory-valuation?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  getStockMovementReport: async (filter: StockMovementFilter = {}): Promise<ApiResponse<StockMovementItem[]>> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.partId) params.append('partId', filter.partId.toString());
    if (filter.transactionType) params.append('transactionType', filter.transactionType);
    if (filter.locationId) params.append('locationId', filter.locationId.toString());

    const response = await api.get<ApiResponse<StockMovementItem[]>>(`/reports/stock-movement?${params.toString()}`);
    return response.data;
  },

  exportStockMovementReport: async (filter: StockMovementFilter = {}): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.partId) params.append('partId', filter.partId.toString());
    if (filter.transactionType) params.append('transactionType', filter.transactionType);
    if (filter.locationId) params.append('locationId', filter.locationId.toString());
    params.append('format', 'csv');

    const response = await api.get(`/reports/stock-movement?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  // Maintenance Reports
  getOverdueMaintenanceReport: async (): Promise<ApiResponse<OverdueMaintenanceReport>> => {
    const response = await api.get<ApiResponse<OverdueMaintenanceReport>>('/reports/overdue-maintenance');
    return response.data;
  },

  exportOverdueMaintenanceReport: async (): Promise<Blob> => {
    const response = await api.get('/reports/overdue-maintenance?format=csv', {
      responseType: 'blob',
    });
    return response.data;
  },

  getMaintenancePerformedReport: async (filter: MaintenancePerformedFilter = {}): Promise<ApiResponse<MaintenancePerformedReport>> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.assetId) params.append('assetId', filter.assetId.toString());
    if (filter.technicianId) params.append('technicianId', filter.technicianId.toString());
    if (filter.workOrderType) params.append('workOrderType', filter.workOrderType);

    const response = await api.get<ApiResponse<MaintenancePerformedReport>>(`/reports/maintenance-performed?${params.toString()}`);
    return response.data;
  },

  exportMaintenancePerformedReport: async (filter: MaintenancePerformedFilter = {}): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.assetId) params.append('assetId', filter.assetId.toString());
    if (filter.technicianId) params.append('technicianId', filter.technicianId.toString());
    if (filter.workOrderType) params.append('workOrderType', filter.workOrderType);
    params.append('format', 'csv');

    const response = await api.get(`/reports/maintenance-performed?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  getPMComplianceReport: async (filter: PMComplianceFilter = {}): Promise<ApiResponse<PMComplianceReport>> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.assetId) params.append('assetId', filter.assetId.toString());

    const response = await api.get<ApiResponse<PMComplianceReport>>(`/reports/pm-compliance?${params.toString()}`);
    return response.data;
  },

  exportPMComplianceReport: async (filter: PMComplianceFilter = {}): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.assetId) params.append('assetId', filter.assetId.toString());
    params.append('format', 'csv');

    const response = await api.get(`/reports/pm-compliance?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  getWorkOrderSummaryReport: async (filter: WorkOrderSummaryFilter = {}): Promise<ApiResponse<WorkOrderSummaryReport>> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);

    const response = await api.get<ApiResponse<WorkOrderSummaryReport>>(`/reports/work-order-summary?${params.toString()}`);
    return response.data;
  },

  exportWorkOrderSummaryReport: async (filter: WorkOrderSummaryFilter = {}): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    params.append('format', 'csv');

    const response = await api.get(`/reports/work-order-summary?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  // Asset Reports
  getAssetMaintenanceHistoryReport: async (filter: AssetMaintenanceHistoryFilter): Promise<ApiResponse<AssetMaintenanceHistoryReport>> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);

    const response = await api.get<ApiResponse<AssetMaintenanceHistoryReport>>(`/reports/asset-maintenance-history/${filter.assetId}?${params.toString()}`);
    return response.data;
  },

  exportAssetMaintenanceHistoryReport: async (filter: AssetMaintenanceHistoryFilter): Promise<Blob> => {
    const params = new URLSearchParams();
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    params.append('format', 'csv');

    const response = await api.get(`/reports/asset-maintenance-history/${filter.assetId}?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },
};
