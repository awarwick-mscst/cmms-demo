// Theme Types
export type ThemeMode = 'light' | 'dark' | 'system';

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

// Work Order Types

export interface WorkOrder {
  id: number;
  workOrderNumber: string;
  type: string;
  priority: string;
  status: string;
  title: string;
  description?: string;
  assetId?: number;
  assetName?: string;
  assetTag?: string;
  locationId?: number;
  locationName?: string;
  requestedBy?: string;
  requestedDate?: string;
  assignedToId?: number;
  assignedToName?: string;
  scheduledStartDate?: string;
  scheduledEndDate?: string;
  actualStartDate?: string;
  actualEndDate?: string;
  estimatedHours?: number;
  actualHours?: number;
  completionNotes?: string;
  preventiveMaintenanceScheduleId?: number;
  preventiveMaintenanceScheduleName?: string;
  createdAt: string;
  updatedAt?: string;
  createdByName?: string;
}

export interface WorkOrderSummary {
  id: number;
  workOrderNumber: string;
  type: string;
  priority: string;
  status: string;
  title: string;
  assetName?: string;
  locationName?: string;
  assignedToName?: string;
  scheduledStartDate?: string;
  scheduledEndDate?: string;
  createdAt: string;
}

export interface CreateWorkOrderRequest {
  type?: string;
  priority?: string;
  title: string;
  description?: string;
  assetId?: number;
  locationId?: number;
  requestedBy?: string;
  requestedDate?: string;
  assignedToId?: number;
  scheduledStartDate?: string;
  scheduledEndDate?: string;
  estimatedHours?: number;
}

export interface UpdateWorkOrderRequest extends CreateWorkOrderRequest {}

export interface CompleteWorkOrderRequest {
  completionNotes?: string;
  actualEndDate?: string;
}

export interface WorkOrderStatusChangeRequest {
  notes?: string;
}

