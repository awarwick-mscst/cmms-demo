import React, { useState } from 'react';
import {
  Box,
  Button,
  Paper,
  Typography,
  TextField,
  IconButton,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Grid,
  Alert,
  FormControlLabel,
  Switch,
  MenuItem,
  Autocomplete,
  Tooltip,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Wifi as WifiIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon,
  Usb as UsbIcon,
  Router as RouterIcon,
  Print as PrintIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { labelService } from '../../services/labelService';
import { LabelPrinter, CreateLabelPrinterRequest, DpiOptions, PrinterConnectionType, PrinterLanguage } from '../../types';
import { useAuth } from '../../hooks/useAuth';

const ConnectionTypes: { value: PrinterConnectionType; label: string }[] = [
  { value: 'Network', label: 'Network (TCP/IP)' },
  { value: 'WindowsPrinter', label: 'Windows Printer (USB/Parallel)' },
];

const PrinterLanguages: { value: PrinterLanguage; label: string; description: string }[] = [
  { value: 'ZPL', label: 'ZPL', description: 'Zebra Programming Language (newer printers)' },
  { value: 'EPL', label: 'EPL', description: 'Eltron Programming Language (older printers like LP 2824)' },
];

export const PrintersPage: React.FC = () => {
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const canManage = hasPermission('labels.manage') || hasPermission('Administrator');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingPrinter, setEditingPrinter] = useState<LabelPrinter | null>(null);
  const [testingId, setTestingId] = useState<number | null>(null);
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [formData, setFormData] = useState<CreateLabelPrinterRequest>({
    name: '',
    connectionType: 'Network',
    ipAddress: '',
    port: 9100,
    windowsPrinterName: '',
    language: 'ZPL',
    dpi: 203,
    isActive: true,
    isDefault: false,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['printers'],
    queryFn: () => labelService.getPrinters(),
  });

  // Fetch Windows printers when connection type is WindowsPrinter
  const { data: windowsPrinters } = useQuery({
    queryKey: ['windows-printers'],
    queryFn: () => labelService.getWindowsPrinters(),
    enabled: formData.connectionType === 'WindowsPrinter',
  });

  const createMutation = useMutation({
    mutationFn: labelService.createPrinter,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['printers'] });
      handleCloseDialog();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreateLabelPrinterRequest }) =>
      labelService.updatePrinter(id, {
        ...data,
        connectionType: data.connectionType || 'Network',
        ipAddress: data.ipAddress || '',
        port: data.port || 9100,
        language: data.language || 'ZPL',
        dpi: data.dpi || 203,
        isActive: data.isActive ?? true,
        isDefault: data.isDefault ?? false,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['printers'] });
      handleCloseDialog();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: labelService.deletePrinter,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['printers'] });
    },
  });

  const testMutation = useMutation({
    mutationFn: labelService.testPrinter,
    onSuccess: (response) => {
      const data = response.data;
      setTestResult({
        success: data?.success ?? false,
        message: data?.message || 'Unknown result'
      });
      setTestingId(null);
    },
    onError: () => {
      setTestResult({ success: false, message: 'Failed to test printer connection' });
      setTestingId(null);
    },
  });

  const testPrintMutation = useMutation({
    mutationFn: labelService.testPrintLabel,
    onSuccess: (response) => {
      const data = response.data;
      setTestResult({
        success: data?.success ?? false,
        message: data?.message || 'Unknown result'
      });
      setTestingId(null);
    },
    onError: () => {
      setTestResult({ success: false, message: 'Failed to send test print' });
      setTestingId(null);
    },
  });

  const setDefaultMutation = useMutation({
    mutationFn: labelService.setDefaultPrinter,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['printers'] });
    },
  });

  const handleOpenCreate = () => {
    setEditingPrinter(null);
    setFormData({
      name: '',
      connectionType: 'Network',
      ipAddress: '',
      port: 9100,
      windowsPrinterName: '',
      language: 'ZPL',
      dpi: 203,
      isActive: true,
      isDefault: false,
    });
    setDialogOpen(true);
  };

  const handleOpenEdit = (printer: LabelPrinter) => {
    setEditingPrinter(printer);
    setFormData({
      name: printer.name,
      connectionType: printer.connectionType || 'Network',
      ipAddress: printer.ipAddress,
      port: printer.port,
      windowsPrinterName: printer.windowsPrinterName,
      printerModel: printer.printerModel,
      language: printer.language || 'ZPL',
      dpi: printer.dpi,
      isActive: printer.isActive,
      isDefault: printer.isDefault,
      location: printer.location,
    });
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingPrinter(null);
  };

  const handleSubmit = () => {
    if (editingPrinter) {
      updateMutation.mutate({ id: editingPrinter.id, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this printer?')) {
      deleteMutation.mutate(id);
    }
  };

  const handleTest = (id: number) => {
    setTestingId(id);
    setTestResult(null);
    testMutation.mutate(id);
  };

  const handleTestPrint = (id: number) => {
    setTestingId(id);
    setTestResult(null);
    testPrintMutation.mutate(id);
  };

  const handleSetDefault = (id: number) => {
    setDefaultMutation.mutate(id);
  };

  const getConnectionDisplay = (printer: LabelPrinter) => {
    if (printer.connectionType === 'WindowsPrinter') {
      return printer.windowsPrinterName || 'Windows Printer';
    }
    return `${printer.ipAddress}:${printer.port}`;
  };

  const columns: GridColDef[] = [
    {
      field: 'actions',
      headerName: '',
      width: 160,
      sortable: false,
      renderCell: (params) => (
        <Box onClick={(e) => e.stopPropagation()}>
          <Tooltip title="Test connection">
            <IconButton
              size="small"
              onClick={() => handleTest(params.row.id)}
              disabled={testingId === params.row.id}
            >
              <WifiIcon fontSize="small" color={testingId === params.row.id ? 'disabled' : 'primary'} />
            </IconButton>
          </Tooltip>
          <Tooltip title="Print test label">
            <IconButton
              size="small"
              onClick={() => handleTestPrint(params.row.id)}
              disabled={testingId === params.row.id}
            >
              <PrintIcon fontSize="small" color={testingId === params.row.id ? 'disabled' : 'secondary'} />
            </IconButton>
          </Tooltip>
          {canManage && (
            <>
              <IconButton size="small" onClick={() => handleOpenEdit(params.row)}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton size="small" onClick={() => handleDelete(params.row.id)} color="error">
                <DeleteIcon fontSize="small" />
              </IconButton>
            </>
          )}
        </Box>
      ),
    },
    { field: 'name', headerName: 'Name', width: 180, flex: 1 },
    {
      field: 'connectionType',
      headerName: 'Connection',
      width: 200,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {params.value === 'WindowsPrinter' ? (
            <UsbIcon fontSize="small" color="action" />
          ) : (
            <RouterIcon fontSize="small" color="action" />
          )}
          <Typography variant="body2">{getConnectionDisplay(params.row)}</Typography>
        </Box>
      ),
    },
    {
      field: 'language',
      headerName: 'Language',
      width: 80,
      renderCell: (params) => (
        <Chip label={params.value || 'ZPL'} size="small" variant="outlined" />
      ),
    },
    { field: 'printerModel', headerName: 'Model', width: 130 },
    { field: 'dpi', headerName: 'DPI', width: 70 },
    { field: 'location', headerName: 'Location', width: 130 },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 90,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          size="small"
          color={params.value ? 'success' : 'default'}
        />
      ),
    },
    {
      field: 'isDefault',
      headerName: 'Default',
      width: 80,
      renderCell: (params) =>
        params.value ? (
          <StarIcon color="primary" fontSize="small" />
        ) : canManage ? (
          <IconButton size="small" onClick={(e) => { e.stopPropagation(); handleSetDefault(params.row.id); }}>
            <StarBorderIcon fontSize="small" />
          </IconButton>
        ) : null,
    },
  ];

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const isFormValid = formData.name && (
    formData.connectionType === 'Network' ? formData.ipAddress : formData.windowsPrinterName
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Label Printers</Typography>
        {canManage && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpenCreate}>
            Add Printer
          </Button>
        )}
      </Box>

      {testResult && (
        <Alert
          severity={testResult.success ? 'success' : 'error'}
          sx={{ mb: 2 }}
          onClose={() => setTestResult(null)}
        >
          {testResult.message}
        </Alert>
      )}

      <Paper sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <DataGrid
          rows={data?.data || []}
          columns={columns}
          loading={isLoading}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
          onRowClick={(params) => handleOpenEdit(params.row)}
          sx={{ flex: 1, '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>

      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingPrinter ? 'Edit Printer' : 'New Printer'}</DialogTitle>
        <DialogContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {(error as any)?.response?.data?.errors?.[0] || 'An error occurred'}
            </Alert>
          )}
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Printer Name"
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Warehouse Label Printer"
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                select
                label="Connection Type"
                value={formData.connectionType || 'Network'}
                onChange={(e) => setFormData({
                  ...formData,
                  connectionType: e.target.value as PrinterConnectionType
                })}
              >
                {ConnectionTypes.map((ct) => (
                  <MenuItem key={ct.value} value={ct.value}>
                    {ct.label}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                select
                label="Printer Language"
                value={formData.language || 'ZPL'}
                onChange={(e) => setFormData({
                  ...formData,
                  language: e.target.value as PrinterLanguage
                })}
              >
                {PrinterLanguages.map((lang) => (
                  <MenuItem key={lang.value} value={lang.value}>
                    {lang.label} - {lang.description}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            {formData.connectionType === 'Network' ? (
              <>
                <Grid item xs={12} sm={8}>
                  <TextField
                    fullWidth
                    label="IP Address"
                    required
                    value={formData.ipAddress}
                    onChange={(e) => setFormData({ ...formData, ipAddress: e.target.value })}
                    placeholder="192.168.1.100"
                  />
                </Grid>
                <Grid item xs={12} sm={4}>
                  <TextField
                    fullWidth
                    label="Port"
                    type="number"
                    value={formData.port || 9100}
                    onChange={(e) => setFormData({ ...formData, port: parseInt(e.target.value) || 9100 })}
                  />
                </Grid>
              </>
            ) : (
              <Grid item xs={12}>
                <Autocomplete
                  freeSolo
                  options={windowsPrinters?.data || []}
                  value={formData.windowsPrinterName || ''}
                  onChange={(_, value) => setFormData({ ...formData, windowsPrinterName: value || '' })}
                  onInputChange={(_, value) => setFormData({ ...formData, windowsPrinterName: value })}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label="Windows Printer Name"
                      required
                      placeholder="Select or type printer name"
                      helperText="Select from installed printers or type the exact Windows printer share name"
                    />
                  )}
                />
              </Grid>
            )}

            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Printer Model"
                value={formData.printerModel || ''}
                onChange={(e) => setFormData({ ...formData, printerModel: e.target.value })}
                placeholder="e.g., Zebra LP 2824"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                select
                label="DPI"
                value={formData.dpi || 203}
                onChange={(e) => setFormData({ ...formData, dpi: parseInt(e.target.value) })}
              >
                {DpiOptions.map((dpi) => (
                  <MenuItem key={dpi} value={dpi}>
                    {dpi} DPI
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Location"
                value={formData.location || ''}
                onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                placeholder="Warehouse, Shipping Desk, etc."
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.isActive ?? true}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.isDefault ?? false}
                    onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
                  />
                }
                label="Default Printer"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            onClick={handleSubmit}
            variant="contained"
            disabled={isSubmitting || !isFormValid}
          >
            {isSubmitting ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
