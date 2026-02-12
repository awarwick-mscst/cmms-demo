import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Switch,
  Alert,
  CircularProgress,
  Skeleton,
} from '@mui/material';
import { Email as EmailIcon, CalendarMonth as CalendarIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationService } from '../../services/notificationService';
import { UserNotificationPreference } from '../../types';

export const NotificationPreferencesPage: React.FC = () => {
  const queryClient = useQueryClient();

  const { data: preferences, isLoading, error } = useQuery({
    queryKey: ['notificationPreferences'],
    queryFn: async () => {
      const response = await notificationService.getMyPreferences();
      return response.data;
    },
  });

  const updateMutation = useMutation({
    mutationFn: (pref: { notificationType: string; emailEnabled: boolean; calendarEnabled: boolean }) =>
      notificationService.updatePreference(pref),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationPreferences'] });
    },
  });

  const handleToggle = (
    pref: UserNotificationPreference,
    field: 'emailEnabled' | 'calendarEnabled'
  ) => {
    updateMutation.mutate({
      notificationType: pref.notificationType,
      emailEnabled: field === 'emailEnabled' ? !pref.emailEnabled : pref.emailEnabled,
      calendarEnabled: field === 'calendarEnabled' ? !pref.calendarEnabled : pref.calendarEnabled,
    });
  };

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Failed to load notification preferences</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Notification Preferences
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Configure which notifications you want to receive via email or calendar invites.
      </Typography>

      <Paper sx={{ width: '100%', overflow: 'hidden' }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Notification Type</TableCell>
                <TableCell align="center" sx={{ width: 120 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1 }}>
                    <EmailIcon fontSize="small" />
                    Email
                  </Box>
                </TableCell>
                <TableCell align="center" sx={{ width: 120 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1 }}>
                    <CalendarIcon fontSize="small" />
                    Calendar
                  </Box>
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 7 }).map((_, index) => (
                  <TableRow key={index}>
                    <TableCell>
                      <Skeleton variant="text" width={200} />
                    </TableCell>
                    <TableCell align="center">
                      <Skeleton variant="rectangular" width={40} height={24} sx={{ mx: 'auto' }} />
                    </TableCell>
                    <TableCell align="center">
                      <Skeleton variant="rectangular" width={40} height={24} sx={{ mx: 'auto' }} />
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                preferences?.map((pref) => (
                  <TableRow key={pref.notificationType} hover>
                    <TableCell>
                      <Typography variant="body1">{pref.notificationTypeDisplay}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {getNotificationDescription(pref.notificationType)}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">
                      <Switch
                        checked={pref.emailEnabled}
                        onChange={() => handleToggle(pref, 'emailEnabled')}
                        disabled={updateMutation.isPending}
                        color="primary"
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Switch
                        checked={pref.calendarEnabled}
                        onChange={() => handleToggle(pref, 'calendarEnabled')}
                        disabled={updateMutation.isPending || !isCalendarApplicable(pref.notificationType)}
                        color="primary"
                      />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      {updateMutation.isPending && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
          <CircularProgress size={16} />
          <Typography variant="body2">Saving...</Typography>
        </Box>
      )}

      {updateMutation.isError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          Failed to update preference
        </Alert>
      )}
    </Box>
  );
};

function getNotificationDescription(type: string): string {
  switch (type) {
    case 'WorkOrderAssigned':
      return 'When a work order is assigned to you';
    case 'WorkOrderApproachingDue':
      return 'Reminder before work order due date';
    case 'WorkOrderOverdue':
      return 'When a work order becomes overdue';
    case 'WorkOrderCompleted':
      return 'When a work order you requested is completed';
    case 'PMScheduleComingDue':
      return 'Reminder before PM schedule due date';
    case 'PMScheduleOverdue':
      return 'When a PM schedule becomes overdue';
    case 'LowStockAlert':
      return 'When inventory falls below reorder point';
    default:
      return '';
  }
}

function isCalendarApplicable(type: string): boolean {
  return ['WorkOrderAssigned', 'WorkOrderApproachingDue', 'PMScheduleComingDue'].includes(type);
}

export default NotificationPreferencesPage;