export interface WorkOrderFilter {
  search?: string;
  type?: string;
  status?: string;
  priority?: string;
  assetId?: number;
  locationId?: number;
  assignedToId?: number;
  scheduledStartFrom?: string;
  scheduledStartTo?: string;
  createdFrom?: string;
  createdTo?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface WorkOrderDashboard {
  totalCount: number;
  byStatus: Record<string, number>;
  byType: Record<string, number>;
  byPriority: Record<string, number>;
  overdueCount: number;
  dueThisWeekCount: number;
}

export interface WorkOrderHistory {
  id: number;
  workOrderId: number;
  fromStatus?: string;
  toStatus: string;
  changedById: number;
  changedByName: string;
  changedAt: string;
  notes?: string;
}

export interface WorkOrderComment {
  id: number;
  workOrderId: number;
  comment: string;
  isInternal: boolean;
  createdById: number;
  createdByName: string;
  createdAt: string;
}

export interface CreateWorkOrderCommentRequest {
  comment: string;
  isInternal?: boolean;
}

export interface WorkOrderLabor {
  id: number;
  workOrderId: number;
  userId: number;
  userName: string;
  workDate: string;
  hoursWorked: number;
  laborType: string;
  hourlyRate?: number;
  totalCost?: number;
  notes?: string;
  createdAt: string;
}

export interface CreateWorkOrderLaborRequest {
  userId: number;
  workDate: string;
  hoursWorked: number;
  laborType?: string;
  hourlyRate?: number;
  notes?: string;
}

export interface WorkOrderLaborSummary {
  totalHours: number;
  totalCost: number;
  hoursByType: Record<string, number>;
}

export interface WorkOrderPart {
  id: number;
  assetId: number;
  assetName: string;
  partId: number;
  partNumber: string;
  partName: string;
  quantityUsed: number;
  unitCostAtTime: number;
  totalCost: number;
  usedDate: string;
  notes?: string;
}

// Preventive Maintenance Types

export interface PreventiveMaintenanceSchedule {
  id: number;
  name: string;
  description?: string;
  assetId?: number;
  assetName?: string;
  assetTag?: string;
  frequencyType: string;
  frequencyValue: number;
  dayOfWeek?: number;
  dayOfMonth?: number;
  nextDueDate?: string;
  lastCompletedDate?: string;
  leadTimeDays: number;
  workOrderTitle: string;
  workOrderDescription?: string;
  priority: string;
  estimatedHours?: number;
  isActive: boolean;
  taskTemplateId?: number;
  taskTemplateName?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface PreventiveMaintenanceScheduleSummary {
  id: number;
  name: string;
  assetName?: string;
  frequencyType: string;
  frequencyValue: number;
  nextDueDate?: string;
  lastCompletedDate?: string;
  priority: string;
  isActive: boolean;
}

export interface CreatePreventiveMaintenanceScheduleRequest {
  name: string;
  description?: string;
  assetId?: number;
  frequencyType?: string;
  frequencyValue?: number;
  dayOfWeek?: number;
  dayOfMonth?: number;
  nextDueDate?: string;
  leadTimeDays?: number;
  workOrderTitle: string;
  workOrderDescription?: string;
  priority?: string;
  estimatedHours?: number;
  isActive?: boolean;
  taskTemplateId?: number;
}

export interface UpdatePreventiveMaintenanceScheduleRequest extends CreatePreventiveMaintenanceScheduleRequest {}

export interface PreventiveMaintenanceScheduleFilter {
  search?: string;
  assetId?: number;
  frequencyType?: string;
  isActive?: boolean;
  dueBefore?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface UpcomingMaintenance {
  scheduleId: number;
  scheduleName: string;
  assetId?: number;
  assetName?: string;
  dueDate: string;
  daysUntilDue: number;
  priority: string;
}

export interface GenerateWorkOrdersResult {
  schedulesProcessed: number;
  workOrdersCreated: number;
  createdWorkOrderIds: number[];
  errors: string[];
}

export const WorkOrderTypes = ['Repair', 'ScheduledJob', 'SafetyInspection', 'PreventiveMaintenance'] as const;
export const WorkOrderStatuses = ['Draft', 'Open', 'InProgress', 'OnHold', 'Completed', 'Cancelled'] as const;
export const WorkOrderPriorities = ['Low', 'Medium', 'High', 'Critical', 'Emergency'] as const;
export const LaborTypes = ['Regular', 'Overtime', 'Emergency'] as const;
export const FrequencyTypes = ['Daily', 'Weekly', 'BiWeekly', 'Monthly', 'Quarterly', 'SemiAnnually', 'Annually', 'Custom'] as const;

// Work Order Task Types

export interface WorkOrderTask {
  id: number;
  workOrderId: number;
  sortOrder: number;
  description: string;
  isCompleted: boolean;
  completedAt?: string;
  completedById?: number;
  completedByName?: string;
  notes?: string;
  isRequired: boolean;
  createdAt: string;
}

export interface WorkOrderTaskSummary {
  totalTasks: number;
  completedTasks: number;
  requiredTasks: number;
  completedRequiredTasks: number;
  completionPercentage: number;
  allRequiredCompleted: boolean;
}

export interface CreateWorkOrderTaskRequest {
  description: string;
  isRequired?: boolean;
  sortOrder?: number;
}

export interface UpdateWorkOrderTaskRequest {
  description: string;
  isRequired?: boolean;
  notes?: string;
}

export interface CompleteTaskRequest {
  notes?: string;
}

export interface ReorderTasksRequest {
  taskIds: number[];
}

export interface ApplyTemplateRequest {
  templateId: number;
  clearExisting?: boolean;
}

// Task Template Types

export interface WorkOrderTaskTemplate {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  itemCount: number;
  items: WorkOrderTaskTemplateItem[];
  createdAt: string;
  updatedAt?: string;
}

export interface WorkOrderTaskTemplateSummary {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  itemCount: number;
  createdAt: string;
}

export interface WorkOrderTaskTemplateItem {
  id: number;
  sortOrder: number;
  description: string;
  isRequired: boolean;
}

export interface TaskTemplateDropdown {
  id: number;
  name: string;
  itemCount: number;
}

export interface CreateWorkOrderTaskTemplateRequest {
  name: string;
  description?: string;
  isActive?: boolean;
  items: CreateWorkOrderTaskTemplateItemRequest[];
}

export interface UpdateWorkOrderTaskTemplateRequest {
  name: string;
  description?: string;
  isActive?: boolean;
  items: UpdateWorkOrderTaskTemplateItemRequest[];
}

export interface CreateWorkOrderTaskTemplateItemRequest {
  sortOrder: number;
  description: string;
  isRequired?: boolean;
}

export interface UpdateWorkOrderTaskTemplateItemRequest {
  id?: number;
  sortOrder: number;
  description: string;
  isRequired?: boolean;
}

export interface TaskTemplateFilter {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

// Work Session Types (Active Time Tracking)

export interface WorkSession {
  id: number;
  workOrderId: number;
  workOrderNumber: string;
  workOrderTitle: string;
  userId: number;
  userName: string;
  startedAt: string;
  endedAt?: string;
  hoursWorked?: number;
  notes?: string;
  isActive: boolean;
  elapsedMinutes: number;
}

export interface StopSessionRequest {
  notes?: string;
}

export interface AddSessionNoteRequest {
  note: string;
}

// Label Printing Types

export interface LabelTemplate {
  id: number;
  name: string;
  description?: string;
  width: number;
  height: number;
  dpi: number;
  elementsJson: string;
  isDefault: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateLabelTemplateRequest {
  name: string;
  description?: string;
  width?: number;
  height?: number;
  dpi?: number;
  elementsJson: string;
  isDefault?: boolean;
}

export interface UpdateLabelTemplateRequest {
  name: string;
  description?: string;
  width: number;
  height: number;
  dpi: number;
  elementsJson: string;
  isDefault: boolean;
}

export type PrinterConnectionType = 'Network' | 'WindowsPrinter';
export type PrinterLanguage = 'ZPL' | 'EPL';

export interface LabelPrinter {
  id: number;
  name: string;
  connectionType: PrinterConnectionType;
  ipAddress: string;
  port: number;
  windowsPrinterName?: string;
  printerModel?: string;
  language: PrinterLanguage;
  dpi: number;
  isActive: boolean;
  isDefault: boolean;
  location?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateLabelPrinterRequest {
  name: string;
  connectionType?: PrinterConnectionType;
  ipAddress?: string;
  port?: number;
  windowsPrinterName?: string;
  printerModel?: string;
  language?: PrinterLanguage;
  dpi?: number;
  isActive?: boolean;
  isDefault?: boolean;
  location?: string;
}

export interface UpdateLabelPrinterRequest {
  name: string;
  connectionType: PrinterConnectionType;
  ipAddress: string;
  port: number;
  windowsPrinterName?: string;
  printerModel?: string;
  language: PrinterLanguage;
  dpi: number;
  isActive: boolean;
  isDefault: boolean;
  location?: string;
}

export interface PrintLabelRequest {
  partId: number;
  templateId?: number;
  printerId?: number;
  quantity?: number;
}

export interface PrintPreviewRequest {
  partId: number;
  templateId?: number;
  printerId?: number;
}

export interface PrintPreviewResponse {
  zpl: string;  // Contains EPL or ZPL commands
  language: PrinterLanguage;
  templateName: string;
  width: number;
  height: number;
}

export interface PrintResult {
  success: boolean;
  message?: string;
  printerName?: string;
  labelsPrinted: number;
}

export interface PrinterTestResult {
  success: boolean;
  message?: string;
}

export interface LabelElement {
  type: string;
  field: string;
  x: number;
  y: number;
  fontSize?: number;
  height?: number;
  maxWidth?: number;
  barcodeWidth?: number;  // Module/bar width for barcodes (1-5 dots)
  format?: string;
}

export const LabelFieldOptions = [
  { value: 'description', label: 'Description' },
  { value: 'partNumber', label: 'Part Number' },
  { value: 'manufacturerPartNumber', label: 'Manufacturer Part Number' },
  { value: 'name', label: 'Name' },
  { value: 'manufacturer', label: 'Manufacturer' },
  { value: 'barcode', label: 'Barcode' },
] as const;

export const DpiOptions = [203, 300, 600] as const;

// Report Types

export interface ReorderReportItem {
  partId: number;
  partNumber: string;
  name: string;
  categoryName?: string;
  supplierName?: string;
  quantityOnHand: number;
  quantityAvailable: number;
  reorderPoint: number;
  reorderQuantity: number;
  quantityToOrder: number;
  unitCost: number;
  estimatedCost: number;
  reorderStatus: string;
  leadTimeDays: number;
}

export interface ReorderReportFilter {
  categoryId?: number;
  supplierId?: number;
  status?: string;
}

export interface InventoryValuationReport {
  totalValue: number;
  totalParts: number;
  totalQuantity: number;
  items: InventoryValuationItem[];
  byCategory: ValuationByCategory[];
  byLocation: ValuationByLocation[];
}

export interface InventoryValuationItem {
  partId: number;
  partNumber: string;
  name: string;
  categoryName?: string;
  quantityOnHand: number;
  unitCost: number;
  totalValue: number;
}

export interface ValuationByCategory {
  categoryId?: number;
  categoryName: string;
  partCount: number;
  totalQuantity: number;
  totalValue: number;
}

export interface ValuationByLocation {
  locationId?: number;
  locationName: string;
  partCount: number;
  totalQuantity: number;
  totalValue: number;
}

export interface InventoryValuationFilter {
  categoryId?: number;
  locationId?: number;
}

export interface StockMovementItem {
  transactionId: number;
  partId: number;
  partNumber: string;
  partName: string;
  transactionType: string;
  quantity: number;
  fromLocationName?: string;
  toLocationName?: string;
  reference?: string;
  notes?: string;
  transactionDate: string;
  performedByName?: string;
}

export interface StockMovementFilter {
  fromDate?: string;
  toDate?: string;
  partId?: number;
  transactionType?: string;
  locationId?: number;
}

export interface OverdueMaintenanceReport {
  totalOverdue: number;
  overduePMCount: number;
  overdueWorkOrderCount: number;
  overduePMSchedules: OverduePMSchedule[];
  overdueWorkOrders: OverdueWorkOrder[];
}

export interface OverduePMSchedule {
  scheduleId: number;
  scheduleName: string;
  assetId?: number;
  assetName?: string;
  dueDate: string;
  daysOverdue: number;
  priority: string;
  frequencyDescription: string;
}

export interface OverdueWorkOrder {
  workOrderId: number;
  workOrderNumber: string;
  title: string;
  type: string;
  priority: string;
  status: string;
  assetId?: number;
  assetName?: string;
  scheduledEndDate?: string;
  daysOverdue: number;
  assignedToName?: string;
}

export interface MaintenancePerformedReport {
  totalWorkOrders: number;
  totalLaborHours: number;
  totalLaborCost: number;
  totalPartsCost: number;
  totalCost: number;
  items: MaintenancePerformedItem[];
}

export interface MaintenancePerformedItem {
  workOrderId: number;
  workOrderNumber: string;
  title: string;
  type: string;
  assetId?: number;
  assetName?: string;
  completedDate?: string;
  laborHours: number;
  laborCost: number;
  partsCost: number;
  totalCost: number;
  completedByName?: string;
}

export interface MaintenancePerformedFilter {
  fromDate?: string;
  toDate?: string;
  assetId?: number;
  technicianId?: number;
  workOrderType?: string;
}

export interface PMComplianceReport {
  totalScheduled: number;
  totalCompleted: number;
  totalMissed: number;
  complianceRate: number;
  fromDate?: string;
  toDate?: string;
  items: PMComplianceItem[];
}

export interface PMComplianceItem {
  scheduleId: number;
  scheduleName: string;
  assetId?: number;
  assetName?: string;
  frequencyDescription: string;
  scheduledCount: number;
  completedCount: number;
  missedCount: number;
  complianceRate: number;
}

export interface PMComplianceFilter {
  fromDate?: string;
  toDate?: string;
  assetId?: number;
}

export interface WorkOrderSummaryReport {
  totalWorkOrders: number;
  fromDate?: string;
  toDate?: string;
  byStatus: WorkOrderCountByStatus[];
  byType: WorkOrderCountByType[];
  byPriority: WorkOrderCountByPriority[];
}

export interface WorkOrderCountByStatus {
  status: string;
  count: number;
  percentage: number;
}

export interface WorkOrderCountByType {
  type: string;
  count: number;
  percentage: number;
}

export interface WorkOrderCountByPriority {
  priority: string;
  count: number;
  percentage: number;
}

export interface WorkOrderSummaryFilter {
  fromDate?: string;
  toDate?: string;
}

export interface AssetMaintenanceHistoryReport {
  assetId: number;
  assetTag: string;
  assetName: string;
  totalWorkOrders: number;
  totalLaborHours: number;
  totalCost: number;
  items: AssetMaintenanceHistoryItem[];
}

export interface AssetMaintenanceHistoryItem {
  workOrderId: number;
  workOrderNumber: string;
  title: string;
  type: string;
  status: string;
  priority: string;
  completedDate?: string;
  laborHours: number;
  totalCost: number;
  technicianName?: string;
}

export interface AssetMaintenanceHistoryFilter {
  assetId: number;
  fromDate?: string;
  toDate?: string;
}

// Attachment Types

export interface Attachment {
  id: number;
  entityType: string;
  entityId: number;
  attachmentType: string;
  title: string;
  fileName: string;
  filePath: string;
  url: string;
  fileSize: number;
  mimeType: string;
  description?: string;
  displayOrder: number;
  isPrimary: boolean;
  uploadedAt: string;
  uploadedBy?: number;
  uploadedByName?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface UpdateAttachmentRequest {
  title?: string;
  description?: string;
  displayOrder?: number;
}

export interface UploadProgress {
  fileName: string;
  progress: number;
  status: 'pending' | 'uploading' | 'success' | 'error';
  error?: string;
}

export const AttachmentTypes = ['Image', 'Document'] as const;

// Barcode Scanner Types

export interface BarcodeLookupResult {
  type: 'Part' | 'Asset';
  id: number;
  name: string;
  code: string;
  description?: string;
  barcode: string;
}
export const AllowedImageExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'] as const;
export const AllowedDocumentExtensions = ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.txt'] as const;
export const MaxFileSize = 10 * 1024 * 1024; // 10 MB

// Notification Types

export interface NotificationQueueItem {
  id: number;
  type: string;
  recipientUserId?: number;
  recipientEmail: string;
  recipientName?: string;
  subject: string;
  status: string;
  retryCount: number;
  scheduledFor: string;
  processedAt?: string;
  errorMessage?: string;
  referenceType?: string;
  referenceId?: number;
  createdAt: string;
}

export interface NotificationLogItem {
  id: number;
  type: string;
  recipientEmail: string;
  subject: string;
  channel: string;
  success: boolean;
  externalMessageId?: string;
  errorMessage?: string;
  sentAt: string;
  referenceType?: string;
  referenceId?: number;
}

export interface UserNotificationPreference {
  id: number;
  userId: number;
  notificationType: string;
  notificationTypeDisplay: string;
  emailEnabled: boolean;
  calendarEnabled: boolean;
}

export interface UpdateNotificationPreferenceRequest {
  notificationType: string;
  emailEnabled: boolean;
  calendarEnabled: boolean;
}

export interface BulkUpdateNotificationPreferencesRequest {
  preferences: UpdateNotificationPreferenceRequest[];
}

export interface IntegrationSettings {
  providerType: string;
  isConfigured: boolean;
  isValid: boolean;
  lastValidated?: string;
}

export interface MicrosoftGraphSettings {
  tenantId: string;
  clientId: string;
  clientSecret: string;
  sharedMailbox: string;
  sharedCalendarId: string;
  teamsWebhookUrl?: string;
}

export interface UpdateMicrosoftGraphSettingsRequest {
  tenantId: string;
  clientId: string;
  clientSecret: string;
  sharedMailbox: string;
  sharedCalendarId?: string;
  teamsWebhookUrl?: string;
}

export interface TestEmailRequest {
  toEmail: string;
}

export interface TestCalendarEventRequest {
  title?: string;
  startTime?: string;
  durationMinutes?: number;
}

export interface CalendarEventItem {
  id: number;
  externalEventId: string;
  calendarType: string;
  userId?: number;
  userName?: string;
  referenceType: string;
  referenceId: number;
  title: string;
  startTime: string;
  endTime: string;
  providerType: string;
  createdAt: string;
}

export interface NotificationStats {
  pendingCount: number;
  processingCount: number;
  sentToday: number;
  failedToday: number;
  totalSent: number;
  totalFailed: number;
}

export interface NotificationQueueFilter {
  status?: string;
  type?: string;
  referenceType?: string;
  referenceId?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface NotificationLogFilter {
  success?: boolean;
  type?: string;
  channel?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface NotificationTypeInfo {
  value: string;
  displayName: string;
  description: string;
}

export const NotificationTypes = [
  'WorkOrderAssigned',
  'WorkOrderApproachingDue',
  'WorkOrderOverdue',
  'WorkOrderCompleted',
  'PMScheduleComingDue',
  'PMScheduleOverdue',
  'LowStockAlert'
] as const;

export const NotificationStatuses = ['Pending', 'Processing', 'Sent', 'Failed'] as const;
export const NotificationChannels = ['Email', 'Calendar', 'Teams'] as const;

// Licensing
export const LicenseTiers = ['Basic', 'Pro', 'Enterprise'] as const;
export type LicenseTier = typeof LicenseTiers[number];

export const LicenseStatuses = ['Valid', 'GracePeriod', 'Expired', 'Revoked', 'NotActivated'] as const;
export type LicenseStatus = typeof LicenseStatuses[number];

export interface LicenseStatusInfo {
  status: LicenseStatus;
  tier: LicenseTier;
  enabledFeatures: string[];
  expiresAt: string | null;
  lastPhoneHome: string | null;
  daysUntilExpiry: number | null;
  graceDaysRemaining: number | null;
  warningMessage: string | null;
  isActivated: boolean;
}

// AI Assistant
export interface AiConversation {
  id: number;
  title: string;
  summary: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface AiConversationDetail {
  id: number;
  title: string;
  summary: string | null;
  createdAt: string;
  updatedAt: string | null;
  messages: AiMessage[];
}

export interface AiMessage {
  id: number;
  role: 'user' | 'assistant' | 'system';
  content: string;
  contextType: string | null;
  createdAt: string;
}

export interface AiStatus {
  enabled: boolean;
  reachable: boolean;
  model: string | null;
}

export interface SendMessageRequest {
  message: string;
  contextType?: string;
  assetId?: number;
}
