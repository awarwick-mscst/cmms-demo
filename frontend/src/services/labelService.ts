import api from './api';
import {
  ApiResponse,
  LabelTemplate,
  LabelPrinter,
  CreateLabelTemplateRequest,
  UpdateLabelTemplateRequest,
  CreateLabelPrinterRequest,
  UpdateLabelPrinterRequest,
  PrintLabelRequest,
  PrintPreviewRequest,
  PrintPreviewResponse,
  PrintResult,
  PrinterTestResult,
} from '../types';

export const labelService = {
  // Template CRUD
  getTemplates: async (): Promise<ApiResponse<LabelTemplate[]>> => {
    const response = await api.get<ApiResponse<LabelTemplate[]>>('/label-templates');
    return response.data;
  },

  getTemplate: async (id: number): Promise<ApiResponse<LabelTemplate>> => {
    const response = await api.get<ApiResponse<LabelTemplate>>(`/label-templates/${id}`);
    return response.data;
  },

  getDefaultTemplate: async (): Promise<ApiResponse<LabelTemplate>> => {
    const response = await api.get<ApiResponse<LabelTemplate>>('/label-templates/default');
    return response.data;
  },

  createTemplate: async (template: CreateLabelTemplateRequest): Promise<ApiResponse<LabelTemplate>> => {
    const response = await api.post<ApiResponse<LabelTemplate>>('/label-templates', template);
    return response.data;
  },

  updateTemplate: async (id: number, template: UpdateLabelTemplateRequest): Promise<ApiResponse<LabelTemplate>> => {
    const response = await api.put<ApiResponse<LabelTemplate>>(`/label-templates/${id}`, template);
    return response.data;
  },

  deleteTemplate: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/label-templates/${id}`);
    return response.data;
  },

  setDefaultTemplate: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(`/label-templates/${id}/set-default`);
    return response.data;
  },

  // Printer CRUD
  getPrinters: async (activeOnly: boolean = false): Promise<ApiResponse<LabelPrinter[]>> => {
    const params = new URLSearchParams();
    if (activeOnly) params.append('activeOnly', 'true');
    const response = await api.get<ApiResponse<LabelPrinter[]>>(`/printers?${params.toString()}`);
    return response.data;
  },

  getPrinter: async (id: number): Promise<ApiResponse<LabelPrinter>> => {
    const response = await api.get<ApiResponse<LabelPrinter>>(`/printers/${id}`);
    return response.data;
  },

  getDefaultPrinter: async (): Promise<ApiResponse<LabelPrinter>> => {
    const response = await api.get<ApiResponse<LabelPrinter>>('/printers/default');
    return response.data;
  },

  createPrinter: async (printer: CreateLabelPrinterRequest): Promise<ApiResponse<LabelPrinter>> => {
    const response = await api.post<ApiResponse<LabelPrinter>>('/printers', printer);
    return response.data;
  },

  updatePrinter: async (id: number, printer: UpdateLabelPrinterRequest): Promise<ApiResponse<LabelPrinter>> => {
    const response = await api.put<ApiResponse<LabelPrinter>>(`/printers/${id}`, printer);
    return response.data;
  },

  deletePrinter: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/printers/${id}`);
    return response.data;
  },

  testPrinter: async (id: number): Promise<ApiResponse<PrinterTestResult>> => {
    const response = await api.post<ApiResponse<PrinterTestResult>>(`/printers/${id}/test`);
    return response.data;
  },

  setDefaultPrinter: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(`/printers/${id}/set-default`);
    return response.data;
  },

  // Printing
  printPartLabel: async (request: PrintLabelRequest): Promise<ApiResponse<PrintResult>> => {
    const response = await api.post<ApiResponse<PrintResult>>('/print/part-label', request);
    return response.data;
  },

  getPreview: async (request: PrintPreviewRequest): Promise<ApiResponse<PrintPreviewResponse>> => {
    const response = await api.post<ApiResponse<PrintPreviewResponse>>('/print/preview', request);
    return response.data;
  },
};
