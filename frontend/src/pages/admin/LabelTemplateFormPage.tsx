import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Paper,
  Typography,
  TextField,
  Grid,
  Alert,
  MenuItem,
  IconButton,
  Card,
  CardContent,
  FormControlLabel,
  Switch,
  Divider,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  ArrowBack as ArrowBackIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { labelService } from '../../services/labelService';
import {
  LabelElement,
  CreateLabelTemplateRequest,
  LabelFieldOptions,
  DpiOptions,
} from '../../types';

export const LabelTemplateFormPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const isEdit = id && id !== 'new';

  const [formData, setFormData] = useState<CreateLabelTemplateRequest>({
    name: '',
    description: '',
    width: 2.0,
    height: 1.0,
    dpi: 203,
    elementsJson: '[]',
    isDefault: false,
  });

  const [elements, setElements] = useState<LabelElement[]>([]);
  const [previewZpl, setPreviewZpl] = useState<string>('');

  const { data: template, isLoading } = useQuery({
    queryKey: ['label-template', id],
    queryFn: () => labelService.getTemplate(parseInt(id!)),
    enabled: !!isEdit,
  });

  useEffect(() => {
    if (template?.data) {
      setFormData({
        name: template.data.name,
        description: template.data.description || '',
        width: template.data.width,
        height: template.data.height,
        dpi: template.data.dpi,
        elementsJson: template.data.elementsJson,
        isDefault: template.data.isDefault,
      });
      try {
        const parsed = JSON.parse(template.data.elementsJson);
        setElements(Array.isArray(parsed) ? parsed : []);
      } catch {
        setElements([]);
      }
    }
  }, [template]);

  const createMutation = useMutation({
    mutationFn: labelService.createTemplate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['label-templates'] });
      navigate('/admin/label-templates');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreateLabelTemplateRequest }) =>
      labelService.updateTemplate(id, {
        ...data,
        width: data.width || 2.0,
        height: data.height || 1.0,
        dpi: data.dpi || 203,
        isDefault: data.isDefault ?? false,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['label-templates'] });
      navigate('/admin/label-templates');
    },
  });

  const handleSubmit = () => {
    const elementsJson = JSON.stringify(elements);
    const data = { ...formData, elementsJson };

    if (isEdit) {
      updateMutation.mutate({ id: parseInt(id!), data });
    } else {
      createMutation.mutate(data);
    }
  };

  const handleAddElement = (type: 'text' | 'barcode') => {
    const newElement: LabelElement = {
      type,
      field: 'partNumber',
      x: 10,
      y: (elements.length + 1) * 30,
      ...(type === 'text' ? { fontSize: 25, maxWidth: 180 } : { height: 50 }),
    };
    setElements([...elements, newElement]);
  };

  const handleUpdateElement = (index: number, updates: Partial<LabelElement>) => {
    const newElements = [...elements];
    newElements[index] = { ...newElements[index], ...updates };
    setElements(newElements);
  };

  const handleRemoveElement = (index: number) => {
    setElements(elements.filter((_, i) => i !== index));
  };

  const handleMoveElement = (index: number, direction: 'up' | 'down') => {
    if (direction === 'up' && index === 0) return;
    if (direction === 'down' && index === elements.length - 1) return;

    const newElements = [...elements];
    const swapIndex = direction === 'up' ? index - 1 : index + 1;
    [newElements[index], newElements[swapIndex]] = [newElements[swapIndex], newElements[index]];
    setElements(newElements);
  };

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  if (isEdit && isLoading) {
    return <Typography>Loading...</Typography>;
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/admin/label-templates')} sx={{ mr: 2 }}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">
          {isEdit ? 'Edit Label Template' : 'New Label Template'}
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {(error as any)?.response?.data?.errors?.[0] || 'An error occurred'}
        </Alert>
      )}

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Template Settings
            </Typography>

            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Template Name"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  multiline
                  rows={2}
                  label="Description"
                  value={formData.description || ''}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="Width (inches)"
                  type="number"
                  inputProps={{ step: 0.25, min: 0.5, max: 8 }}
                  value={formData.width}
                  onChange={(e) =>
                    setFormData({ ...formData, width: parseFloat(e.target.value) || 2.0 })
                  }
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="Height (inches)"
                  type="number"
                  inputProps={{ step: 0.25, min: 0.25, max: 8 }}
                  value={formData.height}
                  onChange={(e) =>
                    setFormData({ ...formData, height: parseFloat(e.target.value) || 1.0 })
                  }
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  select
                  label="DPI"
                  value={formData.dpi}
                  onChange={(e) => setFormData({ ...formData, dpi: parseInt(e.target.value) })}
                >
                  {DpiOptions.map((dpi) => (
                    <MenuItem key={dpi} value={dpi}>
                      {dpi} DPI
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.isDefault ?? false}
                      onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
                    />
                  }
                  label="Set as default template"
                />
              </Grid>
            </Grid>
          </Paper>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6">Label Elements</Typography>
              <Box>
                <Button
                  size="small"
                  startIcon={<AddIcon />}
                  onClick={() => handleAddElement('text')}
                  sx={{ mr: 1 }}
                >
                  Add Text
                </Button>
                <Button
                  size="small"
                  startIcon={<AddIcon />}
                  onClick={() => handleAddElement('barcode')}
                >
                  Add Barcode
                </Button>
              </Box>
            </Box>

            {elements.length === 0 ? (
              <Typography color="textSecondary" sx={{ py: 4, textAlign: 'center' }}>
                No elements. Add text or barcode elements above.
              </Typography>
            ) : (
              <Box sx={{ maxHeight: 400, overflowY: 'auto' }}>
                {elements.map((element, index) => (
                  <Card key={index} variant="outlined" sx={{ mb: 2 }}>
                    <CardContent sx={{ py: 2, '&:last-child': { pb: 2 } }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="subtitle2" color="primary">
                          {element.type === 'text' ? 'Text Element' : 'Barcode Element'}
                        </Typography>
                        <IconButton size="small" onClick={() => handleRemoveElement(index)} color="error">
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Box>

                      <Grid container spacing={1}>
                        <Grid item xs={12}>
                          <TextField
                            fullWidth
                            size="small"
                            select
                            label="Field"
                            value={element.field}
                            onChange={(e) =>
                              handleUpdateElement(index, { field: e.target.value })
                            }
                          >
                            {LabelFieldOptions.map((option) => (
                              <MenuItem key={option.value} value={option.value}>
                                {option.label}
                              </MenuItem>
                            ))}
                          </TextField>
                        </Grid>
                        <Grid item xs={6}>
                          <TextField
                            fullWidth
                            size="small"
                            label="X (dots)"
                            type="number"
                            value={element.x}
                            onChange={(e) =>
                              handleUpdateElement(index, { x: parseInt(e.target.value) || 0 })
                            }
                          />
                        </Grid>
                        <Grid item xs={6}>
                          <TextField
                            fullWidth
                            size="small"
                            label="Y (dots)"
                            type="number"
                            value={element.y}
                            onChange={(e) =>
                              handleUpdateElement(index, { y: parseInt(e.target.value) || 0 })
                            }
                          />
                        </Grid>
                        {element.type === 'text' && (
                          <>
                            <Grid item xs={6}>
                              <TextField
                                fullWidth
                                size="small"
                                label="Font Size"
                                type="number"
                                value={element.fontSize || 25}
                                onChange={(e) =>
                                  handleUpdateElement(index, {
                                    fontSize: parseInt(e.target.value) || 25,
                                  })
                                }
                              />
                            </Grid>
                            <Grid item xs={6}>
                              <TextField
                                fullWidth
                                size="small"
                                label="Max Width"
                                type="number"
                                value={element.maxWidth || 0}
                                onChange={(e) =>
                                  handleUpdateElement(index, {
                                    maxWidth: parseInt(e.target.value) || 0,
                                  })
                                }
                              />
                            </Grid>
                          </>
                        )}
                        {element.type === 'barcode' && (
                          <Grid item xs={12}>
                            <TextField
                              fullWidth
                              size="small"
                              label="Height (dots)"
                              type="number"
                              value={element.height || 50}
                              onChange={(e) =>
                                handleUpdateElement(index, {
                                  height: parseInt(e.target.value) || 50,
                                })
                              }
                            />
                          </Grid>
                        )}
                      </Grid>
                    </CardContent>
                  </Card>
                ))}
              </Box>
            )}
          </Paper>
        </Grid>
      </Grid>

      <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
        <Button onClick={() => navigate('/admin/label-templates')}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={isSubmitting || !formData.name || elements.length === 0}
        >
          {isSubmitting ? 'Saving...' : 'Save Template'}
        </Button>
      </Box>
    </Box>
  );
};
