import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { store } from '../store';
import { logout, setTokens } from '../store/authSlice';

// Dynamically determine API URL based on how the user is accessing the app
// This allows the app to work via hostname, IP address, or localhost
const getApiBaseUrl = (): string => {
  // If explicitly set in environment, use that
  if (process.env.REACT_APP_API_URL) {
    return process.env.REACT_APP_API_URL;
  }

  const { protocol, hostname, port } = window.location;

  // If accessing via standard HTTPS port (443) or no port specified,
  // assume we're behind a reverse proxy - use same origin
  if (protocol === 'https:' && (!port || port === '443')) {
    return `${protocol}//${hostname}/api/v1`;
  }

  // Direct access - use appropriate port
  // Use port 5001 for HTTPS, port 5000 for HTTP
  const apiPort = protocol === 'https:' ? 5001 : 5000;
  return `${protocol}//${hostname}:${apiPort}/api/v1`;
};

const API_BASE_URL = getApiBaseUrl();


const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const state = store.getState();
    const token = state.auth.accessToken;

    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor to handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const state = store.getState();
      const refreshToken = state.auth.refreshToken;

      if (refreshToken) {
        try {
          const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
            refreshToken,
          });

          const { accessToken, refreshToken: newRefreshToken } = response.data.data;

          store.dispatch(setTokens({ accessToken, refreshToken: newRefreshToken }));

          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          }

          return api(originalRequest);
        } catch (refreshError) {
          store.dispatch(logout());
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }
    }

    return Promise.reject(error);
  }
);

export default api;
