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
  Alert,
  Tabs,
  Tab,
} from '@mui/material';
import { PlayArrow as RunIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../../services/reportService';
import { partCategoryService } from '../../services/partCategoryService';
import { partService } from '../../services/partService';
import {
  InventoryValuationFilter,
  InventoryValuationItem,
  ValuationByCategory,
  ValuationByLocation,
} from '../../types';
import ReportTable, { ReportColumn } from '../../components/reports/ReportTable';
import { downloadCsv } from '../../utils/csvExport';

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

export const InventoryValuationPage: React.FC = () => {
  const [filter, setFilter] = useState<InventoryValuationFilter>({});
  const [runReport, setRunReport] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);
  const [tabValue, setTabValue] = useState(0);

  const { data: categoriesData } = useQuery({
    queryKey: ['partCategories'],
    queryFn: () => partCategoryService.getCategories(),
  });

  const { data: locationsData } = useQuery({
    queryKey: ['storageLocations'],
    queryFn: () => partService.getStorageLocations(),
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['inventory-valuation-report', filter],
    queryFn: () => reportService.getInventoryValuationReport(filter),
    enabled: runReport,
  });

  const handleRunReport = () => {
    setRunReport(true);
  };

  const handleExport = async () => {
    setExportLoading(true);
    try {
      const blob = await reportService.exportInventoryValuationReport(filter);
      downloadCsv(blob, 'inventory-valuation-report.csv');
    } catch (err) {
      console.error('Export failed:', err);
    } finally {
      setExportLoading(false);
    }
  };

  const itemColumns: ReportColumn<InventoryValuationItem>[] = [
    { id: 'partNumber', label: 'Part Number', minWidth: 120 },
    { id: 'name', label: 'Name', minWidth: 150 },
    { id: 'categoryName', label: 'Category', minWidth: 100 },
    { id: 'quantityOnHand', label: 'Qty On Hand', align: 'right', minWidth: 100 },
    {
      id: 'unitCost',
      label: 'Unit Cost',
      align: 'right',
      minWidth: 100,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
    {
      id: 'totalValue',
      label: 'Total Value',
      align: 'right',
      minWidth: 120,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
  ];

  const categoryColumns: ReportColumn<ValuationByCategory>[] = [
    { id: 'categoryName', label: 'Category', minWidth: 150 },
    { id: 'partCount', label: 'Parts', align: 'right', minWidth: 80 },
    { id: 'totalQuantity', label: 'Total Qty', align: 'right', minWidth: 100 },
    {
      id: 'totalValue',
      label: 'Total Value',
      align: 'right',
      minWidth: 120,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
  ];

  const locationColumns: ReportColumn<ValuationByLocation>[] = [
    { id: 'locationName', label: 'Location', minWidth: 150 },
    { id: 'partCount', label: 'Parts', align: 'right', minWidth: 80 },
    { id: 'totalQuantity', label: 'Total Qty', align: 'right', minWidth: 100 },
    {
      id: 'totalValue',
      label: 'Total Value',
      align: 'right',
      minWidth: 120,
      format: (value) => `$${(value as number).toFixed(2)}`,
    },
  ];

  const report = data?.data;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Inventory Valuation Report
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Total value of inventory broken down by category and location.
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

      {runReport && report && (
        <>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={6} sm={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Total Parts
                  </Typography>
                  <Typography variant="h4">{report.totalParts}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Total Quantity
                  </Typography>
                  <Typography variant="h4">{report.totalQuantity.toLocaleString()}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={4}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    Total Value
                  </Typography>
                  <Typography variant="h4" color="primary.main">
                    ${report.totalValue.toFixed(2)}
                  </Typography>
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
              <Tab label={`By Part (${report.items.length})`} />
              <Tab label={`By Category (${report.byCategory.length})`} />
              <Tab label={`By Location (${report.byLocation.length})`} />
            </Tabs>

            <TabPanel value={tabValue} index={0}>
              <ReportTable
                columns={itemColumns}
                data={report.items}
                loading={isLoading}
                onExport={handleExport}
                exportLoading={exportLoading}
                emptyMessage="No inventory data"
                getRowKey={(row) => row.partId}
              />
            </TabPanel>

            <TabPanel value={tabValue} index={1}>
              <ReportTable
                columns={categoryColumns}
                data={report.byCategory}
                loading={isLoading}
                emptyMessage="No category data"
                getRowKey={(row) => row.categoryId || 'uncategorized'}
              />
            </TabPanel>

            <TabPanel value={tabValue} index={2}>
              <ReportTable
                columns={locationColumns}
                data={report.byLocation}
                loading={isLoading}
                emptyMessage="No location data"
                getRowKey={(row) => row.locationId || 'unknown'}
              />
            </TabPanel>
          </Paper>
        </>
      )}
    </Box>
  );
};

export default InventoryValuationPage;
