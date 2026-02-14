import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Typography,
} from '@mui/material';
import {
  TrendingUp as PredictIcon,
  ReportProblem as DowntimeIcon,
  Warning as OverdueIcon,
  HealthAndSafety as HealthIcon,
} from '@mui/icons-material';
import { Autocomplete } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '../../services/assetService';

interface QuickActionsProps {
  onAction: (message: string, contextType: string, assetId?: number) => void;
  disabled?: boolean;
}

export const QuickActions: React.FC<QuickActionsProps> = ({ onAction, disabled }) => {
  const [assetDialogOpen, setAssetDialogOpen] = useState(false);
  const [selectedAssetId, setSelectedAssetId] = useState<number | null>(null);

  const { data: assetsData } = useQuery({
    queryKey: ['assets-for-ai'],
    queryFn: () => assetService.getAssets({ pageSize: 1000 }),
    enabled: assetDialogOpen,
  });

  const assets = assetsData?.items || [];

  const actions = [
    {
      icon: <PredictIcon fontSize="large" />,
      title: 'Predict Failures',
      description: 'Analyze maintenance history and predict which assets need service soon',
      contextType: 'predictive',
      message: 'Analyze the maintenance history data provided and predict which assets are most likely to need service soon. Identify patterns in repair frequency, highlight assets with increasing failure rates, and recommend proactive maintenance actions.',
    },
    {
      icon: <DowntimeIcon fontSize="large" />,
      title: 'Down Machines',
      description: 'Review all machines currently down and flag ones needing follow-up',
      contextType: 'downtime_followup',
      message: 'Review all machines currently in a down/maintenance status. For each one, assess the situation: Is there an active work order? Is a technician assigned? How long has it been down? Flag any that appear stalled or need escalation, and recommend next steps.',
    },
    {
      icon: <OverdueIcon fontSize="large" />,
      title: 'Overdue Maintenance',
      description: 'Review overdue PM schedules and work orders, prioritize actions',
      contextType: 'overdue',
      message: 'Review all overdue preventive maintenance schedules and work orders. Prioritize them by criticality, safety impact, and how overdue they are. Recommend which ones to address first and suggest a plan to catch up on the backlog.',
    },
    {
      icon: <HealthIcon fontSize="large" />,
      title: 'Asset Health Check',
      description: 'Full health assessment for a specific asset',
      contextType: 'asset_health',
      needsAsset: true,
      message: 'Perform a comprehensive health assessment of this asset based on its maintenance history, PM compliance, repair frequency, and current status. Identify any concerning trends and recommend maintenance actions.',
    },
  ];

  const handleAction = (action: typeof actions[0]) => {
    if (action.needsAsset) {
      setAssetDialogOpen(true);
    } else {
      onAction(action.message, action.contextType);
    }
  };

  const handleAssetHealthSubmit = () => {
    if (!selectedAssetId) return;
    const action = actions.find((a) => a.contextType === 'asset_health')!;
    onAction(action.message, action.contextType, selectedAssetId);
    setAssetDialogOpen(false);
    setSelectedAssetId(null);
  };

  return (
    <>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2, p: 2 }}>
        {actions.map((action) => (
          <Card
            key={action.contextType}
            sx={{
              cursor: disabled ? 'default' : 'pointer',
              opacity: disabled ? 0.5 : 1,
              transition: 'transform 0.15s, box-shadow 0.15s',
              '&:hover': disabled ? {} : { transform: 'translateY(-2px)', boxShadow: 4 },
            }}
            onClick={() => !disabled && handleAction(action)}
          >
            <CardContent sx={{ textAlign: 'center' }}>
              <Box sx={{ color: 'primary.main', mb: 1 }}>{action.icon}</Box>
              <Typography variant="h6" gutterBottom>
                {action.title}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {action.description}
              </Typography>
            </CardContent>
          </Card>
        ))}
      </Box>

      <Dialog open={assetDialogOpen} onClose={() => setAssetDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Select Asset for Health Check</DialogTitle>
        <DialogContent>
          <Autocomplete
            sx={{ mt: 1 }}
            options={assets}
            getOptionLabel={(option) => `${option.assetTag} - ${option.name}`}
            onChange={(_, value) => setSelectedAssetId(value?.id ?? null)}
            renderInput={(params) => <TextField {...params} label="Search assets..." />}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssetDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleAssetHealthSubmit} disabled={!selectedAssetId}>
            Analyze
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};
