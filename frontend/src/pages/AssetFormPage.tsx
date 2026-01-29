import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import {
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  MenuItem,
  TextField,
  Typography,
  Alert,
} from '@mui/material';
import { Save as SaveIcon, ArrowBack as BackIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { assetService } from '../services/assetService';
import { CreateAssetRequest, AssetStatuses, AssetCriticalities } from '../types';
import { LoadingSpinner } from '../components/common/LoadingSpinner';

export const AssetFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = !!id;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateAssetRequest>({
    defaultValues: {
      status: 'Active',
      criticality: 'Medium',
    },
  });

  const { data: assetData, isLoading: isLoadingAsset } = useQuery({
    queryKey: ['asset', id],
    queryFn: () => assetService.getAsset(Number(id)),
    enabled: isEdit,
  });

  const { data: categoriesData } = useQuery({
    queryKey: ['categories'],
    queryFn: () => assetService.getCategories(),
  });

  const { data: locationsData } = useQuery({
    queryKey: ['locations'],
    queryFn: () => assetService.getLocations(),
  });

  useEffect(() => {
    if (assetData?.data) {
      const asset = assetData.data;
      reset({
        name: asset.name,
        description: asset.description,
        categoryId: asset.categoryId,
        locationId: asset.locationId,
        status: asset.status,
        criticality: asset.criticality,
        manufacturer: asset.manufacturer,
        model: asset.model,
        serialNumber: asset.serialNumber,
        barcode: asset.barcode,
        purchaseDate: asset.purchaseDate?.split('T')[0],
        purchaseCost: asset.purchaseCost,
        warrantyExpiry: asset.warrantyExpiry?.split('T')[0],
        expectedLifeYears: asset.expectedLifeYears,
        installationDate: asset.installationDate?.split('T')[0],
        parentAssetId: asset.parentAssetId,
        assignedTo: asset.assignedTo,
        notes: asset.notes,
      });
    }
  }, [assetData, reset]);

  const createMutation = useMutation({
    mutationFn: assetService.createAsset,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['assets'] });
      navigate(`/assets/${data.data?.id}`);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) => assetService.updateAsset(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assets'] });
      queryClient.invalidateQueries({ queryKey: ['asset', id] });
      navigate(`/assets/${id}`);
    },
  });

  const onSubmit = (data: CreateAssetRequest) => {
    if (isEdit) {
      updateMutation.mutate({ id: Number(id), data });
    } else {
      createMutation.mutate(data);
    }
  };

  const flattenCategories = (categories: any[], level = 0): any[] => {
    return categories.reduce((acc, cat) => {
      acc.push({ ...cat, indent: level });
      if (cat.children?.length) {
        acc.push(...flattenCategories(cat.children, level + 1));
      }
      return acc;
    }, []);
  };

  const flatCategories = flattenCategories(categoriesData?.data || []);
  const flatLocations = locationsData?.data || [];

  if (isEdit && isLoadingAsset) {
    return <LoadingSpinner message="Loading asset..." />;
  }

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/assets')}>
          Back
        </Button>
        <Typography variant="h5">{isEdit ? 'Edit Asset' : 'New Asset'}</Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {(error as any)?.response?.data?.errors?.[0] || 'An error occurred'}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit)}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  General Information
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="name"
                      control={control}
                      rules={{ required: 'Name is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Asset Name"
                          error={!!errors.name}
                          helperText={errors.name?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="categoryId"
                      control={control}
                      rules={{ required: 'Category is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          fullWidth
                          label="Category"
                          error={!!errors.categoryId}
                          helperText={errors.categoryId?.message}
                        >
                          {flatCategories.map((cat) => (
                            <MenuItem key={cat.id} value={cat.id}>
                              {'  '.repeat(cat.indent)}{cat.name}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="locationId"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Location">
                          <MenuItem value="">None</MenuItem>
                          {flatLocations.map((loc) => (
                            <MenuItem key={loc.id} value={loc.id}>
                              {loc.fullPath || loc.name}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="status"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Status">
                          {AssetStatuses.map((status) => (
                            <MenuItem key={status} value={status}>
                              {status}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="criticality"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Criticality">
                          {AssetCriticalities.map((crit) => (
                            <MenuItem key={crit} value={crit}>
                              {crit}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="description"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth multiline rows={3} label="Description" />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Technical Details
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="manufacturer"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth label="Manufacturer" />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="model"
                      control={control}
                      render={({ field }) => <TextField {...field} fullWidth label="Model" />}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="serialNumber"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth label="Serial Number" />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="barcode"
                      control={control}
                      render={({ field }) => <TextField {...field} fullWidth label="Barcode" />}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Financial
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="purchaseDate"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="date"
                          label="Purchase Date"
                          InputLabelProps={{ shrink: true }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="purchaseCost"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Purchase Cost"
                          InputProps={{ startAdornment: '$' }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="warrantyExpiry"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="date"
                          label="Warranty Expiry"
                          InputLabelProps={{ shrink: true }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="expectedLifeYears"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth type="number" label="Expected Life (Years)" />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Notes
                </Typography>
                <Controller
                  name="notes"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} fullWidth multiline rows={4} label="Notes" />
                  )}
                />
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
              <Button variant="outlined" onClick={() => navigate('/assets')}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                startIcon={<SaveIcon />}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : 'Save Asset'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};
