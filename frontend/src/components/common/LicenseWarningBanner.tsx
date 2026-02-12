import React from 'react';
import { Alert, AlertTitle, Box } from '@mui/material';
import { useLicense } from '../../contexts/LicenseContext';

export const LicenseWarningBanner: React.FC = () => {
  const { status } = useLicense();

  if (!status || !status.isActivated) return null;

  const { status: licenseStatus, warningMessage, daysUntilExpiry, graceDaysRemaining } = status;

  if (licenseStatus === 'Valid' && !warningMessage) return null;

  let severity: 'warning' | 'error' = 'warning';
  let title = '';
  let message = '';

  if (licenseStatus === 'Expired' || licenseStatus === 'Revoked') {
    severity = 'error';
    title = licenseStatus === 'Revoked' ? 'License Revoked' : 'License Expired';
    message = warningMessage || 'Your license has expired. Please renew your subscription to continue using the application.';
  } else if (licenseStatus === 'GracePeriod') {
    severity = 'warning';
    title = 'Grace Period';
    message = warningMessage || `Operating in grace period. ${graceDaysRemaining} days remaining.`;
  } else if (warningMessage) {
    severity = 'warning';
    title = 'License Warning';
    message = warningMessage;
  } else if (daysUntilExpiry !== null && daysUntilExpiry <= 14) {
    severity = 'warning';
    title = 'License Expiring Soon';
    message = `Your license expires in ${daysUntilExpiry} days. Please contact your administrator to renew.`;
  } else {
    return null;
  }

  return (
    <Box sx={{ mb: 2 }}>
      <Alert severity={severity} variant="filled">
        <AlertTitle>{title}</AlertTitle>
        {message}
      </Alert>
    </Box>
  );
};
