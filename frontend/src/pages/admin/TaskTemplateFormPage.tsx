import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Grid,
  IconButton,
  List,
  ListItem,
  ListItemIcon,
  ListItemSecondaryAction,
  ListItemText,
  TextField,
  Typography,
  Alert,
  FormControlLabel,
  Switch,
} from '@mui/material';
import {
  Save as SaveIcon,
  ArrowBack as BackIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
  DragIndicator as DragIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { taskTemplateService } from '../../services/taskTemplateService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { UpdateWorkOrderTaskTemplateItemRequest } from '../../types';

interface TemplateItem {
  id?: number;
  tempId: string;
  description: string;
  isRequired: boolean;
}

interface SortableItemProps {
  item: TemplateItem;
  onUpdate: (tempId: string, field: keyof TemplateItem, value: any) => void;
  onDelete: (tempId: string) => void;
}

const SortableItem: React.FC<SortableItemProps> = ({ item, onUpdate, onDelete }) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: item.tempId,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <ListItem ref={setNodeRef} style={style} divider sx={{ py: 2, bgcolor: 'background.paper' }}>
      <ListItemIcon sx={{ minWidth: 32, cursor: 'grab' }} {...attributes} {...listeners}>
        <DragIcon fontSize="small" color="action" />
      </ListItemIcon>
      <ListItemText
        primary={
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <TextField
              size="small"
              fullWidth
              placeholder="Task description"
              value={item.description}
              onChange={(e) => onUpdate(item.tempId, 'description', e.target.value)}
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={item.isRequired}
                  onChange={(e) => onUpdate(item.tempId, 'isRequired', e.target.checked)}
                  size="small"
                />
              }
              label="Required"
              sx={{ whiteSpace: 'nowrap' }}
            />
          </Box>
        }
      />
      <ListItemSecondaryAction>
        <IconButton edge="end" size="small" onClick={() => onDelete(item.tempId)} color="error">
          <DeleteIcon fontSize="small" />
        </IconButton>
      </ListItemSecondaryAction>
    </ListItem>
  );
};

export const TaskTemplateFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = !!id;

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [items, setItems] = useState<TemplateItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const { data: templateData, isLoading } = useQuery({
    queryKey: ['taskTemplate', id],
    queryFn: () => taskTemplateService.getTemplate(Number(id)),
    enabled: isEdit,
  });

  useEffect(() => {
    if (templateData?.data) {
      const template = templateData.data;
      setName(template.name);
      setDescription(template.description || '');
      setIsActive(template.isActive);
      setItems(
        template.items.map((item, index) => ({
          id: item.id,
          tempId: `item-${item.id}`,
          description: item.description,
          isRequired: item.isRequired,
        }))
      );
    }
  }, [templateData]);

  const createMutation = useMutation({
    mutationFn: taskTemplateService.createTemplate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taskTemplates'] });
      navigate('/admin/task-templates');
    },
    onError: (err: any) => {
      setError(err.response?.data?.errors?.[0] || 'Failed to create template');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      taskTemplateService.updateTemplate(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taskTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['taskTemplate', id] });
      navigate('/admin/task-templates');
    },
    onError: (err: any) => {
      setError(err.response?.data?.errors?.[0] || 'Failed to update template');
    },
  });

  const handleAddItem = () => {
    setItems([
      ...items,
      {
        tempId: `new-${Date.now()}`,
        description: '',
        isRequired: true,
      },
    ]);
  };

  const handleUpdateItem = (tempId: string, field: keyof TemplateItem, value: any) => {
    setItems(items.map((item) => (item.tempId === tempId ? { ...item, [field]: value } : item)));
  };

  const handleDeleteItem = (tempId: string) => {
    setItems(items.filter((item) => item.tempId !== tempId));
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const oldIndex = items.findIndex((item) => item.tempId === active.id);
      const newIndex = items.findIndex((item) => item.tempId === over.id);
      setItems(arrayMove(items, oldIndex, newIndex));
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError('Name is required');
      return;
    }

    if (items.length === 0) {
      setError('At least one task item is required');
      return;
    }

    const emptyItems = items.filter((item) => !item.description.trim());
    if (emptyItems.length > 0) {
      setError('All task items must have a description');
      return;
    }

    const requestItems: UpdateWorkOrderTaskTemplateItemRequest[] = items.map((item, index) => ({
      id: item.id,
      sortOrder: index,
      description: item.description,
      isRequired: item.isRequired,
    }));

    const data = {
      name,
      description: description || undefined,
      isActive,
      items: requestItems,
    };

    if (isEdit) {
      updateMutation.mutate({ id: Number(id), data });
    } else {
      createMutation.mutate(data);
    }
  };

  if (isEdit && isLoading) {
    return <LoadingSpinner message="Loading template..." />;
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/admin/task-templates')}>
          Back
        </Button>
        <Typography variant="h5">{isEdit ? 'Edit Task Template' : 'New Task Template'}</Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Template Details
                </Typography>
                <TextField
                  fullWidth
                  label="Name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                  sx={{ mb: 2 }}
                />
                <TextField
                  fullWidth
                  label="Description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  multiline
                  rows={3}
                  sx={{ mb: 2 }}
                />
                <FormControlLabel
                  control={
                    <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
                  }
                  label="Active"
                />
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6">
                    Task Items ({items.length})
                  </Typography>
                  <Button startIcon={<AddIcon />} onClick={handleAddItem} variant="outlined" size="small">
                    Add Task
                  </Button>
                </Box>

                {items.length === 0 ? (
                  <Alert severity="info">
                    No tasks defined. Click "Add Task" to add your first task item.
                  </Alert>
                ) : (
                  <DndContext
                    sensors={sensors}
                    collisionDetection={closestCenter}
                    onDragEnd={handleDragEnd}
                  >
                    <SortableContext
                      items={items.map((i) => i.tempId)}
                      strategy={verticalListSortingStrategy}
                    >
                      <List disablePadding sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                        {items.map((item) => (
                          <SortableItem
                            key={item.tempId}
                            item={item}
                            onUpdate={handleUpdateItem}
                            onDelete={handleDeleteItem}
                          />
                        ))}
                      </List>
                    </SortableContext>
                  </DndContext>
                )}
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
              <Button variant="outlined" onClick={() => navigate('/admin/task-templates')}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                startIcon={<SaveIcon />}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : 'Save Template'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};
