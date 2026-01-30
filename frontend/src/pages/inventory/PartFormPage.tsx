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
import { ArrowBack as BackIcon, Save as SaveIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { partService } from '../../services/partService';
import { partCategoryService } from '../../services/partCategoryService';
import { supplierService } from '../../services/supplierService';
import { CreatePartRequest, PartStatuses, UnitsOfMeasure } from '../../types';

export const PartFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = !!id;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreatePartRequest>({
    defaultValues: {
      status: 'Active',
      unitOfMeasure: 'Each',
      unitCost: 0,
      reorderPoint: 0,
      reorderQuantity: 0,
      minStockLevel: 0,
      maxStockLevel: 0,
      leadTimeDays: 0,
    },
  });

  const { data: partData, isLoading: isLoadingPart } = useQuery({
    queryKey: ['part', id],
    queryFn: () => partService.getPart(Number(id)),
    enabled: isEdit,
  });

  const { data: categoriesData } = useQuery({
    queryKey: ['partCategories'],
    queryFn: () => partCategoryService.getCategories(),
  });

  const { data: suppliersData } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers({ pageSize: 100 }),
  });

  useEffect(() => {
    if (partData?.data) {
      const part = partData.data;
      reset({
        name: part.name,
        description: part.description,
        categoryId: part.categoryId,
        supplierId: part.supplierId,
        unitOfMeasure: part.unitOfMeasure,
        unitCost: part.unitCost,
        reorderPoint: part.reorderPoint,
        reorderQuantity: part.reorderQuantity,
        status: part.status,
        minStockLevel: part.minStockLevel,
        maxStockLevel: part.maxStockLevel,
        leadTimeDays: part.leadTimeDays,
        specifications: part.specifications,
        manufacturer: part.manufacturer,
        manufacturerPartNumber: part.manufacturerPartNumber,
        barcode: part.barcode,
        imageUrl: part.imageUrl,
        notes: part.notes,
      });
    }
  }, [partData, reset]);

  const createMutation = useMutation({
    mutationFn: partService.createPart,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['parts'] });
      navigate(`/inventory/parts/${data.data?.id}`);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) => partService.updatePart(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['parts'] });
      queryClient.invalidateQueries({ queryKey: ['part', id] });
      navigate(`/inventory/parts/${id}`);
    },
  });

  const onSubmit = (data: CreatePartRequest) => {
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

  if (isEdit && isLoadingPart) {
    return <Typography>Loading part...</Typography>;
  }

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/inventory/parts')}>
          Back
        </Button>
        <Typography variant="h5">{isEdit ? 'Edit Part' : 'New Part'}</Typography>
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
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="partNumber"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Part Number"
                          placeholder="Auto-generated if empty"
                          disabled={isEdit}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="name"
                      control={control}
                      rules={{ required: 'Name is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Part Name"
                          required
                          error={!!errors.name}
                          helperText={errors.name?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="description"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          multiline
                          rows={2}
                          label="Description"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="categoryId"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          fullWidth
                          label="Category"
                          value={field.value || ''}
                        >
                          <MenuItem value="">None</MenuItem>
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
                      name="supplierId"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          fullWidth
                          label="Supplier"
                          value={field.value || ''}
                        >
                          <MenuItem value="">None</MenuItem>
                          {suppliersData?.items?.map((supplier) => (
                            <MenuItem key={supplier.id} value={supplier.id}>
                              {supplier.name}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
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
                      name="manufacturerPartNumber"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth label="Manufacturer Part #" />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="barcode"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth label="Barcode" />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="status"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Status">
                          {PartStatuses.map((status) => (
                            <MenuItem key={status} value={status}>
                              {status}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
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
                  Pricing & Stock
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="unitOfMeasure"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Unit">
                          {UnitsOfMeasure.map((unit) => (
                            <MenuItem key={unit} value={unit}>
                              {unit}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="unitCost"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Unit Cost"
                          inputProps={{ min: 0, step: 0.01 }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="reorderPoint"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Reorder Point"
                          inputProps={{ min: 0 }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="reorderQuantity"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Reorder Qty"
                          inputProps={{ min: 0 }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="minStockLevel"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Min Stock"
                          inputProps={{ min: 0 }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="maxStockLevel"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Max Stock"
                          inputProps={{ min: 0 }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="leadTimeDays"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Lead Time (Days)"
                          inputProps={{ min: 0 }}
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Additional Details
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="specifications"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          multiline
                          rows={3}
                          label="Specifications"
                          placeholder="Technical specifications, dimensions, etc."
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="notes"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          multiline
                          rows={2}
                          label="Notes"
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
              <Button variant="outlined" onClick={() => navigate('/inventory/parts')}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                startIcon={<SaveIcon />}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : 'Save Part'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};
