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
import { assetService } from '../services/assetService';
import { Asset, AssetStatuses, AssetCriticalities, AssetFilter } from '../types';
import { useAuth } from '../hooks/useAuth';

const statusColors: Record<string, 'success' | 'warning' | 'error' | 'default' | 'info'> = {
  Active: 'success',
  Inactive: 'default',
  InMaintenance: 'warning',
  Retired: 'info',
  Disposed: 'error',
};

const criticalityColors: Record<string, 'error' | 'warning' | 'info' | 'default'> = {
  Critical: 'error',
  High: 'warning',
  Medium: 'info',
  Low: 'default',
};

export const AssetsPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();

  const [filter, setFilter] = useState<AssetFilter>({
    page: 1,
    pageSize: 20,
  });

  const [searchInput, setSearchInput] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['assets', filter],
    queryFn: () => assetService.getAssets(filter),
  });

  const deleteMutation = useMutation({
    mutationFn: assetService.deleteAsset,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assets'] });
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
    if (window.confirm('Are you sure you want to delete this asset?')) {
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
          <IconButton size="small" onClick={() => navigate(`/assets/${params.row.id}`)}>
            <ViewIcon fontSize="small" />
          </IconButton>
          {hasPermission('assets.edit') && (
            <IconButton size="small" onClick={() => navigate(`/assets/${params.row.id}/edit`)}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {hasPermission('assets.delete') && (
            <IconButton size="small" onClick={() => handleDelete(params.row.id)} color="error">
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </Box>
      ),
    },
    { field: 'assetTag', headerName: 'Asset Tag', width: 130 },
    { field: 'name', headerName: 'Name', width: 200, flex: 1 },
    { field: 'categoryName', headerName: 'Category', width: 150 },
    { field: 'locationName', headerName: 'Location', width: 180 },
    {
      field: 'status',
      headerName: 'Status',
      width: 130,
      renderCell: (params) => (
        <Chip
          label={params.value}
          size="small"
          color={statusColors[params.value as string] || 'default'}
        />
      ),
    },
    {
      field: 'criticality',
      headerName: 'Criticality',
      width: 110,
      renderCell: (params) => (
        <Chip
          label={params.value}
          size="small"
          color={criticalityColors[params.value as string] || 'default'}
          variant="outlined"
        />
      ),
    },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Assets</Typography>
        {hasPermission('assets.create') && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => navigate('/assets/new')}
          >
            Add Asset
          </Button>
        )}
      </Box>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <TextField
            size="small"
            placeholder="Search assets..."
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

          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={filter.status || ''}
              label="Status"
              onChange={(e) => setFilter({ ...filter, status: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {AssetStatuses.map((status) => (
                <MenuItem key={status} value={status}>
                  {status}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Criticality</InputLabel>
            <Select
              value={filter.criticality || ''}
              label="Criticality"
              onChange={(e) => setFilter({ ...filter, criticality: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {AssetCriticalities.map((crit) => (
                <MenuItem key={crit} value={crit}>
                  {crit}
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
          onRowClick={(params) => navigate(`/assets/${params.row.id}`)}
          sx={{ flex: 1, '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>
    </Box>
  );
};
