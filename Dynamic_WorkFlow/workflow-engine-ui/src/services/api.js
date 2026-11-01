import axios from 'axios';

const API_BASE_URL = 
  (typeof window !== 'undefined' && window.__APP_CONFIG__ && window.__APP_CONFIG__.VITE_API_URL && window.__APP_CONFIG__.VITE_API_URL.trim() !== '')
    ? window.__APP_CONFIG__.VITE_API_URL.trim()
    : (import.meta.env.VITE_API_URL || 'http://localhost:5001/api');

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor to inject JWT token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor to handle expired tokens
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const token = localStorage.getItem('token');
        const refreshToken = localStorage.getItem('refreshToken');
        if (token && refreshToken) {
          const res = await axios.post(`${API_BASE_URL}/auth/refresh`, { token, refreshToken });
          if (res.status === 200) {
            localStorage.setItem('token', res.data.token);
            localStorage.setItem('refreshToken', res.data.refreshToken);
            originalRequest.headers.Authorization = `Bearer ${res.data.token}`;
            return api(originalRequest);
          }
        }
      } catch (refreshError) {
        // Refresh token expired or invalid -> log out
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
