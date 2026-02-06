import React, { useState } from 'react';
import {
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  LinearProgress,
  List,
  ListItem,
  ListItemIcon,
  ListItemSecondaryAction,
  ListItemText,
  Menu,
  MenuItem,
  TextField,
  Typography,
  Chip,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  DragIndicator as DragIcon,
  MoreVert as MoreVertIcon,
  PlaylistAdd as TemplateIcon,
} from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
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
import { workOrderTaskService } from '../../services/workOrderTaskService';
import { taskTemplateService } from '../../services/taskTemplateService';
import { WorkOrderTask } from '../../types';

interface SortableTaskItemProps {
  task: WorkOrderTask;
  onToggle: (taskId: number) => void;
  onDelete: (taskId: number) => void;
  disabled: boolean;
}

const SortableTaskItem: React.FC<SortableTaskItemProps> = ({
  task,
  onToggle,
  onDelete,
  disabled,
}) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: task.id,
    disabled,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <ListItem
      ref={setNodeRef}
      style={style}
      divider
      sx={{
        bgcolor: task.isCompleted ? 'action.hover' : 'background.paper',
        '&:hover': { bgcolor: 'action.selected' },
      }}
    >
      {!disabled && (
        <ListItemIcon sx={{ minWidth: 32, cursor: 'grab' }} {...attributes} {...listeners}>
          <DragIcon fontSize="small" color="action" />
        </ListItemIcon>
      )}
      <ListItemIcon sx={{ minWidth: 42 }}>
        <Checkbox
          checked={task.isCompleted}
          onChange={() => onToggle(task.id)}
          disabled={disabled}
        />
      </ListItemIcon>
      <ListItemText
        primary={
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography
              sx={{
                textDecoration: task.isCompleted ? 'line-through' : 'none',
                color: task.isCompleted ? 'text.secondary' : 'text.primary',
              }}
            >
              {task.description}
            </Typography>
            {task.isRequired && (
              <Chip label="Required" size="small" color="error" variant="outlined" />
            )}
          </Box>
        }
        secondary={
          task.isCompleted && task.completedByName ? (
            <Typography variant="caption" color="text.secondary">
              Completed by {task.completedByName} on{' '}
              {task.completedAt ? new Date(task.completedAt).toLocaleString() : ''}
              {task.notes && ` - ${task.notes}`}
            </Typography>
          ) : null
        }
      />
      {!disabled && (
        <ListItemSecondaryAction>
          <IconButton edge="end" size="small" onClick={() => onDelete(task.id)} color="error">
            <DeleteIcon fontSize="small" />
          </IconButton>
        </ListItemSecondaryAction>
      )}
    </ListItem>
  );
};

interface WorkOrderTaskListProps {
  workOrderId: number;
  disabled?: boolean;
}

