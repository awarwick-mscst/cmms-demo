import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  List,
  ListItem,
  ListItemText,
  Chip,
  Divider,
  CircularProgress,
  ToggleButton,
  ToggleButtonGroup,
  IconButton,
  Tooltip,
  FormControlLabel,
  Switch,
} from '@mui/material';
import {
  Assignment as WorkOrderIcon,
  Build as RepairIcon,
  Schedule as PMIcon,
  Warning as OverdueIcon,
  CalendarMonth as CalendarIcon,
  Add as AddIcon,
  List as ListIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { workOrderService } from '../../services/workOrderService';
import { WorkOrderSummary } from '../../types';
import { useAuth } from '../../hooks/useAuth';

type ViewMode = 'all' | 'repair' | 'preventive' | 'overdue' | 'upcoming';

const priorityColors: Record<string, 'default' | 'info' | 'warning' | 'error'> = {
  Low: 'default',
  Medium: 'info',
  High: 'warning',
  Critical: 'error',
  Emergency: 'error',
};

const statusColors: Record<string, 'default' | 'primary' | 'warning' | 'success' | 'error'> = {
  Draft: 'default',
  Open: 'primary',
  InProgress: 'warning',
  OnHold: 'default',
  Completed: 'success',
  Cancelled: 'error',
};

const formatDate = (dateString?: string): string => {
  if (!dateString) return 'No date';
  const date = new Date(dateString);
  const today = new Date();
  const diffDays = Math.floor((date.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays < 0) return `${Math.abs(diffDays)} days overdue`;
  if (diffDays === 0) return 'Due today';
  if (diffDays === 1) return 'Due tomorrow';
  if (diffDays <= 7) return `Due in ${diffDays} days`;
  return date.toLocaleDateString();
};

const isOverdue = (dateString?: string): boolean => {
  if (!dateString) return false;
  return new Date(dateString) < new Date();
};

interface WorkOrderItemProps {
  workOrder: WorkOrderSummary;
  onClick: () => void;
  showOverdueWarning?: boolean;
}

const WorkOrderItem: React.FC<WorkOrderItemProps> = ({ workOrder, onClick, showOverdueWarning }) => {
  const overdue = showOverdueWarning && isOverdue(workOrder.scheduledEndDate);

  return (
    <ListItem
      sx={{
        cursor: 'pointer',
        borderRadius: 1,
        bgcolor: overdue ? 'error.50' : 'transparent',
        borderLeft: overdue ? '3px solid' : 'none',
        borderLeftColor: 'error.main',
        '&:hover': { bgcolor: overdue ? 'error.100' : 'action.hover' },
      }}
      onClick={onClick}
    >
      <ListItemText
        primary={
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 500 }}>
              {workOrder.workOrderNumber}
            </Typography>
            <Chip
              label={workOrder.status}
              size="small"
              color={statusColors[workOrder.status] || 'default'}
              sx={{ height: 20, fontSize: '0.7rem' }}
            />
            <Chip
              label={workOrder.priority}
              size="small"
              color={priorityColors[workOrder.priority] || 'default'}
              variant="outlined"
              sx={{ height: 20, fontSize: '0.7rem' }}
            />
            {overdue && (
              <Chip
                icon={<OverdueIcon sx={{ fontSize: 14 }} />}
                label="Overdue"
                size="small"
                color="error"
                sx={{ height: 20, fontSize: '0.7rem' }}
              />
            )}
          </Box>
        }
        secondary={
          <Box sx={{ mt: 0.5 }}>
            <Typography variant="body2" color="text.primary" noWrap>
              {workOrder.title}
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, mt: 0.5 }}>
              {workOrder.assetName && (
                <Typography variant="caption" color="text.secondary">
                  Asset: {workOrder.assetName}
                </Typography>
              )}
              {workOrder.scheduledEndDate && (
                <Typography
                  variant="caption"
                  color={overdue ? 'error.main' : 'text.secondary'}
                  sx={{ fontWeight: overdue ? 600 : 400 }}
                >
                  {formatDate(workOrder.scheduledEndDate)}
                </Typography>
              )}
            </Box>
          </Box>
        }
      />
    </ListItem>
  );
};

