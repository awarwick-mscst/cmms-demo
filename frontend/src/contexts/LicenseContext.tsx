import React, { createContext, useContext } from 'react';
import { useQuery } from '@tanstack/react-query';
import { licenseService, LicenseStatusResponse } from '../services/licenseService';

interface LicenseContextType {
  status: LicenseStatusResponse | null;
  isLoading: boolean;
  isFeatureEnabled: (feature: string) => boolean;
  refetch: () => void;
}

const LicenseContext = createContext<LicenseContextType>({
  status: null,
  isLoading: true,
  isFeatureEnabled: () => true,
  refetch: () => {},
});

export const LicenseProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['license-status'],
    queryFn: async () => {
      const result = await licenseService.getStatus();
      return result.data;
    },
    staleTime: 5 * 60 * 1000,
    retry: 1,
    // Don't block rendering while loading
    placeholderData: undefined,
  });

  const isFeatureEnabled = (feature: string): boolean => {
    if (!data) return true; // Allow while loading
    if (!data.isActivated) return true; // Allow when not activated (unlicensed mode)
    return data.enabledFeatures.includes(feature);
  };

  return (
    <LicenseContext.Provider value={{ status: data ?? null, isLoading, isFeatureEnabled, refetch }}>
      {children}
    </LicenseContext.Provider>
  );
};

export const useLicense = () => useContext(LicenseContext);
