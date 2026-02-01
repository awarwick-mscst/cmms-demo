import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Grid,
  Card,
  CardContent,
  Chip,
  Alert,
  Tabs,
  Tab,
} from '@mui/material';
import { PlayArrow as RunIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import { OverduePMSchedule, OverdueWorkOrder } from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

const priorityColors: Record<string, 'error' | 'warning' | 'info' | 'success' | 'default'> = {
  Emergency: 'error',
  Critical: 'error',
  High: 'warning',
  Medium: 'info',
  Low: 'success',
};

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => {
  return (
    <div hidden={value !== index}>
      {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
    </div>
  );
};

export const OverdueMaintenancePage: React.FC = () => {
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);
  const [tabValue, setTabValue] = useState(0);

  const { data, isLoading, error } = useQuery({
    queryKey: ['overdue-maintenance-report'],
    queryFn: () => reportService.getOverdueMaintenanceReport(),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportOverdueMaintenanceReport();
      downloadCsv(blob, 'overdue-maintenance-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const pmColumns: ReportColumn<OverduePMSchedule>[] = [
    { id: 'scheduleName', label: 'Schedule Name', minWidth: 150 },
    { id: 'assetName', label: 'Asset', minWidth: 120 },
    { id: 'frequencyDescription', label: 'Frequency', minWidth: 100 },
    {
      id: 'priority',
      label: 'Priority',
      minWidth: 90,
      format: (value) => (
        <Chip
          label={value as string}
          color={priorityColors[value as string] || 'default'}
          size="small"
        />
      ),
    },
    {
      id: 'dueDate',
      label: 'Due Date',
      minWidth: 100,
      format: (value) => new Date(value as string).toLocaleDateString(),
    },
    {
      id: 'daysOverdue',
      label: 'Days Overdue',
      align: 'right',
      minWidth: 100,
      format: (value) => (
        <Typography color="error.main" fontWeight="bold">
          {value as number}
        </Typography>
      ),
    },
  ];

  const woColumns: ReportColumn<OverdueWorkOrder>[] = [
    { id: 'workOrderNumber', label: 'WO Number', minWidth: 120 },
    { id: 'title', label: 'Title', minWidth: 150 },
    { id: 'type', label: 'Type', minWidth: 100 },
    { id: 'assetName', label: 'Asset', minWidth: 120 },
    {
      id: 'priority',
      label: 'Priority',
      minWidth: 90,
      format: (value) => (
        <Chip
          label={value as string}
          color={priorityColors[value as string] || 'default'}
          size="small"
        />
      ),
    },
    { id: 'status', label: 'Status', minWidth: 90 },
    { id: 'assignedToName', label: 'Assigned To', minWidth: 120 },
    {
      id: 'scheduledEndDate',
      label: 'Scheduled End',
      minWidth: 100,
      format: (value) => (value ? new Date(value as string).toLocaleDateString() : ''),
    },
    {
      id: 'daysOverdue',
      label: 'Days Overdue',
      align: 'right',
      minWidth: 100,
      format: (value) => (
        <Typography color="error.main" fontWeight="bold">
          {value as number}
        </Typography>
      ),
    },
  ];

  const report = data?.data;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Overdue Maintenance Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        PM schedules and work orders that are past their due date and require immediate attention.
      </Typography>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={4} md={3}>
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
            <Grid item xs={12} sm={4} md={3}>
              <Button
                variant="outlined"
                onClick={handleExport}
                disabled={exportLoading || report.totalOverdue === 0}
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
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={6} sm={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Total Overdue
                  </Typography>
                  <Typography variant="h4" color="error.main">
                    {report.totalOverdue}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Overdue PM Schedules
                  </Typography>
                  <Typography variant="h4">{report.overduePMCount}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Overdue Work Orders
                  </Typography>
                  <Typography variant="h4">{report.overdueWorkOrderCount}</Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Paper sx={{ mb: 3 }}>
            <Tabs
              value={tabValue}
              onChange={(_, newValue) => setTabValue(newValue)}
              sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}
            >
              <Tab label={`PM Schedules (${report.overduePMCount})`} />
              <Tab label={`Work Orders (${report.overdueWorkOrderCount})`} />
            </Tabs>

            <TabPanel value={tabValue} index={0}>
              <ReportTable
                columns={pmColumns}
                data={report.overduePMSchedules}
                loading={isLoading}
                emptyMessage="No overdue PM schedules"
                getRowKey={(row) => row.scheduleId}
              />
            </TabPanel>

            <TabPanel value={tabValue} index={1}>
              <ReportTable
                columns={woColumns}
                data={report.overdueWorkOrders}
                loading={isLoading}
                emptyMessage="No overdue work orders"
                getRowKey={(row) => row.workOrderId}
              />
            </TabPanel>
          </Paper>
        </>
      )}
    </Box>
  );
};

export default OverdueMaintenancePage;
