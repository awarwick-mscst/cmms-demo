import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  Typography,
  Button,
  TextField,
  Alert,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
} from '@mui/material';
import {
  Key as KeyIcon,
  CheckCircle as CheckIcon,
  Cancel as CancelIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useMutation } from '@tanstack/react-query';
import { useLicense } from '../../contexts/LicenseContext';
import { licenseService } from '../../services/licenseService';

const tierFeatures = [
  { name: 'Work Orders & Assets', feature: 'work-orders', basic: true, pro: true, enterprise: true },
  { name: 'Inventory Management', feature: 'inventory', basic: false, pro: true, enterprise: true },
  { name: 'Preventive Maintenance', feature: 'preventive-maintenance', basic: false, pro: true, enterprise: true },
  { name: 'Advanced Reporting', feature: 'advanced-reporting', basic: false, pro: true, enterprise: true },
  { name: 'Label Printing', feature: 'label-printing', basic: false, pro: true, enterprise: true },
  { name: 'LDAP Integration', feature: 'ldap', basic: false, pro: false, enterprise: true },
  { name: 'Email/Calendar', feature: 'email-calendar', basic: false, pro: false, enterprise: true },
  { name: 'Backup Management', feature: 'backup', basic: false, pro: false, enterprise: true },
  { name: 'API Access', feature: 'api-access', basic: false, pro: false, enterprise: true },
];

export const LicensePage: React.FC = () => {
  const { status, refetch } = useLicense();
  const [activateOpen, setActivateOpen] = useState(false);
  const [licenseKey, setLicenseKey] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const activateMutation = useMutation({
    mutationFn: (key: string) => licenseService.activate(key),
    onSuccess: (data) => {
      if (data.success) {
        setMessage({ type: 'success', text: 'License activated successfully!' });
        setActivateOpen(false);
        setLicenseKey('');
        refetch();
      } else {
        setMessage({ type: 'error', text: (data as any).error || 'Activation failed.' });
      }
    },
    onError: (error: any) => {
      setMessage({ type: 'error', text: error.response?.data?.error || 'Failed to activate license.' });
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: () => licenseService.deactivate(),
    onSuccess: () => {
      setMessage({ type: 'success', text: 'License deactivated.' });
      refetch();
    },
    onError: () => {
      setMessage({ type: 'error', text: 'Failed to deactivate license.' });
    },
  });

  const phoneHomeMutation = useMutation({
    mutationFn: () => licenseService.forcePhoneHome(),
    onSuccess: (data) => {
      if (data.success) {
        setMessage({ type: 'success', text: data.message || 'Phone-home successful.' });
        refetch();
      } else {
        setMessage({ type: 'error', text: (data as any).error || 'Phone-home failed.' });
      }
    },
    onError: () => {
      setMessage({ type: 'error', text: 'Phone-home failed.' });
    },
  });

  const getStatusColor = (s: string) => {
    switch (s) {
      case 'Valid': return 'success';
      case 'GracePeriod': return 'warning';
      case 'Expired': case 'Revoked': return 'error';
      default: return 'default';
    }
  };

  const getTierColor = (tier: string) => {
    switch (tier) {
      case 'Enterprise': return 'error';
      case 'Pro': return 'primary';
      default: return 'default';
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>License Management</Typography>

      {message && (
        <Alert severity={message.type} onClose={() => setMessage(null)} sx={{ mb: 2 }}>
          {message.text}
        </Alert>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2, mb: 3 }}>
        <Card>
          <CardHeader title="License Status" avatar={<KeyIcon />} />
          <CardContent>
            {status ? (
              <Box>
                <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                  <Chip
                    label={status.status}
                    color={getStatusColor(status.status) as any}
                  />
                  {status.isActivated && (
                    <Chip
                      label={status.tier}
                      color={getTierColor(status.tier) as any}
                      variant="outlined"
                    />
                  )}
                </Box>
                {status.isActivated ? (
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Expires: {status.expiresAt ? new Date(status.expiresAt).toLocaleDateString() : 'N/A'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Days Until Expiry: {status.daysUntilExpiry ?? 'N/A'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Last Phone Home: {status.lastPhoneHome ? new Date(status.lastPhoneHome).toLocaleString() : 'Never'}
                    </Typography>
                    {status.graceDaysRemaining !== null && (
                      <Typography variant="body2" color="warning.main">
                        Grace Days Remaining: {status.graceDaysRemaining}
                      </Typography>
                    )}
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No license activated. Enter a license key to activate.
                  </Typography>
                )}
              </Box>
            ) : (
              <CircularProgress size={24} />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader title="Actions" />
          <CardContent>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Button
                variant="contained"
                startIcon={<KeyIcon />}
                onClick={() => setActivateOpen(true)}
              >
                {status?.isActivated ? 'Change License Key' : 'Activate License'}
              </Button>
              {status?.isActivated && (
                <>
                  <Button
                    variant="outlined"
                    startIcon={<RefreshIcon />}
                    onClick={() => phoneHomeMutation.mutate()}
                    disabled={phoneHomeMutation.isPending}
                  >
                    {phoneHomeMutation.isPending ? 'Checking...' : 'Force Phone Home'}
                  </Button>
                  <Button
                    variant="outlined"
                    color="error"
                    startIcon={<CancelIcon />}
                    onClick={() => {
                      if (window.confirm('Are you sure you want to deactivate the license?')) {
                        deactivateMutation.mutate();
                      }
                    }}
                    disabled={deactivateMutation.isPending}
                  >
                    Deactivate License
                  </Button>
                </>
              )}
            </Box>
          </CardContent>
        </Card>
      </Box>

      <Card>
        <CardHeader title="Feature Comparison" />
        <CardContent sx={{ p: 0 }}>
          <TableContainer component={Paper} elevation={0}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell><strong>Feature</strong></TableCell>
                  <TableCell align="center"><strong>Basic</strong></TableCell>
                  <TableCell align="center"><strong>Pro</strong></TableCell>
                  <TableCell align="center"><strong>Enterprise</strong></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {tierFeatures.map((f) => {
                  const enabled = status?.enabledFeatures?.includes(f.feature);
                  return (
                    <TableRow key={f.feature} sx={enabled === false && status?.isActivated ? { opacity: 0.5 } : {}}>
                      <TableCell>{f.name}</TableCell>
                      <TableCell align="center">
                        {f.basic ? <CheckIcon color="success" fontSize="small" /> : <CancelIcon color="disabled" fontSize="small" />}
                      </TableCell>
                      <TableCell align="center">
                        {f.pro ? <CheckIcon color="success" fontSize="small" /> : <CancelIcon color="disabled" fontSize="small" />}
                      </TableCell>
                      <TableCell align="center">
                        {f.enterprise ? <CheckIcon color="success" fontSize="small" /> : <CancelIcon color="disabled" fontSize="small" />}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Dialog open={activateOpen} onClose={() => setActivateOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Activate License</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="License Key"
            fullWidth
            multiline
            rows={4}
            value={licenseKey}
            onChange={(e) => setLicenseKey(e.target.value)}
            placeholder="Paste your license key here..."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setActivateOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => activateMutation.mutate(licenseKey)}
            disabled={!licenseKey.trim() || activateMutation.isPending}
          >
            {activateMutation.isPending ? 'Activating...' : 'Activate'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
