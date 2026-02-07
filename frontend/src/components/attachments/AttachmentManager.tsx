import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Collapse,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Image as ImageIcon,
  Description as DocIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Attachment } from '../../types';
import { attachmentService } from '../../services/attachmentService';
import { FileUploader } from './FileUploader';
import { ImageGallery } from './ImageGallery';
import { DocumentList } from './DocumentList';
import { ConfirmDialog } from '../common/ConfirmDialog';

interface AttachmentManagerProps {
  entityType: string;
  entityId: number;
  title?: string;
  canEdit?: boolean;
  defaultExpanded?: boolean;
}

export const AttachmentManager: React.FC<AttachmentManagerProps> = ({
  entityType,
  entityId,
  title = 'Attachments',
  canEdit = true,
  defaultExpanded = true,
}) => {
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [expanded, setExpanded] = useState(defaultExpanded);

  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['attachments', entityType, entityId],
    queryFn: () => attachmentService.getAttachments(entityType, entityId),
    enabled: !!entityType && entityId > 0,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => attachmentService.deleteAttachment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['attachments', entityType, entityId] });
      queryClient.invalidateQueries({ queryKey: ['primaryImage', entityType, entityId] });
      setDeleteDialogOpen(false);
      setDeleteId(null);
    },
  });

  const setPrimaryMutation = useMutation({
    mutationFn: (id: number) => attachmentService.setPrimaryImage(id),
    onSuccess: () => {
      // Refetch to ensure the UI updates
      queryClient.invalidateQueries({ queryKey: ['attachments', entityType, entityId] });
      queryClient.invalidateQueries({ queryKey: ['primaryImage', entityType, entityId] });
      // Force refetch
      queryClient.refetchQueries({ queryKey: ['primaryImage', entityType, entityId] });
    },
    onError: (error) => {
      console.error('Failed to set primary image:', error);
    },
  });

  const attachments = data?.data || [];
  const images = attachments.filter((a) => a.attachmentType === 'Image');
  const documents = attachments.filter((a) => a.attachmentType === 'Document');

  const handleUploadComplete = () => {
    queryClient.invalidateQueries({ queryKey: ['attachments', entityType, entityId] });
    setUploadDialogOpen(false);
  };

  const handleDeleteClick = (id: number) => {
    setDeleteId(id);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = () => {
    if (deleteId) {
      deleteMutation.mutate(deleteId);
    }
  };

  const handleSetPrimary = (id: number) => {
    setPrimaryMutation.mutate(id);
  };

  return (
    <Card>
      <CardContent sx={{ pb: 1 }}>
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            cursor: 'pointer',
          }}
          onClick={() => setExpanded(!expanded)}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="h6">{title}</Typography>
            <Typography variant="body2" color="text.secondary">
              ({attachments.length})
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {canEdit && (
              <Button
                size="small"
                startIcon={<AddIcon />}
                onClick={(e) => {
                  e.stopPropagation();
                  setUploadDialogOpen(true);
                }}
              >
                Add
              </Button>
            )}
            {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </Box>
        </Box>
      </CardContent>

      <Collapse in={expanded}>
        <Divider />
        <CardContent>
          {isLoading && (
            <Typography color="text.secondary">Loading attachments...</Typography>
          )}

          {error && (
            <Alert severity="error">Failed to load attachments</Alert>
          )}

          {!isLoading && !error && attachments.length === 0 && (
            <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
              No attachments yet.
              {canEdit && ' Click "Add" to upload files.'}
            </Typography>
          )}

          {images.length > 0 && (
            <Box sx={{ mb: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <ImageIcon fontSize="small" color="primary" />
                <Typography variant="subtitle2">
                  Images ({images.length})
                </Typography>
              </Box>
              <ImageGallery
                images={images}
                onDelete={handleDeleteClick}
                onSetPrimary={handleSetPrimary}
                canDelete={canEdit}
                canSetPrimary={canEdit}
              />
            </Box>
          )}

          {documents.length > 0 && (
            <Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <DocIcon fontSize="small" color="action" />
                <Typography variant="subtitle2">
                  Documents ({documents.length})
                </Typography>
              </Box>
              <DocumentList
                documents={documents}
                onDelete={handleDeleteClick}
                canDelete={canEdit}
              />
            </Box>
          )}
        </CardContent>
      </Collapse>

      {/* Upload Dialog */}
      <Dialog
        open={uploadDialogOpen}
        onClose={() => setUploadDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Upload Attachments</DialogTitle>
        <DialogContent>
          <FileUploader
            entityType={entityType}
            entityId={entityId}
            onUploadComplete={handleUploadComplete}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setUploadDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={deleteDialogOpen}
        title="Delete Attachment"
        message="Are you sure you want to delete this attachment? This action cannot be undone."
        confirmText="Delete"
        confirmColor="error"
        onConfirm={handleDeleteConfirm}
        onCancel={() => {
          setDeleteDialogOpen(false);
          setDeleteId(null);
        }}
        isLoading={deleteMutation.isPending}
      />
    </Card>
  );
};
