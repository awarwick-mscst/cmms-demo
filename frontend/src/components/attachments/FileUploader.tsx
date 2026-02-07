import React, { useCallback, useState } from 'react';
import {
  Box,
  Button,
  LinearProgress,
  Typography,
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  IconButton,
  Paper,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  InsertDriveFile as FileIcon,
  Image as ImageIcon,
  Close as CloseIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
import {
  AllowedImageExtensions,
  AllowedDocumentExtensions,
  MaxFileSize,
} from '../../types';
import { attachmentService } from '../../services/attachmentService';

interface FileUploadItem {
  file: File;
  status: 'pending' | 'uploading' | 'success' | 'error';
  progress: number;
  error?: string;
}

interface FileUploaderProps {
  entityType: string;
  entityId: number;
  onUploadComplete?: () => void;
  maxFiles?: number;
}

export const FileUploader: React.FC<FileUploaderProps> = ({
  entityType,
  entityId,
  onUploadComplete,
  maxFiles = 10,
}) => {
  const [files, setFiles] = useState<FileUploadItem[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const allowedExtensions = [...AllowedImageExtensions, ...AllowedDocumentExtensions];

  const validateFile = (file: File): string | null => {
    const extension = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!allowedExtensions.includes(extension as any)) {
      return `File type not allowed. Allowed types: ${allowedExtensions.join(', ')}`;
    }
    if (file.size > MaxFileSize) {
      return `File size exceeds ${MaxFileSize / (1024 * 1024)} MB limit`;
    }
    if (file.size === 0) {
      return 'File is empty';
    }
    return null;
  };

  const addFiles = useCallback((newFiles: File[]) => {
    setError(null);

    const validFiles: FileUploadItem[] = [];
    const errors: string[] = [];

    for (const file of newFiles) {
      if (files.length + validFiles.length >= maxFiles) {
        errors.push(`Maximum ${maxFiles} files allowed`);
        break;
      }

      const validationError = validateFile(file);
      if (validationError) {
        errors.push(`${file.name}: ${validationError}`);
      } else {
        validFiles.push({
          file,
          status: 'pending',
          progress: 0,
        });
      }
    }

    if (errors.length > 0) {
      setError(errors.join('; '));
    }

    if (validFiles.length > 0) {
      setFiles(prev => [...prev, ...validFiles]);
    }
  }, [files.length, maxFiles]);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    const droppedFiles = Array.from(e.dataTransfer.files);
    addFiles(droppedFiles);
  }, [addFiles]);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const selectedFiles = Array.from(e.target.files);
      addFiles(selectedFiles);
    }
    e.target.value = '';
  }, [addFiles]);

  const removeFile = useCallback((index: number) => {
    setFiles(prev => prev.filter((_, i) => i !== index));
  }, []);

  const uploadFiles = async () => {
    if (files.length === 0) return;

    setIsUploading(true);
    setError(null);

    const pendingFiles = files.filter(f => f.status === 'pending');
    const filesToUpload = pendingFiles.map(f => f.file);

    try {
      // Update all pending files to uploading
      setFiles(prev =>
        prev.map(f =>
          f.status === 'pending' ? { ...f, status: 'uploading' as const } : f
        )
      );

      await attachmentService.uploadAttachments(
        entityType,
        entityId,
        filesToUpload,
        {
          onProgress: (progress) => {
            setFiles(prev =>
              prev.map(f =>
                f.status === 'uploading' ? { ...f, progress } : f
              )
            );
          },
        }
      );

      // Mark all as success
      setFiles(prev =>
        prev.map(f =>
          f.status === 'uploading' ? { ...f, status: 'success' as const, progress: 100 } : f
        )
      );

      // Clear successful files after a delay
      setTimeout(() => {
        setFiles(prev => prev.filter(f => f.status !== 'success'));
        onUploadComplete?.();
      }, 1500);

    } catch (err: any) {
      const errorMessage = err.response?.data?.errors?.[0] || err.message || 'Upload failed';
      setError(errorMessage);
      setFiles(prev =>
        prev.map(f =>
          f.status === 'uploading' ? { ...f, status: 'error' as const, error: errorMessage } : f
        )
      );
    } finally {
      setIsUploading(false);
    }
  };

  const getFileIcon = (file: File) => {
    if (file.type.startsWith('image/')) {
      return <ImageIcon color="primary" />;
    }
    return <FileIcon color="action" />;
  };

  const getStatusIcon = (status: FileUploadItem['status']) => {
    switch (status) {
      case 'success':
        return <SuccessIcon color="success" />;
      case 'error':
        return <ErrorIcon color="error" />;
      default:
        return null;
    }
  };

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Paper
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        sx={{
          p: 3,
          textAlign: 'center',
          border: '2px dashed',
          borderColor: isDragging ? 'primary.main' : 'grey.300',
          bgcolor: isDragging ? 'action.hover' : 'background.paper',
          cursor: 'pointer',
          transition: 'all 0.2s ease',
          '&:hover': {
            borderColor: 'primary.light',
            bgcolor: 'action.hover',
          },
        }}
      >
        <input
          type="file"
          id="file-upload"
          multiple
          accept={allowedExtensions.join(',')}
          onChange={handleFileSelect}
          style={{ display: 'none' }}
        />
        <label htmlFor="file-upload" style={{ cursor: 'pointer', display: 'block' }}>
          <UploadIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
          <Typography variant="h6" gutterBottom>
            Drag and drop files here
          </Typography>
          <Typography variant="body2" color="text.secondary">
            or click to browse
          </Typography>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 1 }}>
            Allowed: Images (jpg, png, gif, webp) and Documents (pdf, doc, docx, xls, xlsx, txt)
          </Typography>
          <Typography variant="caption" color="text.secondary" display="block">
            Max size: 10 MB per file
          </Typography>
        </label>
      </Paper>

      {files.length > 0 && (
        <Box sx={{ mt: 2 }}>
          <List dense>
            {files.map((item, index) => (
              <ListItem
                key={`${item.file.name}-${index}`}
                secondaryAction={
                  item.status === 'pending' && (
                    <IconButton edge="end" onClick={() => removeFile(index)} size="small">
                      <CloseIcon />
                    </IconButton>
                  )
                }
              >
                <ListItemIcon>{getFileIcon(item.file)}</ListItemIcon>
                <ListItemText
                  primary={
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body2" noWrap sx={{ maxWidth: 200 }}>
                        {item.file.name}
                      </Typography>
                      {getStatusIcon(item.status)}
                    </Box>
                  }
                  secondary={
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        {attachmentService.formatFileSize(item.file.size)}
                      </Typography>
                      {item.status === 'uploading' && (
                        <LinearProgress
                          variant="determinate"
                          value={item.progress}
                          sx={{ mt: 0.5 }}
                        />
                      )}
                      {item.status === 'error' && item.error && (
                        <Typography variant="caption" color="error">
                          {item.error}
                        </Typography>
                      )}
                    </Box>
                  }
                />
              </ListItem>
            ))}
          </List>

          <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
            <Button
              variant="contained"
              onClick={uploadFiles}
              disabled={isUploading || files.every(f => f.status !== 'pending')}
              startIcon={<UploadIcon />}
            >
              {isUploading ? 'Uploading...' : 'Upload Files'}
            </Button>
            <Button
              variant="outlined"
              onClick={() => setFiles([])}
              disabled={isUploading}
            >
              Clear All
            </Button>
          </Box>
        </Box>
      )}
    </Box>
  );
};
