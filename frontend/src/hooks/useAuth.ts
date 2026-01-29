import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import { loginSuccess, logout as logoutAction, setLoading } from '../store/authSlice';
import { RootState } from '../store';
import { LoginRequest } from '../types';

export const useAuth = () => {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { user, isAuthenticated, isLoading } = useSelector((state: RootState) => state.auth);
  const refreshToken = useSelector((state: RootState) => state.auth.refreshToken);

  const login = useCallback(async (credentials: LoginRequest) => {
    dispatch(setLoading(true));
    try {
      const response = await authService.login(credentials);

      if (response.success && response.data) {
        dispatch(loginSuccess({
          user: response.data.user,
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
        }));
        navigate('/');
        return { success: true };
      }

      return { success: false, error: response.errors[0] || 'Login failed' };
    } catch (error: any) {
      const errorMessage = error.response?.data?.errors?.[0] || 'Login failed';
      return { success: false, error: errorMessage };
    } finally {
      dispatch(setLoading(false));
    }
  }, [dispatch, navigate]);

  const logout = useCallback(async () => {
    try {
      if (refreshToken) {
        await authService.logout(refreshToken);
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      dispatch(logoutAction());
      navigate('/login');
    }
  }, [dispatch, navigate, refreshToken]);

  const hasPermission = useCallback((permission: string) => {
    if (!user) return false;
    return user.permissions.includes(permission) || user.roles.includes('Administrator');
  }, [user]);

  const hasRole = useCallback((role: string) => {
    if (!user) return false;
    return user.roles.includes(role);
  }, [user]);

  return {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    hasPermission,
    hasRole,
  };
};
