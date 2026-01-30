import React, { useState } from 'react';
import {
  Box,
  Button,
  Paper,
  Typography,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Grid,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Collapse,
  Chip,
  MenuItem,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Warehouse as WarehouseIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { storageLocationService } from '../../services/storageLocationService';
import { StorageLocation, CreateStorageLocationRequest } from '../../types';
import { useAuth } from '../../hooks/useAuth';

interface LocationItemProps {
  location: StorageLocation;
  level: number;
  onEdit: (location: StorageLocation) => void;
  onDelete: (id: number) => void;
  canManage: boolean;
}

const LocationItem: React.FC<LocationItemProps> = ({
  location,
  level,
  onEdit,
  onDelete,
  canManage,
}) => {
  const [open, setOpen] = useState(true);
  const hasChildren = location.children && location.children.length > 0;

  const locationDetails = [
    location.building && `Building: ${location.building}`,
    location.aisle && `Aisle: ${location.aisle}`,
    location.rack && `Rack: ${location.rack}`,
    location.shelf && `Shelf: ${location.shelf}`,
    location.bin && `Bin: ${location.bin}`,
  ].filter(Boolean).join(' | ');

  return (
    <>
      <ListItem
        sx={{
          pl: 2 + level * 3,
          bgcolor: level % 2 === 0 ? 'background.paper' : 'action.hover',
        }}
      >
        {hasChildren ? (
          <IconButton size="small" onClick={() => setOpen(!open)} sx={{ mr: 1 }}>
            {open ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
        ) : (
          <WarehouseIcon sx={{ mr: 1, color: 'action.active' }} fontSize="small" />
        )}
        <ListItemText
          primary={
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography>{location.name}</Typography>
              {location.code && (
                <Chip label={location.code} size="small" variant="outlined" />
              )}
              {!location.isActive && (
                <Chip label="Inactive" size="small" color="default" />
              )}
            </Box>
          }
          secondary={locationDetails || location.description}
        />
        <ListItemSecondaryAction>
          <Typography variant="body2" color="text.secondary" sx={{ mr: 2 }}>
            {location.stockItemCount} items
          </Typography>
          {canManage && (
            <>
              <IconButton size="small" onClick={() => onEdit(location)}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton
                size="small"
                onClick={() => onDelete(location.id)}
                color="error"
                disabled={location.stockItemCount > 0}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </>
          )}
        </ListItemSecondaryAction>
      </ListItem>
      {hasChildren && (
        <Collapse in={open}>
          {location.children.map((child) => (
            <LocationItem
              key={child.id}
              location={child}
              level={level + 1}
              onEdit={onEdit}
              onDelete={onDelete}
              canManage={canManage}
            />
          ))}
        </Collapse>
      )}
    </>
  );
};

export const StorageLocationsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const canManage = hasPermission('inventory.manage');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingLocation, setEditingLocation] = useState<StorageLocation | null>(null);
  const [formData, setFormData] = useState<CreateStorageLocationRequest>({
    name: '',
    isActive: true,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['storageLocations', true],
    queryFn: () => storageLocationService.getLocations(true),
  });

  const createMutation = useMutation({
    mutationFn: storageLocationService.createLocation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['storageLocations'] });
      handleCloseDialog();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreateStorageLocationRequest }) =>
      storageLocationService.updateLocation(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['storageLocations'] });
      handleCloseDialog();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: storageLocationService.deleteLocation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['storageLocations'] });
    },
  });

  const handleOpenCreate = (parentId?: number) => {
    setEditingLocation(null);
    setFormData({ name: '', parentId, isActive: true });
    setDialogOpen(true);
  };

  const handleOpenEdit = (location: StorageLocation) => {
    setEditingLocation(location);
    setFormData({
      name: location.name,
      code: location.code,
      description: location.description,
      parentId: location.parentId,
      building: location.building,
      aisle: location.aisle,
      rack: location.rack,
      shelf: location.shelf,
      bin: location.bin,
      isActive: location.isActive,
    });
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingLocation(null);
    setFormData({ name: '', isActive: true });
  };

  const handleSubmit = () => {
    if (editingLocation) {
      updateMutation.mutate({ id: editingLocation.id, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this location?')) {
      deleteMutation.mutate(id);
    }
  };

  const flattenLocations = (locations: StorageLocation[], level = 0): (StorageLocation & { indent: number })[] => {
    return locations.reduce((acc, loc) => {
      acc.push({ ...loc, indent: level });
      if (loc.children?.length) {
        acc.push(...flattenLocations(loc.children, level + 1));
      }
      return acc;
    }, [] as (StorageLocation & { indent: number })[]);
  };

  const flatLocations = flattenLocations(data?.data || []);
  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Storage Locations</Typography>
        {canManage && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => handleOpenCreate()}>
            Add Location
          </Button>
        )}
      </Box>

      <Paper>
        {isLoading ? (
          <Box sx={{ p: 3 }}>
            <Typography>Loading...</Typography>
          </Box>
        ) : data?.data?.length ? (
          <List disablePadding>
            {data.data.map((location) => (
              <LocationItem
                key={location.id}
                location={location}
                level={0}
                onEdit={handleOpenEdit}
                onDelete={handleDelete}
                canManage={canManage}
              />
            ))}
          </List>
        ) : (
          <Box sx={{ p: 3, textAlign: 'center' }}>
            <Typography color="text.secondary">No locations yet</Typography>
          </Box>
        )}
      </Paper>

      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingLocation ? 'Edit Location' : 'New Location'}</DialogTitle>
        <DialogContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {(error as any)?.response?.data?.errors?.[0] || 'An error occurred'}
            </Alert>
          )}
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Name"
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Code"
                value={formData.code || ''}
                onChange={(e) => setFormData({ ...formData, code: e.target.value })}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                label="Parent Location"
                value={formData.parentId || ''}
                onChange={(e) => setFormData({ ...formData, parentId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">None (Top Level)</MenuItem>
                {flatLocations
                  .filter((l) => l.id !== editingLocation?.id)
                  .map((loc) => (
                    <MenuItem key={loc.id} value={loc.id}>
                      {'  '.repeat(loc.indent)}{loc.name}
                    </MenuItem>
                  ))}
              </TextField>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                multiline
                rows={2}
                label="Description"
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Building"
                value={formData.building || ''}
                onChange={(e) => setFormData({ ...formData, building: e.target.value })}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Aisle"
                value={formData.aisle || ''}
                onChange={(e) => setFormData({ ...formData, aisle: e.target.value })}
              />
            </Grid>
            <Grid item xs={4}>
              <TextField
                fullWidth
                label="Rack"
                value={formData.rack || ''}
                onChange={(e) => setFormData({ ...formData, rack: e.target.value })}
              />
            </Grid>
            <Grid item xs={4}>
              <TextField
                fullWidth
                label="Shelf"
                value={formData.shelf || ''}
                onChange={(e) => setFormData({ ...formData, shelf: e.target.value })}
              />
            </Grid>
            <Grid item xs={4}>
              <TextField
                fullWidth
                label="Bin"
                value={formData.bin || ''}
                onChange={(e) => setFormData({ ...formData, bin: e.target.value })}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            onClick={handleSubmit}
            variant="contained"
            disabled={isSubmitting || !formData.name}
          >
            {isSubmitting ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
