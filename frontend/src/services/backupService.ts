import api from './api';
import { ApiResponse } from '../types';

export interface BackupInfo {
  tables: string[];
  recordCounts: Record<string, number>;
  totalRecords: number;
  estimatedSizeBytes: number;
}

export interface BackupValidation {
  isValid: boolean;
  version?: string;
  exportedAt?: string;
  tables: string[];
  recordCounts: Record<string, number>;
  totalRecords: number;
  errors: string[];
  warnings: string[];
}

export interface BackupImportResult {
  success: boolean;
  tablesImported: number;
  recordsImported: number;
  errors: string[];
  warnings: string[];
}

export const backupService = {
  getInfo: async (): Promise<ApiResponse<BackupInfo>> => {
    const response = await api.get<ApiResponse<BackupInfo>>('/backup/info');
    return response.data;
  },

  export: async (): Promise<Blob> => {
    const response = await api.get('/backup/export', {
      responseType: 'blob',
    });
    return response.data;
  },

  validate: async (file: File): Promise<ApiResponse<BackupValidation>> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post<ApiResponse<BackupValidation>>('/backup/validate', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  import: async (file: File, clearExisting: boolean = false): Promise<ApiResponse<BackupImportResult>> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post<ApiResponse<BackupImportResult>>(
      `/backup/import?clearExisting=${clearExisting}`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data;
  },
};