export const WorkOrderTaskList: React.FC<WorkOrderTaskListProps> = ({
  workOrderId,
  disabled = false,
}) => {
  const queryClient = useQueryClient();
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [templateDialogOpen, setTemplateDialogOpen] = useState(false);
  const [newTaskDescription, setNewTaskDescription] = useState('');
  const [newTaskRequired, setNewTaskRequired] = useState(true);
  const [selectedTemplateId, setSelectedTemplateId] = useState<number | ''>('');
  const [clearExisting, setClearExisting] = useState(false);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Fetch tasks
  const { data: tasksData, isLoading } = useQuery({
    queryKey: ['workOrderTasks', workOrderId],
    queryFn: () => workOrderTaskService.getTasks(workOrderId),
  });

  // Fetch task summary
  const { data: summaryData } = useQuery({
    queryKey: ['workOrderTaskSummary', workOrderId],
    queryFn: () => workOrderTaskService.getTaskSummary(workOrderId),
  });

  // Fetch available templates
  const { data: templatesData } = useQuery({
    queryKey: ['activeTaskTemplates'],
    queryFn: () => taskTemplateService.getActiveTemplates(),
  });

  const tasks = tasksData?.data || [];
  const summary = summaryData?.data;
  const templates = templatesData?.data || [];

  const invalidateQueries = () => {
    queryClient.invalidateQueries({ queryKey: ['workOrderTasks', workOrderId] });
    queryClient.invalidateQueries({ queryKey: ['workOrderTaskSummary', workOrderId] });
  };

  // Mutations
  const createTaskMutation = useMutation({
    mutationFn: () =>
      workOrderTaskService.createTask(workOrderId, {
        description: newTaskDescription,
        isRequired: newTaskRequired,
      }),
    onSuccess: () => {
      invalidateQueries();
      setAddDialogOpen(false);
      setNewTaskDescription('');
      setNewTaskRequired(true);
    },
  });

  const toggleTaskMutation = useMutation({
    mutationFn: (taskId: number) => workOrderTaskService.toggleTaskCompletion(workOrderId, taskId),
    onSuccess: invalidateQueries,
  });

  const deleteTaskMutation = useMutation({
    mutationFn: (taskId: number) => workOrderTaskService.deleteTask(workOrderId, taskId),
    onSuccess: invalidateQueries,
  });

  const reorderTasksMutation = useMutation({
    mutationFn: (taskIds: number[]) =>
      workOrderTaskService.reorderTasks(workOrderId, { taskIds }),
    onSuccess: invalidateQueries,
  });

  const applyTemplateMutation = useMutation({
    mutationFn: () =>
      workOrderTaskService.applyTemplate(workOrderId, {
        templateId: selectedTemplateId as number,
        clearExisting,
      }),
    onSuccess: () => {
      invalidateQueries();
      setTemplateDialogOpen(false);
      setSelectedTemplateId('');
      setClearExisting(false);
    },
  });

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const oldIndex = tasks.findIndex((t) => t.id === active.id);
      const newIndex = tasks.findIndex((t) => t.id === over.id);
      const newOrder = arrayMove(tasks, oldIndex, newIndex);
      reorderTasksMutation.mutate(newOrder.map((t) => t.id));
    }
  };

  if (isLoading) {
    return <LinearProgress />;
  }

  return (
    <Box>
      {/* Progress Summary */}
      {summary && summary.totalTasks > 0 && (
        <Box sx={{ mb: 2 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
            <Typography variant="body2" color="text.secondary">
              {summary.completedTasks} of {summary.totalTasks} tasks completed
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {Math.round(summary.completionPercentage)}%
            </Typography>
          </Box>
          <LinearProgress
            variant="determinate"
            value={summary.completionPercentage}
            color={summary.allRequiredCompleted ? 'success' : 'primary'}
            sx={{ height: 8, borderRadius: 1 }}
          />
          {!summary.allRequiredCompleted && summary.requiredTasks > 0 && (
            <Typography variant="caption" color="error" sx={{ mt: 0.5, display: 'block' }}>
              {summary.completedRequiredTasks} of {summary.requiredTasks} required tasks completed
            </Typography>
          )}
        </Box>
      )}

      {/* Action Buttons */}
      {!disabled && (
        <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
          <Button startIcon={<AddIcon />} variant="outlined" size="small" onClick={() => setAddDialogOpen(true)}>
            Add Task
          </Button>
          <Button
            startIcon={<TemplateIcon />}
            variant="outlined"
            size="small"
            onClick={() => setTemplateDialogOpen(true)}
            disabled={templates.length === 0}
          >
            Apply Template
          </Button>
        </Box>
      )}

      {/* Task List */}
      {tasks.length === 0 ? (
        <Alert severity="info">
          No tasks defined for this work order.
          {!disabled && ' Add tasks manually or apply a template.'}
        </Alert>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={tasks.map((t) => t.id)} strategy={verticalListSortingStrategy}>
            <List disablePadding sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
              {tasks.map((task) => (
                <SortableTaskItem
                  key={task.id}
                  task={task}
                  onToggle={(id) => toggleTaskMutation.mutate(id)}
                  onDelete={(id) => deleteTaskMutation.mutate(id)}
                  disabled={disabled}
                />
              ))}
            </List>
          </SortableContext>
        </DndContext>
      )}

      {/* Add Task Dialog */}
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Task</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="Task Description"
            value={newTaskDescription}
            onChange={(e) => setNewTaskDescription(e.target.value)}
            sx={{ mt: 1 }}
            autoFocus
          />
          <Box sx={{ mt: 2 }}>
            <label>
              <Checkbox
                checked={newTaskRequired}
                onChange={(e) => setNewTaskRequired(e.target.checked)}
              />
              Required task
            </label>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => createTaskMutation.mutate()}
            disabled={!newTaskDescription.trim() || createTaskMutation.isPending}
          >
            Add Task
          </Button>
        </DialogActions>
      </Dialog>

      {/* Apply Template Dialog */}
      <Dialog
        open={templateDialogOpen}
        onClose={() => setTemplateDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Apply Task Template</DialogTitle>
        <DialogContent>
          <TextField
            select
            fullWidth
            label="Select Template"
            value={selectedTemplateId}
            onChange={(e) => setSelectedTemplateId(Number(e.target.value))}
            sx={{ mt: 1 }}
          >
            {templates.map((template) => (
              <MenuItem key={template.id} value={template.id}>
                {template.name} ({template.itemCount} tasks)
              </MenuItem>
            ))}
          </TextField>
          <Box sx={{ mt: 2 }}>
            <label>
              <Checkbox
                checked={clearExisting}
                onChange={(e) => setClearExisting(e.target.checked)}
              />
              Clear existing tasks before applying
            </label>
          </Box>
          {clearExisting && tasks.length > 0 && (
            <Alert severity="warning" sx={{ mt: 1 }}>
              This will remove all {tasks.length} existing task(s) before adding template tasks.
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTemplateDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => applyTemplateMutation.mutate()}
            disabled={!selectedTemplateId || applyTemplateMutation.isPending}
          >
            Apply Template
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
