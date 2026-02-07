import React, { useState } from 'react';
import {
  Box,
  Card,
  CardMedia,
  CardContent,
  Typography,
  Skeleton,
  Dialog,
  DialogContent,
  IconButton,
} from '@mui/material';
import {
  Close as CloseIcon,
  BrokenImage as NoImageIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { attachmentService } from '../../services/attachmentService';

interface PrimaryImageProps {
  entityType: string;
  entityId: number;
  height?: number;
  showTitle?: boolean;
}

export const PrimaryImage: React.FC<PrimaryImageProps> = ({
  entityType,
  entityId,
  height = 200,
  showTitle = false,
}) => {
  const [lightboxOpen, setLightboxOpen] = useState(false);

  const apiBaseUrl = process.env.REACT_APP_API_URL?.replace('/api/v1', '') || 'http://fragbox:5000';

  const { data, isLoading } = useQuery({
    queryKey: ['primaryImage', entityType, entityId],
    queryFn: () => attachmentService.getPrimaryImage(entityType, entityId),
    enabled: !!entityType && entityId > 0,
    staleTime: 0, // Always refetch when invalidated
  });

  const primaryImage = data?.data;

  if (isLoading) {
    return (
      <Card>
        <Skeleton variant="rectangular" height={height} />
      </Card>
    );
  }

  if (!primaryImage) {
    return (
      <Card sx={{ height }}>
        <Box
          sx={{
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: 'grey.100',
            color: 'grey.400',
          }}
        >
          <NoImageIcon sx={{ fontSize: 48, mb: 1 }} />
          <Typography variant="body2" color="text.secondary">
            No image
          </Typography>
        </Box>
      </Card>
    );
  }

  const imageUrl = `${apiBaseUrl}${primaryImage.url}`;

  return (
    <>
      <Card
        sx={{
          cursor: 'pointer',
          '&:hover': {
            boxShadow: 4,
          },
        }}
        onClick={() => setLightboxOpen(true)}
      >
        <CardMedia
          component="img"
          height={height}
          image={imageUrl}
          alt={primaryImage.title}
          sx={{ objectFit: 'cover' }}
        />
        {showTitle && (
          <CardContent sx={{ py: 1 }}>
            <Typography variant="caption" color="text.secondary" noWrap>
              {primaryImage.title}
            </Typography>
          </CardContent>
        )}
      </Card>

      {/* Lightbox Dialog */}
      <Dialog
        open={lightboxOpen}
        onClose={() => setLightboxOpen(false)}
        maxWidth="lg"
        PaperProps={{
          sx: {
            bgcolor: 'rgba(0, 0, 0, 0.9)',
            backgroundImage: 'none',
          },
        }}
      >
        <DialogContent
          sx={{
            p: 0,
            position: 'relative',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <IconButton
            onClick={() => setLightboxOpen(false)}
            sx={{
              position: 'absolute',
              top: 8,
              right: 8,
              color: 'white',
              bgcolor: 'rgba(0, 0, 0, 0.5)',
              '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.7)' },
            }}
          >
            <CloseIcon />
          </IconButton>
          <img
            src={imageUrl}
            alt={primaryImage.title}
            style={{
              maxWidth: '100%',
              maxHeight: '80vh',
              objectFit: 'contain',
            }}
          />
        </DialogContent>
      </Dialog>
    </>
  );
};
