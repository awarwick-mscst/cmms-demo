import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  Grid,
  TextField,
  Alert,
  Chip,
} from '@mui/material';
import { PlayArrow as RunIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import { partService } from '../../services/partService';
import { StockMovementFilter, StockMovementItem, TransactionTypes } from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

const transactionTypeColors: Record<string, 'success' | 'error' | 'info' | 'warning' | 'default'> = {
  Receive: 'success',
  Issue: 'error',
  Transfer: 'info',
  Adjust: 'warning',
  Reserve: 'default',
  Unreserve: 'default',
};

export const StockMovementPage: React.FC = () => {
  const [filter, setFilter] = useState<StockMovementFilter>({
    fromDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0],
  });
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);

  const { data: locationsData } = useQuery({
    queryKey: ['storageLocations'],
    queryFn: () => partService.getStorageLocations(),
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['stock-movement-report', filter],
    queryFn: () => reportService.getStockMovementReport(filter),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportStockMovementReport(filter);
      downloadCsv(blob, 'stock-movement-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const columns: ReportColumn<StockMovementItem>[] = [
    {
      id: 'transactionDate',
      label: 'Date',
      minWidth: 140,
      format: (value) => new Date(value as string).toLocaleString(),
    },
    { id: 'partNumber', label: 'Part Number', minWidth: 120 },
    { id: 'partName', label: 'Part Name', minWidth: 150 },
    {
      id: 'transactionType',
      label: 'Type',
      minWidth: 100,
      format: (value) => (
        <Chip
          label={value as string}
          color={transactionTypeColors[value as string] || 'default'}
          size="small"
        />
      ),
    },
    {
      id: 'quantity',
      label: 'Quantity',
      align: 'right',
      minWidth: 80,
      format: (value) => {
        const qty = value as number;
        return (
          <Typography color={qty > 0 ? 'success.main' : qty < 0 ? 'error.main' : 'inherit'}>
            {qty > 0 ? '+' : ''}{qty}
          </Typography>
        );
      },
    },
    { id: 'fromLocationName', label: 'From Location', minWidth: 120 },
    { id: 'toLocationName', label: 'To Location', minWidth: 120 },
    { id: 'reference', label: 'Reference', minWidth: 100 },
    { id: 'performedByName', label: 'Performed By', minWidth: 120 },
    { id: 'notes', label: 'Notes', minWidth: 150 },
  ];

  const reportData = data?.data || [];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Stock Movement Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Transaction history showing all inventory movements over a date range.
      </Typography>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Filters
        </Typography>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={6} md={2}>
            <TextField
              label="From Date"
              type="date"
              size="small"
              fullWidth
              value={filter.fromDate || ''}
              onChange={(e) => {
                setFilter({ ...filter, fromDate: e.target.value || undefined });
                setRunReport(false);
              }}
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <TextField
              label="To Date"
              type="date"
              size="small"
              fullWidth
              value={filter.toDate || ''}
              onChange={(e) => {
                setFilter({ ...filter, toDate: e.target.value || undefined });
                setRunReport(false);
              }}
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Transaction Type</InputLabel>
              <Select
                value={filter.transactionType || ''}
                label="Transaction Type"
                onChange={(e) => {
                  setFilter({ ...filter, transactionType: e.target.value || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All Types</MenuItem>
                {TransactionTypes.map((type) => (
                  <MenuItem key={type} value={type}>
                    {type}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Location</InputLabel>
              <Select
                value={filter.locationId || ''}
                label="Location"
                onChange={(e) => {
                  setFilter({ ...filter, locationId: e.target.value as number || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All Locations</MenuItem>
                {locationsData?.data?.map((loc) => (
                  <MenuItem key={loc.id} value={loc.id}>
                    {loc.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <Button
              variant="contained"
              startIcon={<RunIcon />}
              onClick={handleRunReport}
              fullWidth
            >
              Run Report
            </Button>
          </Grid>
        </Grid>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load report. Please try again.
        </Alert>
      )}

      {runReport && data?.data && (
        <ReportTable
          title={`${reportData.length} Transactions`}
          columns={columns}
          data={reportData}
          loading={isLoading}
          onExport={handleExport}
          exportLoading={exportLoading}
          emptyMessage="No transactions found for this period"
          getRowKey={(row) => row.transactionId}
        />
      )}
    </Box>
  );
};

export default StockMovementPage;
