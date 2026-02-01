import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Avatar,
  Chip,
  Divider,
  CircularProgress,
} from '@mui/material';
import {
  Person as PersonIcon,
  Timer as TimerIcon,
  Engineering as WorkingIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { workOrderService } from '../../services/workOrderService';
import { WorkSession } from '../../types';

const formatElapsedTime = (startedAt: string): string => {
  const start = new Date(startedAt).getTime();
  const now = Date.now();
  const seconds = Math.floor((now - start) / 1000);
  const hrs = Math.floor(seconds / 3600);
  const mins = Math.floor((seconds % 3600) / 60);
  const secs = seconds % 60;
  return `${hrs.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
};

interface WorkerCardProps {
  session: WorkSession;
  elapsedTime: string;
  onClick: () => void;
}

const WorkerCard: React.FC<WorkerCardProps> = ({ session, elapsedTime, onClick }) => (
  <ListItem
    sx={{
      cursor: 'pointer',
      borderRadius: 1,
      '&:hover': { bgcolor: 'action.hover' },
    }}
    onClick={onClick}
  >
    <ListItemAvatar>
      <Avatar sx={{ bgcolor: 'success.main' }}>
        {session.userName?.charAt(0) || <PersonIcon />}
      </Avatar>
    </ListItemAvatar>
    <ListItemText
      primary={
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography variant="subtitle2">{session.userName}</Typography>
          <Chip
            icon={<TimerIcon />}
            label={elapsedTime}
            size="small"
            color="success"
            sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}
          />
        </Box>
      }
      secondary={
        <Box>
          <Typography variant="body2" color="text.secondary" noWrap>
            {session.workOrderNumber} - {session.workOrderTitle}
          </Typography>
        </Box>
      }
    />
  </ListItem>
);

export const AvailabilityWidget: React.FC = () => {
  const navigate = useNavigate();
  const [elapsedTimes, setElapsedTimes] = useState<Record<number, string>>({});

  const { data, isLoading, error } = useQuery({
    queryKey: ['activeSessions'],
    queryFn: () => workOrderService.getAllActiveSessions(),
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const sessions = data?.data || [];

  // Update elapsed times every second
  useEffect(() => {
    const updateTimes = () => {
      const times: Record<number, string> = {};
      sessions.forEach((session) => {
        times[session.id] = formatElapsedTime(session.startedAt);
      });
      setElapsedTimes(times);
    };

    updateTimes();
    const interval = setInterval(updateTimes, 1000);
    return () => clearInterval(interval);
  }, [sessions]);

  const handleSessionClick = (workOrderId: number) => {
    navigate(`/maintenance/work-orders/${workOrderId}`);
  };

  return (
    <Paper sx={{ p: 3, height: '100%' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <WorkingIcon color="primary" />
        <Typography variant="h6">Availability</Typography>
        <Chip
          label={sessions.length}
          color={sessions.length > 0 ? 'success' : 'default'}
          size="small"
          sx={{ ml: 'auto' }}
        />
      </Box>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {sessions.length === 0
          ? 'No technicians currently clocked in'
          : `${sessions.length} technician${sessions.length !== 1 ? 's' : ''} working`}
      </Typography>

      <Divider sx={{ mb: 1 }} />

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={32} />
        </Box>
      ) : error ? (
        <Typography color="error" variant="body2">
          Failed to load availability data
        </Typography>
      ) : sessions.length === 0 ? (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <PersonIcon sx={{ fontSize: 48, color: 'text.disabled', mb: 1 }} />
          <Typography color="text.secondary" variant="body2">
            No active work sessions
          </Typography>
        </Box>
      ) : (
        <List disablePadding>
          {sessions.map((session, index) => (
            <React.Fragment key={session.id}>
              {index > 0 && <Divider variant="inset" component="li" />}
              <WorkerCard
                session={session}
                elapsedTime={elapsedTimes[session.id] || '00:00:00'}
                onClick={() => handleSessionClick(session.workOrderId)}
              />
            </React.Fragment>
          ))}
        </List>
      )}
    </Paper>
  );
};
