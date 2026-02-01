import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Grid,
  Card,
  CardContent,
  TextField,
  Alert,
} from '@mui/material';
import { PlayArrow as RunIcon, PieChart as ChartIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import {
  WorkOrderSummaryFilter,
  WorkOrderCountByStatus,
  WorkOrderCountByType,
  WorkOrderCountByPriority,
} from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

export const WorkOrderSummaryPage: React.FC = () => {
  const [filter, setFilter] = useState<WorkOrderSummaryFilter>({
    fromDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0],
  });
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ['work-order-summary-report', filter],
    queryFn: () => reportService.getWorkOrderSummaryReport(filter),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportWorkOrderSummaryReport(filter);
      downloadCsv(blob, 'work-order-summary-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const statusColumns: ReportColumn<WorkOrderCountByStatus>[] = [
    { id: 'status', label: 'Status', minWidth: 120 },
    { id: 'count', label: 'Count', align: 'right', minWidth: 80 },
    {
      id: 'percentage',
      label: 'Percentage',
      align: 'right',
      minWidth: 100,
      format: (value) => `${(value as number).toFixed(1)}%`,
    },
  ];

  const typeColumns: ReportColumn<WorkOrderCountByType>[] = [
    { id: 'type', label: 'Type', minWidth: 120 },
    { id: 'count', label: 'Count', align: 'right', minWidth: 80 },
    {
      id: 'percentage',
      label: 'Percentage',
      align: 'right',
      minWidth: 100,
      format: (value) => `${(value as number).toFixed(1)}%`,
    },
  ];

  const priorityColumns: ReportColumn<WorkOrderCountByPriority>[] = [
    { id: 'priority', label: 'Priority', minWidth: 120 },
    { id: 'count', label: 'Count', align: 'right', minWidth: 80 },
    {
      id: 'percentage',
      label: 'Percentage',
      align: 'right',
      minWidth: 100,
      format: (value) => `${(value as number).toFixed(1)}%`,
    },
  ];

  const report = data?.data;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Work Order Summary Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Work orders grouped by status, type, and priority for a date range.
      </Typography>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Filters
        </Typography>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={6} md={3}>
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
          <Grid item xs={12} sm={6} md={3}>
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
            <Button
              variant="contained"
              startIcon={<RunIcon />}
              onClick={handleRunReport}
              fullWidth
            >
              Run Report
            </Button>
          </Grid>
          {runReport && report && (
            <Grid item xs={12} sm={6} md={3}>
              <Button
                variant="outlined"
                onClick={handleExport}
                disabled={exportLoading}
                fullWidth
              >
                {exportLoading ? 'Exporting...' : 'Export CSV'}
              </Button>
            </Grid>
          )}
        </Grid>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load report. Please try again.
        </Alert>
      )}

      {runReport && report && (
        <>
          <Card sx={{ mb: 3 }}>
            <CardContent sx={{ textAlign: 'center' }}>
              <ChartIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
              <Typography color="text.secondary" gutterBottom>
                Total Work Orders
              </Typography>
              <Typography variant="h3">{report.totalWorkOrders}</Typography>
              {filter.fromDate && filter.toDate && (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                  {new Date(filter.fromDate).toLocaleDateString()} - {new Date(filter.toDate).toLocaleDateString()}
                </Typography>
              )}
            </CardContent>
          </Card>

          <Grid container spacing={3}>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="h6" gutterBottom>
                  By Status
                </Typography>
                <ReportTable
                  columns={statusColumns}
                  data={report.byStatus}
                  loading={isLoading}
                  emptyMessage="No data"
                  getRowKey={(row) => row.status}
                />
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="h6" gutterBottom>
                  By Type
                </Typography>
                <ReportTable
                  columns={typeColumns}
                  data={report.byType}
                  loading={isLoading}
                  emptyMessage="No data"
                  getRowKey={(row) => row.type}
                />
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="h6" gutterBottom>
                  By Priority
                </Typography>
                <ReportTable
                  columns={priorityColumns}
                  data={report.byPriority}
                  loading={isLoading}
                  emptyMessage="No data"
                  getRowKey={(row) => row.priority}
                />
              </Paper>
            </Grid>
          </Grid>
        </>
      )}
    </Box>
  );
};

export default WorkOrderSummaryPage;
