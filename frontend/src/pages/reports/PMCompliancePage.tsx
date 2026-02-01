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
  LinearProgress,
} from '@mui/material';
import { PlayArrow as RunIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import { assetService } from '../../services/assetService';
import { PMComplianceFilter, PMComplianceItem } from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

const getComplianceColor = (rate: number): 'success' | 'warning' | 'error' => {
  if (rate >= 90) return 'success';
  if (rate >= 70) return 'warning';
  return 'error';
};

export const PMCompliancePage: React.FC = () => {
  const [filter, setFilter] = useState<PMComplianceFilter>({
    fromDate: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0],
  });
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);

  const { data: assetsData } = useQuery({
    queryKey: ['assets-list'],
    queryFn: () => assetService.getAssets({ pageSize: 100 }),
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['pm-compliance-report', filter],
    queryFn: () => reportService.getPMComplianceReport(filter),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportPMComplianceReport(filter);
      downloadCsv(blob, 'pm-compliance-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const columns: ReportColumn<PMComplianceItem>[] = [
    { id: 'scheduleName', label: 'Schedule Name', minWidth: 150 },
    { id: 'assetName', label: 'Asset', minWidth: 120 },
    { id: 'frequencyDescription', label: 'Frequency', minWidth: 100 },
    { id: 'scheduledCount', label: 'Scheduled', align: 'right', minWidth: 90 },
    { id: 'completedCount', label: 'Completed', align: 'right', minWidth: 90 },
    { id: 'missedCount', label: 'Missed', align: 'right', minWidth: 80 },
    {
      id: 'complianceRate',
      label: 'Compliance',
      align: 'right',
      minWidth: 150,
      format: (value) => {
        const rate = value as number;
        return (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <LinearProgress
              variant="determinate"
              value={rate}
              color={getComplianceColor(rate)}
              sx={{ flex: 1, height: 8, borderRadius: 4 }}
            />
            <Typography variant="body2" sx={{ minWidth: 45 }}>
              {rate.toFixed(1)}%
            </Typography>
          </Box>
        );
      },
    },
  ];

  const report = data?.data;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        PM Compliance Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Scheduled vs completed preventive maintenance. Track compliance rate by schedule.
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
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Total Scheduled
                  </Typography>
                  <Typography variant="h4">{report.totalScheduled}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Completed
                  </Typography>
                  <Typography variant="h4" color="success.main">
                    {report.totalCompleted}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Missed
                  </Typography>
                  <Typography variant="h4" color="error.main">
                    {report.totalMissed}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Overall Compliance
                  </Typography>
                  <Typography
                    variant="h4"
                    color={`${getComplianceColor(report.complianceRate)}.main`}
                  >
                    {report.complianceRate.toFixed(1)}%
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
            emptyMessage="No PM schedules found"
            getRowKey={(row) => row.scheduleId}
          />
        </>
      )}
    </Box>
  );
};

export default PMCompliancePage;
