import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Paper,
  Typography,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  IconButton,
  Chip,
  InputAdornment,
  Grid,
  Card,
  CardContent,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workOrderService } from '../../services/workOrderService';
import {
  WorkOrderFilter,
  WorkOrderTypes,
  WorkOrderStatuses,
  WorkOrderPriorities,
} from '../../types';
import { useAuth } from '../../hooks/useAuth';

const statusColors: Record<string, 'success' | 'warning' | 'error' | 'default' | 'info' | 'primary'> = {
  Draft: 'default',
  Open: 'info',
  InProgress: 'primary',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'error',
};

const priorityColors: Record<string, 'error' | 'warning' | 'info' | 'default' | 'primary'> = {
  Emergency: 'error',
  Critical: 'error',
  High: 'warning',
  Medium: 'info',
  Low: 'default',
};

const typeLabels: Record<string, string> = {
  Repair: 'Repair',
  ScheduledJob: 'Scheduled Job',
  SafetyInspection: 'Safety Inspection',
  PreventiveMaintenance: 'PM',
};

export const WorkOrdersPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();

  const [filter, setFilter] = useState<WorkOrderFilter>({
    page: 1,
    pageSize: 20,
  });

  const [searchInput, setSearchInput] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['workOrders', filter],
    queryFn: () => workOrderService.getWorkOrders(filter),
  });

  const { data: dashboard } = useQuery({
    queryKey: ['workOrderDashboard'],
    queryFn: () => workOrderService.getDashboard(),
  });

  const deleteMutation = useMutation({
    mutationFn: workOrderService.deleteWorkOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workOrders'] });
      queryClient.invalidateQueries({ queryKey: ['workOrderDashboard'] });
    },
  });

  const handleSearch = () => {
    setFilter({ ...filter, search: searchInput, page: 1 });
  };

  const handlePaginationChange = (model: GridPaginationModel) => {
    setFilter({
      ...filter,
      page: model.page + 1,
      pageSize: model.pageSize,
    });
  };

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this work order?')) {
      deleteMutation.mutate(id);
    }
  };

  const columns: GridColDef[] = [
    {
      field: 'actions',
      headerName: '',
      width: 120,
      sortable: false,
      renderCell: (params) => (
        <Box onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => navigate(`/maintenance/work-orders/${params.row.id}`)}>
            <ViewIcon fontSize="small" />
          </IconButton>
          {hasPermission('work-orders.edit') && (
            <IconButton size="small" onClick={() => navigate(`/maintenance/work-orders/${params.row.id}/edit`)}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {hasPermission('work-orders.delete') && (
            <IconButton size="small" onClick={() => handleDelete(params.row.id)} color="error">
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </Box>
      ),
    },
    { field: 'workOrderNumber', headerName: 'WO #', width: 150 },
    { field: 'title', headerName: 'Title', width: 250, flex: 1 },
    {
      field: 'type',
      headerName: 'Type',
      width: 130,
      renderCell: (params) => typeLabels[params.value as string] || params.value,
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.value}
          size="small"
          color={statusColors[params.value as string] || 'default'}
        />
      ),
    },
    {
      field: 'priority',
      headerName: 'Priority',
      width: 110,
      renderCell: (params) => (
        <Chip
          label={params.value}
          size="small"
          color={priorityColors[params.value as string] || 'default'}
          variant="outlined"
        />
      ),
    },
    { field: 'assetName', headerName: 'Asset', width: 150 },
    { field: 'assignedToName', headerName: 'Assigned To', width: 150 },
    {
      field: 'scheduledStartDate',
      headerName: 'Scheduled',
      width: 120,
      valueFormatter: (params: any) =>
        params.value ? new Date(params.value).toLocaleDateString() : '-',
    },
  ];

  const dashboardData = dashboard?.data;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Work Orders</Typography>
        {hasPermission('work-orders.create') && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => navigate('/maintenance/work-orders/new')}
          >
            Create Work Order
          </Button>
        )}
      </Box>

      {/* Dashboard Summary Cards */}
      {dashboardData && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={2}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 1 }}>
                <Typography variant="h4" color="info.main">
                  {dashboardData.byStatus?.Open || 0}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Open
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 1 }}>
                <Typography variant="h4" color="primary.main">
                  {dashboardData.byStatus?.InProgress || 0}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  In Progress
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 1 }}>
                <Typography variant="h4" color="warning.main">
                  {dashboardData.byStatus?.OnHold || 0}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  On Hold
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 1 }}>
                <Typography variant="h4" color="error.main">
                  {dashboardData.overdueCount || 0}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Overdue
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 1 }}>
                <Typography variant="h4" color="text.secondary">
                  {dashboardData.dueThisWeekCount || 0}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Due This Week
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 1 }}>
                <Typography variant="h4" color="success.main">
                  {dashboardData.byStatus?.Completed || 0}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Completed
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <TextField
            size="small"
            placeholder="Search work orders..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton size="small" onClick={handleSearch}>
                    <SearchIcon />
                  </IconButton>
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 250 }}
          />

          <FormControl size="small" sx={{ minWidth: 140 }}>
            <InputLabel>Type</InputLabel>
            <Select
              value={filter.type || ''}
              label="Type"
              onChange={(e) => setFilter({ ...filter, type: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {WorkOrderTypes.map((type) => (
                <MenuItem key={type} value={type}>
                  {typeLabels[type] || type}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={filter.status || ''}
              label="Status"
              onChange={(e) => setFilter({ ...filter, status: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {WorkOrderStatuses.map((status) => (
                <MenuItem key={status} value={status}>
                  {status}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Priority</InputLabel>
            <Select
              value={filter.priority || ''}
              label="Priority"
              onChange={(e) => setFilter({ ...filter, priority: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {WorkOrderPriorities.map((priority) => (
                <MenuItem key={priority} value={priority}>
                  {priority}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>
      </Paper>

      <Paper sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <DataGrid
          rows={data?.items || []}
          columns={columns}
          loading={isLoading}
          paginationMode="server"
          rowCount={data?.totalCount || 0}
          paginationModel={{
            page: (filter.page || 1) - 1,
            pageSize: filter.pageSize || 20,
          }}
          onPaginationModelChange={handlePaginationChange}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
          onRowClick={(params) => navigate(`/maintenance/work-orders/${params.row.id}`)}
          sx={{ flex: 1, '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>
    </Box>
  );
};
