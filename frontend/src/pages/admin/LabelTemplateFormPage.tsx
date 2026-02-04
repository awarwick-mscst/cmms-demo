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
  FormControlLabel,
  Switch,
  Tabs,
  Tab,
  Divider,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Save as SaveIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { labelService } from '../../services/labelService';
import {
  LabelElement,
  CreateLabelTemplateRequest,
  DpiOptions,
} from '../../types';
import { LabelDesigner } from '../../components/admin/LabelDesigner';

// Common label sizes in inches
const LABEL_PRESETS = [
  { name: 'Custom', width: 0, height: 0 },
  { name: '2" x 1" (Standard)', width: 2.0, height: 1.0 },
  { name: '2.25" x 1.25"', width: 2.25, height: 1.25 },
  { name: '2" x 0.5" (Small)', width: 2.0, height: 0.5 },
  { name: '3" x 1"', width: 3.0, height: 1.0 },
  { name: '3" x 2"', width: 3.0, height: 2.0 },
  { name: '4" x 2"', width: 4.0, height: 2.0 },
  { name: '4" x 6" (Shipping)', width: 4.0, height: 6.0 },
];

export const LabelTemplateFormPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const isEdit = id && id !== 'new';

  const [activeTab, setActiveTab] = useState(0);
  const [selectedPreset, setSelectedPreset] = useState('Custom');
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

      // Check if matches a preset
      const preset = LABEL_PRESETS.find(
        p => p.width === template.data!.width && p.height === template.data!.height
      );
      setSelectedPreset(preset?.name || 'Custom');
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

  const handlePresetChange = (presetName: string) => {
    setSelectedPreset(presetName);
    const preset = LABEL_PRESETS.find(p => p.name === presetName);
    if (preset && preset.width > 0) {
      setFormData(prev => ({
        ...prev,
        width: preset.width,
        height: preset.height,
      }));
    }
  };

  const handleDimensionChange = (dimension: 'width' | 'height', value: number) => {
    setFormData(prev => ({ ...prev, [dimension]: value }));
    // Check if it now matches a preset
    const newDimensions = { ...formData, [dimension]: value };
    const preset = LABEL_PRESETS.find(
      p => p.width === newDimensions.width && p.height === newDimensions.height
    );
    setSelectedPreset(preset?.name || 'Custom');
  };

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  if (isEdit && isLoading) {
    return <Typography>Loading...</Typography>;
  }

  return (
    <Box sx={{ height: 'calc(100vh - 120px)', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <IconButton onClick={() => navigate('/admin/label-templates')} sx={{ mr: 2 }}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5" sx={{ flex: 1 }}>
          {isEdit ? 'Edit Label Template' : 'New Label Template'}
        </Typography>
        <Button
          variant="contained"
          startIcon={<SaveIcon />}
          onClick={handleSubmit}
          disabled={isSubmitting || !formData.name || elements.length === 0}
        >
          {isSubmitting ? 'Saving...' : 'Save Template'}
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {(error as any)?.response?.data?.errors?.[0] || 'An error occurred'}
        </Alert>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 2 }}>
        <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)}>
          <Tab label="Design" />
          <Tab label="Settings" />
        </Tabs>
      </Paper>

      {/* Design Tab */}
      {activeTab === 0 && (
        <Box sx={{ flex: 1, minHeight: 0 }}>
          <LabelDesigner
            width={formData.width || 2.0}
            height={formData.height || 1.0}
            dpi={formData.dpi || 203}
            elements={elements}
            onElementsChange={setElements}
          />
        </Box>
      )}

      {/* Settings Tab */}
      {activeTab === 1 && (
        <Paper sx={{ p: 3, maxWidth: 600 }}>
          <Typography variant="h6" sx={{ mb: 3 }}>
            Template Settings
          </Typography>

          <Grid container spacing={3}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Template Name"
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Small Part Label"
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
                placeholder="Describe when to use this template"
              />
            </Grid>

            <Grid item xs={12}>
              <Divider sx={{ my: 1 }} />
              <Typography variant="subtitle2" sx={{ mb: 2 }}>
                Label Size
              </Typography>
            </Grid>

            <Grid item xs={12}>
              <TextField
                fullWidth
                select
                label="Label Size Preset"
                value={selectedPreset}
                onChange={(e) => handlePresetChange(e.target.value)}
              >
                {LABEL_PRESETS.map((preset) => (
                  <MenuItem key={preset.name} value={preset.name}>
                    {preset.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                label="Width (inches)"
                type="number"
                inputProps={{ step: 0.25, min: 0.5, max: 8 }}
                value={formData.width}
                onChange={(e) => handleDimensionChange('width', parseFloat(e.target.value) || 2.0)}
              />
            </Grid>

            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                label="Height (inches)"
                type="number"
                inputProps={{ step: 0.25, min: 0.25, max: 8 }}
                value={formData.height}
                onChange={(e) => handleDimensionChange('height', parseFloat(e.target.value) || 1.0)}
              />
            </Grid>

            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                select
                label="Printer DPI"
                value={formData.dpi}
                onChange={(e) => setFormData({ ...formData, dpi: parseInt(e.target.value) })}
                helperText="Match your printer's DPI"
              >
                {DpiOptions.map((dpi) => (
                  <MenuItem key={dpi} value={dpi}>
                    {dpi} DPI
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12}>
              <Divider sx={{ my: 1 }} />
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
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', ml: 4 }}>
                The default template is used when no template is specified
              </Typography>
            </Grid>
          </Grid>
        </Paper>
      )}
    </Box>
  );
};
