import React, { useState, useRef, useCallback, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  MenuItem,
  IconButton,
  Button,
  Slider,
  Divider,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
} from '@mui/material';
import {
  Delete as DeleteIcon,
  TextFields as TextIcon,
  QrCode as BarcodeIcon,
  ZoomIn as ZoomInIcon,
  ZoomOut as ZoomOutIcon,
  CenterFocusStrong as CenterIcon,
} from '@mui/icons-material';
import { LabelElement, LabelFieldOptions } from '../../types';

interface LabelDesignerProps {
  width: number;  // inches
  height: number; // inches
  dpi: number;
  elements: LabelElement[];
  onElementsChange: (elements: LabelElement[]) => void;
}

const SCALE_FACTORS = [0.5, 0.75, 1, 1.25, 1.5, 2];
const BASE_PPI = 96; // Base pixels per inch for screen display

export const LabelDesigner: React.FC<LabelDesignerProps> = ({
  width,
  height,
  dpi,
  elements,
  onElementsChange,
}) => {
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [scale, setScale] = useState(1);
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const canvasRef = useRef<HTMLDivElement>(null);

  // Convert inches to screen pixels (for display)
  const labelWidthPx = width * BASE_PPI * scale;
  const labelHeightPx = height * BASE_PPI * scale;

  // Convert dots to screen pixels
  const dotsToPixels = useCallback((dots: number) => {
    return (dots / dpi) * BASE_PPI * scale;
  }, [dpi, scale]);

  // Convert screen pixels to dots
  const pixelsToDots = useCallback((pixels: number) => {
    return Math.round((pixels / scale / BASE_PPI) * dpi);
  }, [dpi, scale]);

  const handleAddElement = (type: 'text' | 'barcode') => {
    const newElement: LabelElement = {
      type,
      field: 'partNumber',
      x: 20,
      y: 20 + elements.length * 60,
      ...(type === 'text'
        ? { fontSize: 30, maxWidth: Math.round(width * dpi * 0.8) }
        : { height: 60, barcodeWidth: 2 }),
    };
    const newElements = [...elements, newElement];
    onElementsChange(newElements);
    setSelectedIndex(newElements.length - 1);
  };

  const handleUpdateElement = (index: number, updates: Partial<LabelElement>) => {
    const newElements = [...elements];
    newElements[index] = { ...newElements[index], ...updates };
    onElementsChange(newElements);
  };

  const handleDeleteElement = (index: number) => {
    onElementsChange(elements.filter((_, i) => i !== index));
    setSelectedIndex(null);
  };

  const handleCanvasClick = (e: React.MouseEvent) => {
    if (e.target === canvasRef.current) {
      setSelectedIndex(null);
    }
  };

  const handleElementMouseDown = (e: React.MouseEvent, index: number) => {
    e.stopPropagation();
    setSelectedIndex(index);
    setIsDragging(true);

    const element = elements[index];
    const elementX = dotsToPixels(element.x);
    const elementY = dotsToPixels(element.y);

    const rect = canvasRef.current?.getBoundingClientRect();
    if (rect) {
      setDragOffset({
        x: e.clientX - rect.left - elementX,
        y: e.clientY - rect.top - elementY,
      });
    }
  };

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging || selectedIndex === null || !canvasRef.current) return;

    const rect = canvasRef.current.getBoundingClientRect();
    const newX = e.clientX - rect.left - dragOffset.x;
    const newY = e.clientY - rect.top - dragOffset.y;

    // Convert to dots and clamp to label bounds
    const maxX = width * dpi;
    const maxY = height * dpi;
    const dotX = Math.max(0, Math.min(pixelsToDots(newX), maxX - 10));
    const dotY = Math.max(0, Math.min(pixelsToDots(newY), maxY - 10));

    handleUpdateElement(selectedIndex, { x: dotX, y: dotY });
  }, [isDragging, selectedIndex, dragOffset, width, height, dpi, pixelsToDots]);

  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  useEffect(() => {
    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, handleMouseMove, handleMouseUp]);

  const selectedElement = selectedIndex !== null ? elements[selectedIndex] : null;

  // Get approximate font size in pixels for display
  const getFontSizePixels = (fontSize: number) => {
    return dotsToPixels(fontSize) * 0.8;
  };

  // Get barcode display dimensions
  const getBarcodeDisplayHeight = (element: LabelElement) => {
    return dotsToPixels(element.height || 50);
  };

  const getBarcodeDisplayWidth = (element: LabelElement) => {
    // Approximate barcode width based on module width and typical Code128 encoding
    const moduleWidth = element.barcodeWidth || 2;
    const estimatedModules = 80; // Approximate for short Code128
    return dotsToPixels(moduleWidth * estimatedModules);
  };

  return (
    <Box sx={{ display: 'flex', gap: 2, height: '100%' }}>
      {/* Canvas Area */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Toolbar */}
        <Paper sx={{ p: 1, mb: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
          <Button
            size="small"
            startIcon={<TextIcon />}
            onClick={() => handleAddElement('text')}
            variant="outlined"
          >
            Add Text
          </Button>
          <Button
            size="small"
            startIcon={<BarcodeIcon />}
            onClick={() => handleAddElement('barcode')}
            variant="outlined"
          >
            Add Barcode
          </Button>
          <Divider orientation="vertical" flexItem sx={{ mx: 1 }} />
          <Tooltip title="Zoom Out">
            <IconButton
              size="small"
              onClick={() => setScale(s => Math.max(0.5, s - 0.25))}
              disabled={scale <= 0.5}
            >
              <ZoomOutIcon />
            </IconButton>
          </Tooltip>
          <Typography variant="body2" sx={{ minWidth: 50, textAlign: 'center' }}>
            {Math.round(scale * 100)}%
          </Typography>
          <Tooltip title="Zoom In">
            <IconButton
              size="small"
              onClick={() => setScale(s => Math.min(2, s + 0.25))}
              disabled={scale >= 2}
            >
              <ZoomInIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Reset Zoom">
            <IconButton size="small" onClick={() => setScale(1)}>
              <CenterIcon />
            </IconButton>
          </Tooltip>
          <Box sx={{ flex: 1 }} />
          <Typography variant="body2" color="text.secondary">
            {width}" × {height}" @ {dpi} DPI
          </Typography>
        </Paper>

        {/* Label Canvas */}
        <Box
          sx={{
            flex: 1,
            overflow: 'auto',
            bgcolor: 'grey.200',
            borderRadius: 1,
            p: 3,
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'flex-start',
          }}
        >
          <Paper
            ref={canvasRef}
            onClick={handleCanvasClick}
            sx={{
              width: labelWidthPx,
              height: labelHeightPx,
              minWidth: labelWidthPx,
              minHeight: labelHeightPx,
              position: 'relative',
              bgcolor: 'white',
              cursor: 'default',
              boxShadow: 3,
              // Grid background for alignment help
              backgroundImage: `
                linear-gradient(rgba(0,0,0,0.05) 1px, transparent 1px),
                linear-gradient(90deg, rgba(0,0,0,0.05) 1px, transparent 1px)
              `,
              backgroundSize: `${BASE_PPI * scale / 4}px ${BASE_PPI * scale / 4}px`,
            }}
          >
            {elements.map((element, index) => {
              const isSelected = selectedIndex === index;
              const x = dotsToPixels(element.x);
              const y = dotsToPixels(element.y);

              if (element.type === 'text') {
                const fontSize = getFontSizePixels(element.fontSize || 25);
                const maxWidth = element.maxWidth ? dotsToPixels(element.maxWidth) : undefined;

                return (
                  <Box
                    key={index}
                    onMouseDown={(e) => handleElementMouseDown(e, index)}
                    sx={{
                      position: 'absolute',
                      left: x,
                      top: y,
                      fontSize: `${fontSize}px`,
                      fontFamily: 'monospace',
                      fontWeight: 'bold',
                      cursor: isDragging && isSelected ? 'grabbing' : 'grab',
                      userSelect: 'none',
                      padding: '2px 4px',
                      border: isSelected ? '2px solid #1976d2' : '2px solid transparent',
                      borderRadius: 1,
                      bgcolor: isSelected ? 'rgba(25, 118, 210, 0.1)' : 'transparent',
                      maxWidth: maxWidth,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      '&:hover': {
                        border: isSelected ? '2px solid #1976d2' : '2px dashed #666',
                      },
                    }}
                  >
                    {LabelFieldOptions.find(f => f.value === element.field)?.label || element.field}
                  </Box>
                );
              } else {
                // Barcode element
                const barcodeHeight = getBarcodeDisplayHeight(element);
                const barcodeWidth = getBarcodeDisplayWidth(element);

                return (
                  <Box
                    key={index}
                    onMouseDown={(e) => handleElementMouseDown(e, index)}
                    sx={{
                      position: 'absolute',
                      left: x,
                      top: y,
                      cursor: isDragging && isSelected ? 'grabbing' : 'grab',
                      userSelect: 'none',
                      border: isSelected ? '2px solid #1976d2' : '2px solid transparent',
                      borderRadius: 1,
                      bgcolor: isSelected ? 'rgba(25, 118, 210, 0.1)' : 'transparent',
                      padding: '2px',
                      '&:hover': {
                        border: isSelected ? '2px solid #1976d2' : '2px dashed #666',
                      },
                    }}
                  >
                    {/* Barcode visualization */}
                    <Box
                      sx={{
                        width: barcodeWidth,
                        height: barcodeHeight,
                        display: 'flex',
                        alignItems: 'stretch',
                      }}
                    >
                      {/* Generate barcode-like pattern */}
                      {Array.from({ length: 30 }).map((_, i) => (
                        <Box
                          key={i}
                          sx={{
                            flex: Math.random() > 0.5 ? 2 : 1,
                            bgcolor: i % 2 === 0 ? 'black' : 'white',
                          }}
                        />
                      ))}
                    </Box>
                    {/* Human readable text below */}
                    <Typography
                      variant="caption"
                      sx={{
                        display: 'block',
                        textAlign: 'center',
                        fontFamily: 'monospace',
                        fontSize: Math.max(8, barcodeHeight * 0.2),
                        mt: 0.5,
                      }}
                    >
                      {LabelFieldOptions.find(f => f.value === element.field)?.label || element.field}
                    </Typography>
                  </Box>
                );
              }
            })}
          </Paper>
        </Box>
      </Box>

      {/* Properties Panel */}
      <Paper sx={{ width: 280, p: 2, display: 'flex', flexDirection: 'column' }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Properties
        </Typography>

        {selectedElement ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="subtitle2" color="primary">
                {selectedElement.type === 'text' ? 'Text Element' : 'Barcode Element'}
              </Typography>
              <IconButton
                size="small"
                onClick={() => handleDeleteElement(selectedIndex!)}
                color="error"
              >
                <DeleteIcon />
              </IconButton>
            </Box>

            <TextField
              fullWidth
              size="small"
              select
              label="Data Field"
              value={selectedElement.field}
              onChange={(e) => handleUpdateElement(selectedIndex!, { field: e.target.value })}
            >
              {LabelFieldOptions.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>

            <Divider />

            <Typography variant="caption" color="text.secondary">
              Position (dots)
            </Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <TextField
                size="small"
                label="X"
                type="number"
                value={selectedElement.x}
                onChange={(e) => handleUpdateElement(selectedIndex!, { x: parseInt(e.target.value) || 0 })}
                inputProps={{ min: 0, max: width * dpi }}
              />
              <TextField
                size="small"
                label="Y"
                type="number"
                value={selectedElement.y}
                onChange={(e) => handleUpdateElement(selectedIndex!, { y: parseInt(e.target.value) || 0 })}
                inputProps={{ min: 0, max: height * dpi }}
              />
            </Box>

            {selectedElement.type === 'text' && (
              <>
                <Divider />
                <Typography variant="caption" color="text.secondary">
                  Font Size: {selectedElement.fontSize || 25} dots
                </Typography>
                <Slider
                  value={selectedElement.fontSize || 25}
                  onChange={(_, value) => handleUpdateElement(selectedIndex!, { fontSize: value as number })}
                  min={15}
                  max={100}
                  step={5}
                  marks={[
                    { value: 15, label: 'S' },
                    { value: 40, label: 'M' },
                    { value: 70, label: 'L' },
                    { value: 100, label: 'XL' },
                  ]}
                />

                <TextField
                  size="small"
                  label="Max Width (dots)"
                  type="number"
                  value={selectedElement.maxWidth || 0}
                  onChange={(e) => handleUpdateElement(selectedIndex!, { maxWidth: parseInt(e.target.value) || 0 })}
                  helperText="0 = no limit"
                  inputProps={{ min: 0, max: width * dpi }}
                />
              </>
            )}

            {selectedElement.type === 'barcode' && (
              <>
                <Divider />
                <Typography variant="caption" color="text.secondary">
                  Barcode Height: {selectedElement.height || 50} dots
                </Typography>
                <Slider
                  value={selectedElement.height || 50}
                  onChange={(_, value) => handleUpdateElement(selectedIndex!, { height: value as number })}
                  min={30}
                  max={150}
                  step={5}
                  marks={[
                    { value: 30, label: 'S' },
                    { value: 70, label: 'M' },
                    { value: 110, label: 'L' },
                    { value: 150, label: 'XL' },
                  ]}
                />

                <Typography variant="caption" color="text.secondary">
                  Bar Width: {selectedElement.barcodeWidth || 2} dots
                </Typography>
                <Slider
                  value={selectedElement.barcodeWidth || 2}
                  onChange={(_, value) => handleUpdateElement(selectedIndex!, { barcodeWidth: value as number })}
                  min={1}
                  max={5}
                  step={1}
                  marks={[
                    { value: 1, label: '1' },
                    { value: 2, label: '2' },
                    { value: 3, label: '3' },
                    { value: 4, label: '4' },
                    { value: 5, label: '5' },
                  ]}
                />
                <Typography variant="caption" color="text.secondary">
                  Larger bar width = easier to scan, but takes more space
                </Typography>
              </>
            )}

            <Divider />
            <Typography variant="caption" color="text.secondary">
              Tip: Drag elements on the canvas to position them visually
            </Typography>
          </Box>
        ) : (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <Typography color="text.secondary" sx={{ mb: 2 }}>
              Select an element on the canvas to edit its properties
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Or add a new element using the toolbar above
            </Typography>
          </Box>
        )}
      </Paper>
    </Box>
  );
};
