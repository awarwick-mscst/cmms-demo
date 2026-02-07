import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Grid,
  Paper,
  Typography,
} from '@mui/material';
import { Edit as EditIcon, ArrowBack as BackIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '../services/assetService';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { useAuth } from '../hooks/useAuth';
import { AttachmentManager, PrimaryImage } from '../components/attachments';

export const AssetDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();

  const { data, isLoading, error } = useQuery({
    queryKey: ['asset', id],
    queryFn: () => assetService.getAsset(Number(id)),
    enabled: !!id,
  });

  if (isLoading) {
    return <LoadingSpinner message="Loading asset details..." />;
  }

  if (error || !data?.data) {
    return (
      <Box>
        <Typography color="error">Asset not found</Typography>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/assets')}>
          Back to Assets
        </Button>
      </Box>
    );
  }

  const asset = data.data;

  const InfoItem: React.FC<{ label: string; value: React.ReactNode }> = ({ label, value }) => (
    <Box sx={{ mb: 2 }}>
      <Typography variant="caption" color="text.secondary" display="block">
        {label}
      </Typography>
      <Typography variant="body1">{value || '-'}</Typography>
    </Box>
  );

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Button startIcon={<BackIcon />} onClick={() => navigate('/assets')}>
            Back
          </Button>
          <Typography variant="h5">{asset.name}</Typography>
          <Chip label={asset.assetTag} variant="outlined" />
        </Box>
        {hasPermission('assets.edit') && (
          <Button
            variant="contained"
            startIcon={<EditIcon />}
            onClick={() => navigate(`/assets/${id}/edit`)}
          >
            Edit
          </Button>
        )}
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                General Information
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Asset Tag" value={asset.assetTag} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Name" value={asset.name} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Category" value={asset.categoryName} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Location" value={asset.locationName} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem
                    label="Status"
                    value={<Chip label={asset.status} size="small" color="primary" />}
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem
                    label="Criticality"
                    value={<Chip label={asset.criticality} size="small" variant="outlined" />}
                  />
                </Grid>
                <Grid item xs={12}>
                  <InfoItem label="Description" value={asset.description} />
                </Grid>
              </Grid>
            </CardContent>
          </Card>

          <Card sx={{ mt: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Technical Details
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Manufacturer" value={asset.manufacturer} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Model" value={asset.model} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Serial Number" value={asset.serialNumber} />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <InfoItem label="Barcode" value={asset.barcode} />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Box sx={{ mb: 3 }}>
            <PrimaryImage entityType="Asset" entityId={asset.id} height={220} />
          </Box>

          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Financial Information
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <InfoItem
                label="Purchase Date"
                value={asset.purchaseDate ? new Date(asset.purchaseDate).toLocaleDateString() : null}
              />
              <InfoItem
                label="Purchase Cost"
                value={asset.purchaseCost ? `$${asset.purchaseCost.toLocaleString()}` : null}
              />
              <InfoItem
                label="Warranty Expiry"
                value={asset.warrantyExpiry ? new Date(asset.warrantyExpiry).toLocaleDateString() : null}
              />
              <InfoItem label="Expected Life" value={asset.expectedLifeYears ? `${asset.expectedLifeYears} years` : null} />
            </CardContent>
          </Card>

          <Card sx={{ mt: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Maintenance
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <InfoItem
                label="Installation Date"
                value={asset.installationDate ? new Date(asset.installationDate).toLocaleDateString() : null}
              />
              <InfoItem
                label="Last Maintenance"
                value={asset.lastMaintenanceDate ? new Date(asset.lastMaintenanceDate).toLocaleDateString() : null}
              />
              <InfoItem
                label="Next Maintenance"
                value={asset.nextMaintenanceDate ? new Date(asset.nextMaintenanceDate).toLocaleDateString() : null}
              />
            </CardContent>
          </Card>

          <Card sx={{ mt: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Assignment
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <InfoItem label="Assigned To" value={asset.assignedToName} />
              <InfoItem label="Parent Asset" value={asset.parentAssetName} />
            </CardContent>
          </Card>
        </Grid>

        {asset.notes && (
          <Grid item xs={12}>
            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" gutterBottom>
                Notes
              </Typography>
              <Typography variant="body1" style={{ whiteSpace: 'pre-wrap' }}>
                {asset.notes}
              </Typography>
            </Paper>
          </Grid>
        )}

        <Grid item xs={12}>
          <AttachmentManager
            entityType="Asset"
            entityId={asset.id}
            canEdit={hasPermission('assets.edit')}
          />
        </Grid>
      </Grid>
    </Box>
  );
};