export const WorkOrdersWidget: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [viewMode, setViewMode] = useState<ViewMode>('all');
  const [assignedToMe, setAssignedToMe] = useState(false);

  const getQueryParams = () => {
    const today = new Date().toISOString().split('T')[0];
    const baseParams: Record<string, any> = {
      pageSize: 10,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    // Add assigned to me filter if enabled
    if (assignedToMe && user?.id) {
      baseParams.assignedToId = user.id;
    }

    switch (viewMode) {
      case 'all':
        return { ...baseParams };
      case 'repair':
        return { ...baseParams, type: 'Repair' };
      case 'preventive':
        return { ...baseParams, type: 'PreventiveMaintenance' };
      case 'overdue':
        return { ...baseParams, status: 'Open,InProgress', scheduledStartTo: today, sortBy: 'scheduledEndDate', sortDescending: false };
      case 'upcoming':
        return { ...baseParams, status: 'Open,InProgress', sortBy: 'scheduledEndDate', sortDescending: false };
      default:
        return baseParams;
    }
  };

  const { data, isLoading, error } = useQuery({
    queryKey: ['workOrdersWidget', viewMode, assignedToMe, user?.id],
    queryFn: () => workOrderService.getWorkOrders(getQueryParams()),
    refetchInterval: 60000, // Refresh every minute
  });

  const workOrders = data?.items || [];

  const handleViewModeChange = (_: React.MouseEvent<HTMLElement>, newMode: ViewMode | null) => {
    if (newMode) {
      setViewMode(newMode);
    }
  };

  const handleWorkOrderClick = (id: number) => {
    navigate(`/maintenance/work-orders/${id}`);
  };

  const getEmptyMessage = () => {
    switch (viewMode) {
      case 'all':
        return 'No work orders found';
      case 'repair':
        return 'No repair work orders';
      case 'preventive':
        return 'No preventive maintenance scheduled';
      case 'overdue':
        return 'No overdue work orders';
      case 'upcoming':
        return 'No upcoming work orders';
      default:
        return 'No work orders';
    }
  };

  const getViewIcon = () => {
    switch (viewMode) {
      case 'all':
        return <WorkOrderIcon color="primary" />;
      case 'repair':
        return <RepairIcon color="primary" />;
      case 'preventive':
        return <PMIcon color="primary" />;
      case 'overdue':
        return <OverdueIcon color="primary" />;
      case 'upcoming':
        return <CalendarIcon color="primary" />;
      default:
        return <WorkOrderIcon color="primary" />;
    }
  };

  return (
    <Paper sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        {getViewIcon()}
        <Typography variant="h6">Work Orders</Typography>
        <Chip
          label={data?.totalCount || 0}
          size="small"
          color="primary"
          sx={{ ml: 1 }}
        />
        <Box sx={{ ml: 'auto' }}>
          <Tooltip title="Create Work Order">
            <IconButton
              size="small"
              color="primary"
              onClick={() => navigate('/maintenance/work-orders/new')}
            >
              <AddIcon />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, flexWrap: 'wrap', gap: 1 }}>
        <ToggleButtonGroup
          value={viewMode}
          exclusive
          onChange={handleViewModeChange}
          size="small"
        >
          <ToggleButton value="all">
            <Tooltip title="All Work Orders">
              <ListIcon fontSize="small" />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="repair">
            <Tooltip title="Repairs">
              <RepairIcon fontSize="small" />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="preventive">
            <Tooltip title="Preventive Maintenance">
              <PMIcon fontSize="small" />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="overdue">
            <Tooltip title="Overdue">
              <OverdueIcon fontSize="small" />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="upcoming">
            <Tooltip title="By Due Date">
              <CalendarIcon fontSize="small" />
            </Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        <FormControlLabel
          control={
            <Switch
              size="small"
              checked={assignedToMe}
              onChange={(e) => setAssignedToMe(e.target.checked)}
            />
          }
          label={<Typography variant="body2">My Work</Typography>}
          sx={{ mr: 0 }}
        />
      </Box>

      <Divider sx={{ mb: 1 }} />

      <Box sx={{ flex: 1, overflow: 'auto' }}>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={32} />
          </Box>
        ) : error ? (
          <Typography color="error" variant="body2">
            Failed to load work orders
          </Typography>
        ) : workOrders.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <WorkOrderIcon sx={{ fontSize: 48, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary" variant="body2">
              {getEmptyMessage()}
            </Typography>
          </Box>
        ) : (
          <List disablePadding>
            {workOrders.map((workOrder, index) => (
              <React.Fragment key={workOrder.id}>
                {index > 0 && <Divider component="li" />}
                <WorkOrderItem
                  workOrder={workOrder}
                  onClick={() => handleWorkOrderClick(workOrder.id)}
                  showOverdueWarning={viewMode === 'overdue' || viewMode === 'upcoming'}
                />
              </React.Fragment>
            ))}
          </List>
        )}
      </Box>

      {workOrders.length > 0 && (
        <Box sx={{ mt: 2, textAlign: 'center' }}>
          <Typography
            variant="body2"
            color="primary"
            sx={{ cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
            onClick={() => navigate('/maintenance/work-orders')}
          >
            View all work orders
          </Typography>
        </Box>
      )}
    </Paper>
  );
};
