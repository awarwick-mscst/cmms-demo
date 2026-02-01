import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  TextField,
  Typography,
  MenuItem,
  Alert,
  Snackbar,
  Autocomplete,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Save as SaveIcon,
  Clear as ClearIcon,
} from '@mui/icons-material';
import { useQuery, useMutation } from '@tanstack/react-query';
import { partService } from '../../services/partService';
import { Part, StorageLocation } from '../../types';

interface ReceiveLineItem {
  id: number;
  part: Part | null;
  locationId: number | '';
  quantity: number | '';
  unitCost: number | '';
  notes: string;
}

export const ReceiveInventoryPage: React.FC = () => {
  const [lineItems, setLineItems] = useState<ReceiveLineItem[]>([
    { id: 1, part: null, locationId: '', quantity: '', unitCost: '', notes: '' },
  ]);
  const [poNumber, setPoNumber] = useState('');
  const [supplierNotes, setSupplierNotes] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  // Fetch parts
  const { data: partsData } = useQuery({
    queryKey: ['parts', { pageSize: 500 }],
    queryFn: () => partService.getParts({ pageSize: 500 }),
  });

  // Fetch storage locations
  const { data: locationsData } = useQuery({
    queryKey: ['storageLocations'],
    queryFn: () => partService.getStorageLocations(),
  });

  const parts = partsData?.items || [];
  const locations = locationsData?.data || [];

  // Flatten locations for dropdown
  const flattenLocations = (locs: StorageLocation[], prefix = ''): { id: number; name: string }[] => {
    return locs.flatMap(loc => {
      const name = prefix ? `${prefix} > ${loc.name}` : loc.name;
      const children = loc.children ? flattenLocations(loc.children, name) : [];
      return [{ id: loc.id, name: loc.fullPath || name }, ...children];
    });
  };
  const flatLocations = flattenLocations(locations);

  const receiveMutation = useMutation({
    mutationFn: async (item: ReceiveLineItem) => {
      if (!item.part || !item.locationId || !item.quantity) {
        throw new Error('Part, location, and quantity are required');
      }
      return partService.adjustStock(item.part.id, {
        transactionType: 'Receive',
        locationId: item.locationId as number,
        quantity: item.quantity as number,
        unitCost: item.unitCost as number || undefined,
        notes: [poNumber ? `PO: ${poNumber}` : '', item.notes].filter(Boolean).join(' - ') || undefined,
      });
    },
  });

  const handleAddLine = () => {
    setLineItems([
      ...lineItems,
      { id: Date.now(), part: null, locationId: '', quantity: '', unitCost: '', notes: '' },
    ]);
  };

  const handleRemoveLine = (id: number) => {
    if (lineItems.length > 1) {
      setLineItems(lineItems.filter(item => item.id !== id));
    }
  };

  const handleLineChange = (id: number, field: keyof ReceiveLineItem, value: any) => {
    setLineItems(lineItems.map(item =>
      item.id === id ? { ...item, [field]: value } : item
    ));
  };

  const handlePartSelect = (id: number, part: Part | null) => {
    setLineItems(lineItems.map(item => {
      if (item.id === id) {
        return {
          ...item,
          part,
          unitCost: part?.unitCost || '',
        };
      }
      return item;
    }));
  };

  const handleReceiveAll = async () => {
    const validItems = lineItems.filter(item => item.part && item.locationId && item.quantity);

    if (validItems.length === 0) {
      setErrorMessage('Please add at least one complete line item');
      return;
    }

    let successCount = 0;
    let errorCount = 0;

    for (const item of validItems) {
      try {
        await receiveMutation.mutateAsync(item);
        successCount++;
      } catch (error) {
        errorCount++;
        console.error('Failed to receive:', item, error);
      }
    }

    if (successCount > 0) {
      setSuccessMessage(`Successfully received ${successCount} item(s)`);
      // Clear the form
      setLineItems([{ id: Date.now(), part: null, locationId: '', quantity: '', unitCost: '', notes: '' }]);
      setPoNumber('');
      setSupplierNotes('');
    }

    if (errorCount > 0) {
      setErrorMessage(`Failed to receive ${errorCount} item(s)`);
    }
  };

  const handleClear = () => {
    setLineItems([{ id: Date.now(), part: null, locationId: '', quantity: '', unitCost: '', notes: '' }]);
    setPoNumber('');
    setSupplierNotes('');
  };

  const isValid = lineItems.some(item => item.part && item.locationId && item.quantity);

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        Receive Inventory
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={4}>
              <TextField
                fullWidth
                label="PO Number (optional)"
                value={poNumber}
                onChange={(e) => setPoNumber(e.target.value)}
                placeholder="e.g., PO-2024-001"
              />
            </Grid>
            <Grid item xs={12} sm={6} md={8}>
              <TextField
                fullWidth
                label="Notes (optional)"
                value={supplierNotes}
                onChange={(e) => setSupplierNotes(e.target.value)}
                placeholder="e.g., Delivery from ABC Supplier"
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Items to Receive
          </Typography>

          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ width: '30%' }}>Part</TableCell>
                  <TableCell sx={{ width: '20%' }}>Location</TableCell>
                  <TableCell sx={{ width: '12%' }}>Quantity</TableCell>
                  <TableCell sx={{ width: '12%' }}>Unit Cost</TableCell>
                  <TableCell sx={{ width: '20%' }}>Notes</TableCell>
                  <TableCell sx={{ width: '6%' }}></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {lineItems.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <Autocomplete
                        size="small"
                        options={parts}
                        getOptionLabel={(option) => `${option.partNumber} - ${option.name}`}
                        value={item.part}
                        onChange={(_, value) => handlePartSelect(item.id, value)}
                        renderInput={(params) => (
                          <TextField {...params} placeholder="Search parts..." />
                        )}
                        isOptionEqualToValue={(option, value) => option.id === value.id}
                      />
                    </TableCell>
                    <TableCell>
                      <TextField
                        select
                        size="small"
                        fullWidth
                        value={item.locationId}
                        onChange={(e) => handleLineChange(item.id, 'locationId', Number(e.target.value))}
                      >
                        <MenuItem value="">Select location</MenuItem>
                        {flatLocations.map((loc) => (
                          <MenuItem key={loc.id} value={loc.id}>
                            {loc.name}
                          </MenuItem>
                        ))}
                      </TextField>
                    </TableCell>
                    <TableCell>
                      <TextField
                        size="small"
                        type="number"
                        fullWidth
                        value={item.quantity}
                        onChange={(e) => handleLineChange(item.id, 'quantity', e.target.value ? Number(e.target.value) : '')}
                        inputProps={{ min: 1 }}
                      />
                    </TableCell>
                    <TableCell>
                      <TextField
                        size="small"
                        type="number"
                        fullWidth
                        value={item.unitCost}
                        onChange={(e) => handleLineChange(item.id, 'unitCost', e.target.value ? Number(e.target.value) : '')}
                        inputProps={{ min: 0, step: 0.01 }}
                        InputProps={{ startAdornment: '$' }}
                      />
                    </TableCell>
                    <TableCell>
                      <TextField
                        size="small"
                        fullWidth
                        value={item.notes}
                        onChange={(e) => handleLineChange(item.id, 'notes', e.target.value)}
                        placeholder="Line notes"
                      />
                    </TableCell>
                    <TableCell>
                      <IconButton
                        size="small"
                        onClick={() => handleRemoveLine(item.id)}
                        disabled={lineItems.length === 1}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          <Box sx={{ mt: 2, display: 'flex', gap: 2 }}>
            <Button startIcon={<AddIcon />} onClick={handleAddLine}>
              Add Line
            </Button>
          </Box>

          <Box sx={{ mt: 3, display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
            <Button
              variant="outlined"
              startIcon={<ClearIcon />}
              onClick={handleClear}
            >
              Clear All
            </Button>
            <Button
              variant="contained"
              color="success"
              startIcon={<SaveIcon />}
              onClick={handleReceiveAll}
              disabled={!isValid || receiveMutation.isPending}
            >
              {receiveMutation.isPending ? 'Receiving...' : 'Receive All'}
            </Button>
          </Box>
        </CardContent>
      </Card>

      <Snackbar
        open={!!successMessage}
        autoHideDuration={4000}
        onClose={() => setSuccessMessage('')}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setSuccessMessage('')}>
          {successMessage}
        </Alert>
      </Snackbar>

      <Snackbar
        open={!!errorMessage}
        autoHideDuration={4000}
        onClose={() => setErrorMessage('')}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="error" onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
};
