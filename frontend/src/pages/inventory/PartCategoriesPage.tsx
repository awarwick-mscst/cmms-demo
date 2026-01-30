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
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { partCategoryService } from '../../services/partCategoryService';
import { PartCategory, CreatePartCategoryRequest } from '../../types';
import { useAuth } from '../../hooks/useAuth';

interface CategoryItemProps {
  category: PartCategory;
  level: number;
  onEdit: (category: PartCategory) => void;
  onDelete: (id: number) => void;
  canManage: boolean;
  allCategories: PartCategory[];
}

const CategoryItem: React.FC<CategoryItemProps> = ({
  category,
  level,
  onEdit,
  onDelete,
  canManage,
  allCategories,
}) => {
  const [open, setOpen] = useState(true);
  const hasChildren = category.children && category.children.length > 0;

  return (
    <>
      <ListItem
        sx={{
          pl: 2 + level * 3,
          bgcolor: level % 2 === 0 ? 'background.paper' : 'action.hover',
        }}
      >
        {hasChildren && (
          <IconButton size="small" onClick={() => setOpen(!open)} sx={{ mr: 1 }}>
            {open ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
        )}
        {!hasChildren && <Box sx={{ width: 40 }} />}
        <ListItemText
          primary={
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography>{category.name}</Typography>
              {category.code && (
                <Chip label={category.code} size="small" variant="outlined" />
              )}
              {!category.isActive && (
                <Chip label="Inactive" size="small" color="default" />
              )}
            </Box>
          }
          secondary={category.description}
        />
        <ListItemSecondaryAction>
          <Typography variant="body2" color="text.secondary" sx={{ mr: 2 }}>
            {category.partCount} parts
          </Typography>
          {canManage && (
            <>
              <IconButton size="small" onClick={() => onEdit(category)}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton
                size="small"
                onClick={() => onDelete(category.id)}
                color="error"
                disabled={category.partCount > 0}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </>
          )}
        </ListItemSecondaryAction>
      </ListItem>
      {hasChildren && (
        <Collapse in={open}>
          {category.children.map((child) => (
            <CategoryItem
              key={child.id}
              category={child}
              level={level + 1}
              onEdit={onEdit}
              onDelete={onDelete}
              canManage={canManage}
              allCategories={allCategories}
            />
          ))}
        </Collapse>
      )}
    </>
  );
};

export const PartCategoriesPage: React.FC = () => {
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const canManage = hasPermission('inventory.manage');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<PartCategory | null>(null);
  const [formData, setFormData] = useState<CreatePartCategoryRequest>({
    name: '',
    isActive: true,
    sortOrder: 0,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['partCategories', true],
    queryFn: () => partCategoryService.getCategories(true),
  });

  const createMutation = useMutation({
    mutationFn: partCategoryService.createCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partCategories'] });
      handleCloseDialog();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreatePartCategoryRequest }) =>
      partCategoryService.updateCategory(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partCategories'] });
      handleCloseDialog();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: partCategoryService.deleteCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partCategories'] });
    },
  });

  const handleOpenCreate = (parentId?: number) => {
    setEditingCategory(null);
    setFormData({ name: '', parentId, isActive: true, sortOrder: 0 });
    setDialogOpen(true);
  };

  const handleOpenEdit = (category: PartCategory) => {
    setEditingCategory(category);
    setFormData({
      name: category.name,
      code: category.code,
      description: category.description,
      parentId: category.parentId,
      sortOrder: category.sortOrder,
      isActive: category.isActive,
    });
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingCategory(null);
    setFormData({ name: '', isActive: true, sortOrder: 0 });
  };

  const handleSubmit = () => {
    if (editingCategory) {
      updateMutation.mutate({ id: editingCategory.id, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this category?')) {
      deleteMutation.mutate(id);
    }
  };

  const flattenCategories = (categories: PartCategory[], level = 0): (PartCategory & { indent: number })[] => {
    return categories.reduce((acc, cat) => {
      acc.push({ ...cat, indent: level });
      if (cat.children?.length) {
        acc.push(...flattenCategories(cat.children, level + 1));
      }
      return acc;
    }, [] as (PartCategory & { indent: number })[]);
  };

  const flatCategories = flattenCategories(data?.data || []);
  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Part Categories</Typography>
        {canManage && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => handleOpenCreate()}>
            Add Category
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
            {data.data.map((category) => (
              <CategoryItem
                key={category.id}
                category={category}
                level={0}
                onEdit={handleOpenEdit}
                onDelete={handleDelete}
                canManage={canManage}
                allCategories={data.data || []}
              />
            ))}
          </List>
        ) : (
          <Box sx={{ p: 3, textAlign: 'center' }}>
            <Typography color="text.secondary">No categories yet</Typography>
          </Box>
        )}
      </Paper>

      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingCategory ? 'Edit Category' : 'New Category'}</DialogTitle>
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
                label="Parent Category"
                value={formData.parentId || ''}
                onChange={(e) => setFormData({ ...formData, parentId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">None (Top Level)</MenuItem>
                {flatCategories
                  .filter((c) => c.id !== editingCategory?.id)
                  .map((cat) => (
                    <MenuItem key={cat.id} value={cat.id}>
                      {'  '.repeat(cat.indent)}{cat.name}
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
            <Grid item xs={6}>
              <TextField
                fullWidth
                type="number"
                label="Sort Order"
                value={formData.sortOrder}
                onChange={(e) => setFormData({ ...formData, sortOrder: Number(e.target.value) })}
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
