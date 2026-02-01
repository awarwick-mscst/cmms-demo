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
  Chip,
  Alert,
} from '@mui/material';
import { PlayArrow as RunIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import { partCategoryService } from '../../services/partCategoryService';
import { supplierService } from '../../services/supplierService';
import { ReorderReportFilter, ReorderReportItem } from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

const statusColors: Record<string, 'error' | 'warning' | 'info'> = {
  OutOfStock: 'error',
  Critical: 'error',
  Low: 'warning',
};

export const ReorderReportPage: React.FC = () => {
  const [filter, setFilter] = useState<ReorderReportFilter>({});
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);

  const { data: categoriesData } = useQuery({
    queryKey: ['partCategories'],
    queryFn: () => partCategoryService.getCategories(),
  });

  const { data: suppliersData } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers({ pageSize: 100 }),
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['reorder-report', filter],
    queryFn: () => reportService.getReorderReport(filter),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportReorderReport(filter);
      downloadCsv(blob, 'reorder-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const columns: ReportColumn<ReorderReportItem>[] = [
    { id: 'partNumber', label: 'Part Number', minWidth: 120 },
    { id: 'name', label: 'Name', minWidth: 150 },
    { id: 'categoryName', label: 'Category', minWidth: 100 },
    { id: 'supplierName', label: 'Supplier', minWidth: 100 },
    {
      id: 'reorderStatus',
      label: 'Status',
      minWidth: 100,
      format: (value) => (
        <Chip
          label={value as string}
          color={statusColors[value as string] || 'default'}
          size="small"
        />
      ),
    },
    { id: 'quantityOnHand', label: 'On Hand', align: 'right', minWidth: 80 },
    { id: 'quantityAvailable', label: 'Available', align: 'right', minWidth: 80 },
    { id: 'reorderPoint', label: 'Reorder Point', align: 'right', minWidth: 100 },
    { id: 'quantityToOrder', label: 'Qty to Order', align: 'right', minWidth: 100 },
    {
      id: 'unitCost',
      label: 'Unit Cost',
      align: 'right',
      minWidth: 90,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
    {
      id: 'estimatedCost',
      label: 'Est. Cost',
      align: 'right',
      minWidth: 100,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
    { id: 'leadTimeDays', label: 'Lead Time', align: 'right', minWidth: 90 },
  ];

  const reportData = data?.data || [];
  const totalEstimatedCost = reportData.reduce((sum, item) => sum + item.estimatedCost, 0);
  const outOfStockCount = reportData.filter((i) => i.reorderStatus === 'OutOfStock').length;
  const criticalCount = reportData.filter((i) => i.reorderStatus === 'Critical').length;
  const lowCount = reportData.filter((i) => i.reorderStatus === 'Low').length;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Reorder Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Parts that are at or below their reorder point and need to be ordered.
      </Typography>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Filters
        </Typography>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={4} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Category</InputLabel>
              <Select
                value={filter.categoryId || ''}
                label="Category"
                onChange={(e) => {
                  setFilter({ ...filter, categoryId: e.target.value as number || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All Categories</MenuItem>
                {categoriesData?.data?.map((cat) => (
                  <MenuItem key={cat.id} value={cat.id}>
                    {cat.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={4} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Supplier</InputLabel>
              <Select
                value={filter.supplierId || ''}
                label="Supplier"
                onChange={(e) => {
                  setFilter({ ...filter, supplierId: e.target.value as number || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All Suppliers</MenuItem>
                {suppliersData?.items?.map((sup) => (
                  <MenuItem key={sup.id} value={sup.id}>
                    {sup.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={4} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Status</InputLabel>
              <Select
                value={filter.status || ''}
                label="Status"
                onChange={(e) => {
                  setFilter({ ...filter, status: e.target.value || undefined });
                  setRunReport(false);
                }}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="OutOfStock">Out of Stock</MenuItem>
                <MenuItem value="Critical">Critical</MenuItem>
                <MenuItem value="Low">Low</MenuItem>
              </Select>
            </FormControl>
          </Grid>
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
        </Grid>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load report. Please try again.
        </Alert>
      )}

      {runReport && data?.data && (
        <>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Total Items
                  </Typography>
                  <Typography variant="h4">{reportData.length}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Out of Stock
                  </Typography>
                  <Typography variant="h4" color="error.main">
                    {outOfStockCount}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Critical / Low
                  </Typography>
                  <Typography variant="h4" color="warning.main">
                    {criticalCount} / {lowCount}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Est. Order Cost
                  </Typography>
                  <Typography variant="h4">${totalEstimatedCost.toFixed(2)}</Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <ReportTable
            columns={columns}
            data={reportData}
            loading={isLoading}
            onExport={handleExport}
            exportLoading={exportLoading}
            emptyMessage="No items need reordering"
            getRowKey={(row) => row.partId}
          />
        </>
      )}
    </Box>
  );
};

export default ReorderReportPage;
