import api from './api';
import { ApiResponse } from '../types';

export interface DatabaseSettings {
  provider: string;
  server: string;
  port?: number;
  database: string;
  authType: string;
  username?: string;
  password?: string;
  additionalOptions?: string;
  filePath?: string;
  isConfigured: boolean;
  tier: string;
}

export interface DatabaseTestRequest {
  provider: string;
  server: string;
  port?: number;
  database: string;
  authType: string;
  username?: string;
  password?: string;
  additionalOptions?: string;
  filePath?: string;
}

export interface DatabaseTestResult {
  success: boolean;
  message?: string;
  serverVersion?: string;
  errorDetails?: string;
  latencyMs?: number;
}

export interface DatabaseProviderInfo {
  name: string;
  displayName: string;
  defaultPort: number;
  supportsWindowsAuth: boolean;
  requiresFilePath: boolean;
  isSupported: boolean;
  notSupportedReason?: string;
}

export const databaseConfigService = {
  getStatus: async (): Promise<boolean> => {
    const response = await api.get<ApiResponse<boolean>>('/databaseconfig/status');
    return response.data.data ?? false;
  },

  getSettings: async (): Promise<DatabaseSettings> => {
    const response = await api.get<ApiResponse<DatabaseSettings>>('/databaseconfig/settings');
    return response.data.data ?? {
      provider: 'SqlServer',
      server: 'localhost',
      database: 'CMMS',
      authType: 'Windows',
      isConfigured: false,
      tier: 'Small',
    };
  },

  saveSettings: async (settings: DatabaseSettings): Promise<boolean> => {
    const response = await api.post<ApiResponse<boolean>>('/databaseconfig/settings', settings);
    return response.data.data ?? false;
  },

  testConnection: async (request: DatabaseTestRequest): Promise<DatabaseTestResult> => {
    const response = await api.post<ApiResponse<DatabaseTestResult>>('/databaseconfig/test', request);
    return response.data.data ?? { success: false, message: 'Unknown error' };
  },

  getProviders: async (): Promise<DatabaseProviderInfo[]> => {
    const response = await api.get<ApiResponse<DatabaseProviderInfo[]>>('/databaseconfig/providers');
    return response.data.data ?? [];
  },

  initialSetup: async (settings: DatabaseSettings): Promise<boolean> => {
    const response = await api.post<ApiResponse<boolean>>('/databaseconfig/setup', settings);
    return response.data.data ?? false;
  }
};
