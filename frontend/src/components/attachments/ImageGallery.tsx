import React, { useState } from 'react';
import {
  Box,
  Chip,
  Dialog,
  DialogContent,
  IconButton,
  Typography,
  ImageList,
  ImageListItem,
  ImageListItemBar,
  Tooltip,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import {
  Close as CloseIcon,
  Delete as DeleteIcon,
  ChevronLeft as PrevIcon,
  ChevronRight as NextIcon,
  Download as DownloadIcon,
  ZoomIn as ZoomInIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon,
} from '@mui/icons-material';
import { Attachment } from '../../types';
import { attachmentService } from '../../services/attachmentService';

interface ImageGalleryProps {
  images: Attachment[];
  onDelete?: (id: number) => void;
  onSetPrimary?: (id: number) => void;
  canDelete?: boolean;
  canSetPrimary?: boolean;
}

export const ImageGallery: React.FC<ImageGalleryProps> = ({
  images,
  onDelete,
  onSetPrimary,
  canDelete = false,
  canSetPrimary = false,
}) => {
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [currentIndex, setCurrentIndex] = useState(0);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const apiBaseUrl = process.env.REACT_APP_API_URL?.replace('/api/v1', '') || 'http://fragbox:5000';

  const getImageUrl = (attachment: Attachment) => {
    return `${apiBaseUrl}${attachment.url}`;
  };

  const handleImageClick = (index: number) => {
    setCurrentIndex(index);
    setLightboxOpen(true);
  };

  const handlePrev = () => {
    setCurrentIndex((prev) => (prev > 0 ? prev - 1 : images.length - 1));
  };

  const handleNext = () => {
    setCurrentIndex((prev) => (prev < images.length - 1 ? prev + 1 : 0));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowLeft') handlePrev();
    if (e.key === 'ArrowRight') handleNext();
    if (e.key === 'Escape') setLightboxOpen(false);
  };

  const handleDownload = (attachment: Attachment) => {
    const link = document.createElement('a');
    link.href = attachmentService.getDownloadUrl(attachment.id);
    link.download = attachment.fileName;
    link.click();
  };

  if (images.length === 0) {
    return null;
  }

  const currentImage = images[currentIndex];

  return (
    <Box>
      <ImageList
        cols={isMobile ? 2 : 4}
        gap={8}
        sx={{ width: '100%', maxHeight: 400, overflow: 'auto' }}
      >
        {images.map((image, index) => (
          <ImageListItem
            key={image.id}
            sx={{
              cursor: 'pointer',
              position: 'relative',
              '&:hover': {
                opacity: 0.8,
              },
            }}
          >
            {image.isPrimary && (
              <Chip
                icon={<StarIcon />}
                label="Primary"
                size="small"
                color="primary"
                sx={{
                  position: 'absolute',
                  top: 4,
                  left: 4,
                  zIndex: 1,
                  fontSize: '0.7rem',
                  height: 22,
                }}
              />
            )}
            <img
              src={getImageUrl(image)}
              alt={image.title}
              loading="lazy"
              style={{
                height: 150,
                objectFit: 'cover',
                borderRadius: 4,
                border: image.isPrimary ? `2px solid ${theme.palette.primary.main}` : 'none',
              }}
              onClick={() => handleImageClick(index)}
            />
            <ImageListItemBar
              title={image.title}
              subtitle={attachmentService.formatFileSize(image.fileSize)}
              actionIcon={
                <Box sx={{ display: 'flex' }}>
                  {canSetPrimary && onSetPrimary && !image.isPrimary && (
                    <Tooltip title="Set as Primary">
                      <IconButton
                        sx={{ color: 'rgba(255, 255, 255, 0.8)' }}
                        onClick={(e) => {
                          e.stopPropagation();
                          onSetPrimary(image.id);
                        }}
                        size="small"
                      >
                        <StarBorderIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                  <Tooltip title="View">
                    <IconButton
                      sx={{ color: 'rgba(255, 255, 255, 0.8)' }}
                      onClick={() => handleImageClick(index)}
                      size="small"
                    >
                      <ZoomInIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  {canDelete && onDelete && (
                    <Tooltip title="Delete">
                      <IconButton
                        sx={{ color: 'rgba(255, 255, 255, 0.8)' }}
                        onClick={(e) => {
                          e.stopPropagation();
                          onDelete(image.id);
                        }}
                        size="small"
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </Box>
              }
            />
          </ImageListItem>
        ))}
      </ImageList>

      {/* Lightbox Dialog */}
      <Dialog
        open={lightboxOpen}
        onClose={() => setLightboxOpen(false)}
        maxWidth="xl"
        fullWidth
        onKeyDown={handleKeyDown}
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
            minHeight: '60vh',
          }}
        >
          {/* Close button */}
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

          {/* Navigation arrows */}
          {images.length > 1 && (
            <>
              <IconButton
                onClick={handlePrev}
                sx={{
                  position: 'absolute',
                  left: 8,
                  color: 'white',
                  bgcolor: 'rgba(0, 0, 0, 0.5)',
                  '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.7)' },
                }}
              >
                <PrevIcon />
              </IconButton>
              <IconButton
                onClick={handleNext}
                sx={{
                  position: 'absolute',
                  right: 8,
                  color: 'white',
                  bgcolor: 'rgba(0, 0, 0, 0.5)',
                  '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.7)' },
                }}
              >
                <NextIcon />
              </IconButton>
            </>
          )}

          {/* Main image */}
          {currentImage && (
            <Box sx={{ textAlign: 'center' }}>
              <img
                src={getImageUrl(currentImage)}
                alt={currentImage.title}
                style={{
                  maxWidth: '100%',
                  maxHeight: '70vh',
                  objectFit: 'contain',
                }}
              />
              <Box sx={{ mt: 2, color: 'white' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1 }}>
                  <Typography variant="h6">{currentImage.title}</Typography>
                  {currentImage.isPrimary && (
                    <Chip icon={<StarIcon />} label="Primary" size="small" color="primary" />
                  )}
                </Box>
                <Typography variant="body2" color="grey.400">
                  {currentImage.fileName} ({attachmentService.formatFileSize(currentImage.fileSize)})
                </Typography>
                <Box sx={{ mt: 1, display: 'flex', justifyContent: 'center', gap: 1 }}>
                  {canSetPrimary && onSetPrimary && !currentImage.isPrimary && (
                    <Tooltip title="Set as Primary">
                      <IconButton
                        onClick={() => {
                          onSetPrimary(currentImage.id);
                          setLightboxOpen(false);
                        }}
                        sx={{ color: 'white' }}
                      >
                        <StarBorderIcon />
                      </IconButton>
                    </Tooltip>
                  )}
                  <Tooltip title="Download">
                    <IconButton
                      onClick={() => handleDownload(currentImage)}
                      sx={{ color: 'white' }}
                    >
                      <DownloadIcon />
                    </IconButton>
                  </Tooltip>
                </Box>
                {images.length > 1 && (
                  <Typography variant="caption" color="grey.500">
                    {currentIndex + 1} / {images.length}
                  </Typography>
                )}
              </Box>
            </Box>
          )}
        </DialogContent>
      </Dialog>
    </Box>
  );
};
