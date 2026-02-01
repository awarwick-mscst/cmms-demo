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
  Card,
  CardContent,
  TextField,
  Alert,
} from '@mui/material';
import { PlayArrow as RunIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import { assetService } from '../../services/assetService';
import { MaintenancePerformedFilter, MaintenancePerformedItem, WorkOrderTypes } from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

export const MaintenancePerformedPage: React.FC = () => {
  const [filter, setFilter] = useState<MaintenancePerformedFilter>({
    fromDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0],
  });
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);

  const { data: assetsData } = useQuery({
    queryKey: ['assets-list'],
    queryFn: () => assetService.getAssets({ pageSize: 100 }),
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['maintenance-performed-report', filter],
    queryFn: () => reportService.getMaintenancePerformedReport(filter),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportMaintenancePerformedReport(filter);
      downloadCsv(blob, 'maintenance-performed-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const columns: ReportColumn<MaintenancePerformedItem>[] = [
    { id: 'workOrderNumber', label: 'WO Number', minWidth: 120 },
    { id: 'title', label: 'Title', minWidth: 150 },
    { id: 'type', label: 'Type', minWidth: 100 },
    { id: 'assetName', label: 'Asset', minWidth: 120 },
    {
      id: 'completedDate',
      label: 'Completed',
      minWidth: 100,
      format: (value) => (value ? new Date(value as string).toLocaleDateString() : ''),
    },
    {
      id: 'laborHours',
      label: 'Labor Hrs',
      align: 'right',
      minWidth: 90,
      format: (value) => (value as number).toFixed(1),
    },
    {
      id: 'laborCost',
      label: 'Labor Cost',
      align: 'right',
      minWidth: 100,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
    {
      id: 'partsCost',
      label: 'Parts Cost',
      align: 'right',
      minWidth: 100,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
    {
      id: 'totalCost',
      label: 'Total Cost',
      align: 'right',
      minWidth: 100,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
    { id: 'completedByName', label: 'Completed By', minWidth: 120 },
  ];

  const report = data?.data;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Maintenance Performed Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Completed work orders with labor hours, parts used, and total costs.
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
              <InputLabel>Asset</InputLabel>
              <Select
                value={filter.assetId || ''}
                label="Asset"
                onChange={(e) => {
                  setFilter({ ...filter, assetId: e.target.value as number || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All Assets</MenuItem>
                {assetsData?.items?.map((asset) => (
                  <MenuItem key={asset.id} value={asset.id}>
                    {asset.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Type</InputLabel>
              <Select
                value={filter.workOrderType || ''}
                label="Type"
                onChange={(e) => {
                  setFilter({ ...filter, workOrderType: e.target.value || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All Types</MenuItem>
                {WorkOrderTypes.map((type) => (
                  <MenuItem key={type} value={type}>
                    {type}
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

      {runReport && report && (
        <>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={6} sm={4} md={2}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Work Orders
                  </Typography>
                  <Typography variant="h5">{report.totalWorkOrders}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4} md={2}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Labor Hours
                  </Typography>
                  <Typography variant="h5">{report.totalLaborHours.toFixed(1)}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4} md={2}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Labor Cost
                  </Typography>
                  <Typography variant="h5">${report.totalLaborCost.toFixed(2)}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4} md={2}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Parts Cost
                  </Typography>
                  <Typography variant="h5">${report.totalPartsCost.toFixed(2)}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4} md={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Total Cost
                  </Typography>
                  <Typography variant="h4" color="primary.main">
                    ${report.totalCost.toFixed(2)}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <ReportTable
            columns={columns}
            data={report.items}
            loading={isLoading}
            onExport={handleExport}
            exportLoading={exportLoading}
            emptyMessage="No completed maintenance in this period"
            getRowKey={(row) => row.workOrderId}
          />
        </>
      )}
    </Box>
  );
};

export default MaintenancePerformedPage;
