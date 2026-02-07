import axios from 'axios';
import api from './api';
import { ApiResponse, Attachment, UpdateAttachmentRequest } from '../types';
import { store } from '../store';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://fragbox:5000/api/v1';

export const attachmentService = {
  getAttachments: async (entityType: string, entityId: number): Promise<ApiResponse<Attachment[]>> => {
    const params = new URLSearchParams();
    params.append('entityType', entityType);
    params.append('entityId', entityId.toString());
    const response = await api.get<ApiResponse<Attachment[]>>(`/attachments?${params.toString()}`);
    return response.data;
  },

  getAttachment: async (id: number): Promise<ApiResponse<Attachment>> => {
    const response = await api.get<ApiResponse<Attachment>>(`/attachments/${id}`);
    return response.data;
  },

  uploadAttachments: async (
    entityType: string,
    entityId: number,
    files: File[],
    options?: {
      title?: string;
      description?: string;
      onProgress?: (progress: number) => void;
    }
  ): Promise<ApiResponse<Attachment[]>> => {
    const formData = new FormData();
    formData.append('entityType', entityType);
    formData.append('entityId', entityId.toString());

    if (options?.title) {
      formData.append('title', options.title);
    }
    if (options?.description) {
      formData.append('description', options.description);
    }

    files.forEach((file) => {
      formData.append('files', file);
    });

    const state = store.getState();
    const token = state.auth.accessToken;

    const response = await axios.post<ApiResponse<Attachment[]>>(
      `${API_BASE_URL}/attachments`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
          Authorization: token ? `Bearer ${token}` : '',
        },
        onUploadProgress: (progressEvent) => {
          if (options?.onProgress && progressEvent.total) {
            const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            options.onProgress(progress);
          }
        },
      }
    );

    return response.data;
  },

  updateAttachment: async (id: number, request: UpdateAttachmentRequest): Promise<ApiResponse<Attachment>> => {
    const response = await api.put<ApiResponse<Attachment>>(`/attachments/${id}`, request);
    return response.data;
  },

  deleteAttachment: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/attachments/${id}`);
    return response.data;
  },

  setPrimaryImage: async (id: number): Promise<ApiResponse<Attachment>> => {
    const response = await api.post<ApiResponse<Attachment>>(`/attachments/${id}/set-primary`);
    return response.data;
  },

  getPrimaryImage: async (entityType: string, entityId: number): Promise<ApiResponse<Attachment>> => {
    const params = new URLSearchParams();
    params.append('entityType', entityType);
    params.append('entityId', entityId.toString());
    const response = await api.get<ApiResponse<Attachment>>(`/attachments/primary?${params.toString()}`);
    return response.data;
  },

  getDownloadUrl: (id: number): string => {
    return `${API_BASE_URL}/attachments/${id}/download`;
  },

  formatFileSize: (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  },

  isImage: (mimeType: string): boolean => {
    return mimeType.startsWith('image/');
  },

  getFileIcon: (mimeType: string): string => {
    if (mimeType.startsWith('image/')) return 'image';
    if (mimeType === 'application/pdf') return 'pdf';
    if (mimeType.includes('word') || mimeType.includes('document')) return 'doc';
    if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return 'xls';
    if (mimeType === 'text/plain') return 'txt';
    return 'file';
  },
};
