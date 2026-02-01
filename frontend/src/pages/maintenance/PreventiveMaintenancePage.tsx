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
  Switch,
  FormControlLabel,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as GenerateIcon,
  Schedule as ScheduleIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { preventiveMaintenanceService } from '../../services/preventiveMaintenanceService';
import { PreventiveMaintenanceScheduleFilter, FrequencyTypes } from '../../types';
import { useAuth } from '../../hooks/useAuth';

const priorityColors: Record<string, 'error' | 'warning' | 'info' | 'default'> = {
  Emergency: 'error',
  Critical: 'error',
  High: 'warning',
  Medium: 'info',
  Low: 'default',
};

const frequencyLabels: Record<string, string> = {
  Daily: 'Daily',
  Weekly: 'Weekly',
  BiWeekly: 'Bi-Weekly',
  Monthly: 'Monthly',
  Quarterly: 'Quarterly',
  SemiAnnually: 'Semi-Annually',
  Annually: 'Annually',
  Custom: 'Custom',
};

export const PreventiveMaintenancePage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const [generateResult, setGenerateResult] = useState<string | null>(null);

  const [filter, setFilter] = useState<PreventiveMaintenanceScheduleFilter>({
    page: 1,
    pageSize: 20,
    isActive: true,
  });

  const [searchInput, setSearchInput] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['pmSchedules', filter],
    queryFn: () => preventiveMaintenanceService.getSchedules(filter),
  });

  const { data: upcomingData } = useQuery({
    queryKey: ['upcomingMaintenance'],
    queryFn: () => preventiveMaintenanceService.getUpcomingMaintenance(14),
  });

  const deleteMutation = useMutation({
    mutationFn: preventiveMaintenanceService.deleteSchedule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pmSchedules'] });
    },
  });

  const generateMutation = useMutation({
    mutationFn: preventiveMaintenanceService.generateDueWorkOrders,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['pmSchedules'] });
      queryClient.invalidateQueries({ queryKey: ['workOrders'] });
      queryClient.invalidateQueries({ queryKey: ['workOrderDashboard'] });
      setGenerateResult(`Generated ${data.data?.workOrdersCreated || 0} work orders from ${data.data?.schedulesProcessed || 0} schedules`);
      setTimeout(() => setGenerateResult(null), 5000);
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
    if (window.confirm('Are you sure you want to delete this PM schedule?')) {
      deleteMutation.mutate(id);
    }
  };

  const formatFrequency = (type: string, value: number) => {
    if (value === 1) {
      return frequencyLabels[type] || type;
    }
    return `Every ${value} ${type.toLowerCase()}`;
  };

  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Name', width: 200, flex: 1 },
    { field: 'assetName', headerName: 'Asset', width: 150 },
    {
      field: 'frequency',
      headerName: 'Frequency',
      width: 150,
      valueGetter: (params: any) => formatFrequency(params.row?.frequencyType, params.row?.frequencyValue),
    },
    {
      field: 'nextDueDate',
      headerName: 'Next Due',
      width: 120,
      renderCell: (params) => {
        if (!params.value) return '-';
        const date = new Date(params.value);
        const now = new Date();
        const daysUntil = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
        const isOverdue = daysUntil < 0;
        const isDueSoon = daysUntil >= 0 && daysUntil <= 7;

        return (
          <Box>
            <Typography variant="body2" color={isOverdue ? 'error' : isDueSoon ? 'warning.main' : 'inherit'}>
              {date.toLocaleDateString()}
            </Typography>
            {isOverdue && <Typography variant="caption" color="error">Overdue</Typography>}
          </Box>
        );
      },
    },
    {
      field: 'lastCompletedDate',
      headerName: 'Last Completed',
      width: 120,
      valueFormatter: (params: any) => params.value ? new Date(params.value).toLocaleDateString() : '-',
    },
    {
      field: 'priority',
      headerName: 'Priority',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.value}
          size="small"
          color={priorityColors[params.value as string] || 'default'}
          variant="outlined"
        />
      ),
    },
    {
      field: 'isActive',
      headerName: 'Active',
      width: 80,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'Yes' : 'No'}
          size="small"
          color={params.value ? 'success' : 'default'}
        />
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 120,
      sortable: false,
      renderCell: (params) => (
        <Box>
          {hasPermission('preventive-maintenance.manage') && (
            <>
              <IconButton size="small" onClick={() => navigate(`/maintenance/pm-schedules/${params.row.id}/edit`)}>
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

  const canManage = hasPermission('preventive-maintenance.manage');

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Preventive Maintenance Schedules</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {canManage && (
            <>
              <Button
                variant="outlined"
                startIcon={<GenerateIcon />}
                onClick={() => generateMutation.mutate()}
                disabled={generateMutation.isPending}
              >
                Generate Due Work Orders
              </Button>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => navigate('/maintenance/pm-schedules/new')}
              >
                Add Schedule
              </Button>
            </>
          )}
        </Box>
      </Box>

      {generateResult && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setGenerateResult(null)}>
          {generateResult}
        </Alert>
      )}

      {upcomingData?.data && upcomingData.data.length > 0 && (
        <Paper sx={{ p: 2, mb: 2, bgcolor: 'warning.light' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <ScheduleIcon />
            <Typography variant="subtitle1">Upcoming Maintenance (Next 14 Days)</Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            {upcomingData.data.slice(0, 5).map((item) => (
              <Chip
                key={item.scheduleId}
                label={`${item.scheduleName} - ${new Date(item.dueDate).toLocaleDateString()} (${item.daysUntilDue} days)`}
                color={item.daysUntilDue <= 3 ? 'error' : 'warning'}
                variant="outlined"
              />
            ))}
            {upcomingData.data.length > 5 && (
              <Chip label={`+${upcomingData.data.length - 5} more`} variant="outlined" />
            )}
          </Box>
        </Paper>
      )}

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Search schedules..."
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
            <InputLabel>Frequency</InputLabel>
            <Select
              value={filter.frequencyType || ''}
              label="Frequency"
              onChange={(e) => setFilter({ ...filter, frequencyType: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {FrequencyTypes.map((type) => (
                <MenuItem key={type} value={type}>
                  {frequencyLabels[type] || type}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControlLabel
            control={
              <Switch
                checked={filter.isActive !== false}
                onChange={(e) => setFilter({ ...filter, isActive: e.target.checked ? true : undefined, page: 1 })}
              />
            }
            label="Active Only"
          />
        </Box>
      </Paper>

      <Paper sx={{ height: 600 }}>
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
        />
      </Paper>
    </Box>
  );
};
