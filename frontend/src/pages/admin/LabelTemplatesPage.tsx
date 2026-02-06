import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Paper,
  Typography,
  IconButton,
  Chip,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { labelService } from '../../services/labelService';
import { LabelTemplate } from '../../types';
import { useAuth } from '../../hooks/useAuth';

export const LabelTemplatesPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const canManage = hasPermission('labels.manage') || hasPermission('Administrator');

  const { data, isLoading } = useQuery({
    queryKey: ['label-templates'],
    queryFn: () => labelService.getTemplates(),
  });

  const deleteMutation = useMutation({
    mutationFn: labelService.deleteTemplate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['label-templates'] });
    },
  });

  const setDefaultMutation = useMutation({
    mutationFn: labelService.setDefaultTemplate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['label-templates'] });
    },
  });

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this template?')) {
      deleteMutation.mutate(id);
    }
  };

  const handleSetDefault = (id: number) => {
    setDefaultMutation.mutate(id);
  };

  const parseElementCount = (elementsJson: string): number => {
    try {
      const elements = JSON.parse(elementsJson);
      return Array.isArray(elements) ? elements.length : 0;
    } catch {
      return 0;
    }
  };

  const columns: GridColDef[] = [
    {
      field: 'actions',
      headerName: '',
      width: 80,
      sortable: false,
      renderCell: (params) => (
        <Box onClick={(e) => e.stopPropagation()}>
          {canManage && (
            <>
              <IconButton
                size="small"
                onClick={() => navigate(`/admin/label-templates/${params.row.id}/edit`)}
              >
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
    { field: 'name', headerName: 'Name', width: 200, flex: 1 },
    { field: 'description', headerName: 'Description', width: 250 },
    {
      field: 'size',
      headerName: 'Size (inches)',
      width: 120,
      renderCell: (params) => `${params.row.width}" x ${params.row.height}"`,
    },
    { field: 'dpi', headerName: 'DPI', width: 80 },
    {
      field: 'elements',
      headerName: 'Elements',
      width: 100,
      renderCell: (params) => parseElementCount(params.row.elementsJson),
    },
    {
      field: 'isDefault',
      headerName: 'Default',
      width: 80,
      renderCell: (params) =>
        params.value ? (
          <StarIcon color="primary" fontSize="small" />
        ) : canManage ? (
          <IconButton size="small" onClick={(e) => { e.stopPropagation(); handleSetDefault(params.row.id); }}>
            <StarBorderIcon fontSize="small" />
          </IconButton>
        ) : null,
    },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Label Templates</Typography>
        {canManage && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => navigate('/admin/label-templates/new')}
          >
            New Template
          </Button>
        )}
      </Box>

      <Paper sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <DataGrid
          rows={data?.data || []}
          columns={columns}
          loading={isLoading}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
          onRowClick={(params) => navigate(`/admin/label-templates/${params.row.id}/edit`)}
          sx={{ flex: 1, '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>
    </Box>
  );
};
