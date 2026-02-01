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
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Wifi as WifiIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { labelService } from '../../services/labelService';
import { LabelPrinter, CreateLabelPrinterRequest, DpiOptions } from '../../types';
import { useAuth } from '../../hooks/useAuth';

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
    ipAddress: '',
    port: 9100,
    dpi: 203,
    isActive: true,
    isDefault: false,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['printers'],
    queryFn: () => labelService.getPrinters(),
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
        port: data.port || 9100,
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
      ipAddress: '',
      port: 9100,
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
      ipAddress: printer.ipAddress,
      port: printer.port,
      printerModel: printer.printerModel,
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

  const handleSetDefault = (id: number) => {
    setDefaultMutation.mutate(id);
  };

  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Name', width: 200, flex: 1 },
    { field: 'ipAddress', headerName: 'IP Address', width: 150 },
    { field: 'port', headerName: 'Port', width: 80 },
    { field: 'printerModel', headerName: 'Model', width: 150 },
    { field: 'dpi', headerName: 'DPI', width: 80 },
    { field: 'location', headerName: 'Location', width: 150 },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 100,
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
          <IconButton size="small" onClick={() => handleSetDefault(params.row.id)}>
            <StarBorderIcon fontSize="small" />
          </IconButton>
        ) : null,
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 150,
      sortable: false,
      renderCell: (params) => (
        <Box>
          <IconButton
            size="small"
            onClick={() => handleTest(params.row.id)}
            disabled={testingId === params.row.id}
            title="Test connection"
          >
            <WifiIcon fontSize="small" color={testingId === params.row.id ? 'disabled' : 'primary'} />
          </IconButton>
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
  ];

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
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

      <Paper sx={{ height: 500 }}>
        <DataGrid
          rows={data?.data || []}
          columns={columns}
          loading={isLoading}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
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
              />
            </Grid>
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
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Printer Model"
                value={formData.printerModel || ''}
                onChange={(e) => setFormData({ ...formData, printerModel: e.target.value })}
                placeholder="Zebra ZD420"
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
            disabled={isSubmitting || !formData.name || !formData.ipAddress}
          >
            {isSubmitting ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
