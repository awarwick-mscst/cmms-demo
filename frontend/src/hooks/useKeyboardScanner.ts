import { useEffect, useRef, useCallback } from 'react';

interface UseKeyboardScannerOptions {
  onScan: (barcode: string) => void;
  minLength?: number;
  maxDelayMs?: number;
  enabled?: boolean;
}

export const useKeyboardScanner = ({
  onScan,
  minLength = 4,
  maxDelayMs = 50,
  enabled = true,
}: UseKeyboardScannerOptions) => {
  const bufferRef = useRef<string>('');
  const lastKeyTimeRef = useRef<number>(0);

  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      if (!enabled) return;

      // Skip if user is typing in an input or textarea
      const target = event.target as HTMLElement;
      if (
        target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.isContentEditable
      ) {
        return;
      }

      const now = Date.now();
      const timeSinceLastKey = now - lastKeyTimeRef.current;

      // If too much time has passed, reset the buffer
      if (timeSinceLastKey > maxDelayMs) {
        bufferRef.current = '';
      }

      lastKeyTimeRef.current = now;

      // Handle Enter key - submit the barcode
      if (event.key === 'Enter') {
        if (bufferRef.current.length >= minLength) {
          onScan(bufferRef.current);
        }
        bufferRef.current = '';
        return;
      }

      // Only accept printable characters
      if (event.key.length === 1 && !event.ctrlKey && !event.altKey && !event.metaKey) {
        bufferRef.current += event.key;
      }
    },
    [enabled, maxDelayMs, minLength, onScan]
  );

  useEffect(() => {
    if (enabled) {
      window.addEventListener('keydown', handleKeyDown);
      return () => window.removeEventListener('keydown', handleKeyDown);
    }
  }, [enabled, handleKeyDown]);

  const reset = useCallback(() => {
    bufferRef.current = '';
    lastKeyTimeRef.current = 0;
  }, []);

  return { reset };
};
