import api from './api';

export interface LicenseStatusResponse {
  status: string;
  tier: string;
  enabledFeatures: string[];
  expiresAt: string | null;
  lastPhoneHome: string | null;
  daysUntilExpiry: number | null;
  graceDaysRemaining: number | null;
  warningMessage: string | null;
  isActivated: boolean;
}

export interface ActivateLicenseResponse {
  success: boolean;
  error?: string;
  tier?: string;
  features?: string[];
  expiresAt?: string;
}

export const licenseService = {
  getStatus: async () => {
    const response = await api.get<{ success: boolean; data: LicenseStatusResponse }>('/license/status');
    return response.data;
  },

  activate: async (licenseKey: string) => {
    const response = await api.post<{ success: boolean; data: ActivateLicenseResponse; message?: string }>('/license/activate', { licenseKey });
    return response.data;
  },

  deactivate: async () => {
    const response = await api.post<{ success: boolean; message?: string }>('/license/deactivate');
    return response.data;
  },

  forcePhoneHome: async () => {
    const response = await api.post<{ success: boolean; data: { daysUntilExpiry: number; warning?: string }; message?: string }>('/license/phone-home');
    return response.data;
  },
};
