import api from './api';
import { ApiResponse, BarcodeLookupResult } from '../types';

export const barcodeService = {
  lookup: async (barcode: string): Promise<ApiResponse<BarcodeLookupResult>> => {
    const response = await api.get<ApiResponse<BarcodeLookupResult>>(`/barcode/lookup/${encodeURIComponent(barcode)}`);
    return response.data;
  },
};
