import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Collapse,
  Divider,
  IconButton,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Typography,
} from '@mui/material';
import {
  CheckCircle as CheckIcon,
  Cancel as CancelIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Dns as ServerIcon,
  Security as SecurityIcon,
  Group as GroupIcon,
  Refresh as RefreshIcon,
  Warning as WarningIcon,
  Help as HelpIcon,
} from '@mui/icons-material';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { ldapService } from '../../services/ldapService';

export const LdapStatusCard: React.FC = () => {
  const navigate = useNavigate();
  const [expanded, setExpanded] = useState(false);

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['ldapStatus'],
    queryFn: () => ldapService.getStatus(),
    staleTime: 60000, // 1 minute
  });

  const testConnectionMutation = useMutation({
    mutationFn: () => ldapService.testConnection(),
  });

  const status = data?.data;

  if (isLoading) {
    return (
      <Card sx={{ mb: 3 }}>
        <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <CircularProgress size={20} />
          <Typography>Loading LDAP status...</Typography>
        </CardContent>
      </Card>
    );
  }

  const isEnabled = status?.enabled ?? false;
  const hasWarnings = (status?.configurationWarnings?.length ?? 0) > 0;

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <SecurityIcon color={isEnabled ? 'primary' : 'disabled'} />
            <Box>
              <Typography variant="subtitle1" fontWeight="medium">
                LDAP / Active Directory Authentication
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5 }}>
                <Chip
                  size="small"
                  icon={isEnabled ? <CheckIcon /> : <CancelIcon />}
                  label={isEnabled ? 'Enabled' : 'Disabled'}
                  color={isEnabled ? 'success' : 'default'}
                />
                {isEnabled && status?.authenticationMode && (
                  <Chip
                    size="small"
                    label={`Mode: ${status.authenticationMode}`}
                    variant="outlined"
                  />
                )}
                {hasWarnings && (
                  <Chip
                    size="small"
                    icon={<WarningIcon />}
                    label={`${status?.configurationWarnings.length} warning(s)`}
                    color="warning"
                  />
                )}
              </Box>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Button
              size="small"
              startIcon={<HelpIcon />}
              onClick={() => navigate('/admin/help', { state: { section: 'ldap' } })}
            >
              Setup Guide
            </Button>
            <IconButton size="small" onClick={() => refetch()}>
              <RefreshIcon />
            </IconButton>
            <IconButton size="small" onClick={() => setExpanded(!expanded)}>
              {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          </Box>
        </Box>

        <Collapse in={expanded}>
          <Divider sx={{ my: 2 }} />

          {!isEnabled ? (
            <Alert severity="info" sx={{ mb: 2 }}>
              LDAP authentication is not enabled. Users authenticate with local passwords only.
              See the <strong>Setup Guide</strong> for configuration instructions.
            </Alert>
          ) : (
            <>
              <List dense>
                <ListItem>
                  <ListItemIcon>
                    <ServerIcon />
                  </ListItemIcon>
                  <ListItemText
                    primary="Server"
                    secondary={`${status?.server}:${status?.port} ${
                      status?.useSsl ? '(SSL)' : status?.useStartTls ? '(StartTLS)' : '(Unencrypted)'
                    }`}
                  />
                </ListItem>
                <ListItem>
                  <ListItemIcon>
                    <GroupIcon />
                  </ListItemIcon>
                  <ListItemText
                    primary="Group Mappings"
                    secondary={
                      status?.groupMappingsCount
                        ? `${status.groupMappingsCount} LDAP group(s) mapped to roles`
                        : 'No group mappings configured'
                    }
                  />
                </ListItem>
                <ListItem>
                  <ListItemText
                    primary="Default Roles"
                    secondary={status?.defaultRoles?.join(', ') || 'None'}
                    sx={{ ml: 7 }}
                  />
                </ListItem>
                <ListItem>
                  <ListItemText
                    primary="Settings"
                    secondary={[
                      status?.allowLocalFallback && 'Local fallback enabled',
                      status?.syncUserAttributes && 'User sync enabled',
                    ]
                      .filter(Boolean)
                      .join(' • ') || 'Default settings'}
                    sx={{ ml: 7 }}
                  />
                </ListItem>
              </List>

              {hasWarnings && (
                <Alert severity="warning" sx={{ mt: 2 }}>
                  <Typography variant="subtitle2">Configuration Warnings:</Typography>
                  <ul style={{ margin: '8px 0 0 0', paddingLeft: '20px' }}>
                    {status?.configurationWarnings.map((warning, index) => (
                      <li key={index}>{warning}</li>
                    ))}
                  </ul>
                </Alert>
              )}

              <Box sx={{ mt: 2, display: 'flex', gap: 2 }}>
                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => testConnectionMutation.mutate()}
                  disabled={testConnectionMutation.isPending}
                >
                  {testConnectionMutation.isPending ? (
                    <CircularProgress size={16} sx={{ mr: 1 }} />
                  ) : null}
                  Test Connection
                </Button>
              </Box>

              {testConnectionMutation.data && (
                <Alert
                  severity={testConnectionMutation.data.data?.success ? 'success' : 'error'}
                  sx={{ mt: 2 }}
                >
                  {testConnectionMutation.data.data?.success ? (
                    <>
                      Connection successful! {testConnectionMutation.data.data.serverInfo}
                      <br />
                      <Typography variant="caption">
                        Response time: {testConnectionMutation.data.data.responseTimeMs?.toFixed(0)}ms
                      </Typography>
                    </>
                  ) : (
                    testConnectionMutation.data.data?.message || 'Connection failed'
                  )}
                </Alert>
              )}
            </>
          )}
        </Collapse>
      </CardContent>
    </Card>
  );
};
