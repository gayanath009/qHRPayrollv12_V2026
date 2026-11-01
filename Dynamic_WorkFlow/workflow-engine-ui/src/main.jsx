import React from 'react';
import ReactDOM from 'react-dom/client';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import App from './App';
import './index.css';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#540033', // Brand Burgundy
      dark: '#3d0025', // Deep Burgundy Hover
      light: '#781050', // Light Burgundy
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#700044', // Complementary Burgundy Shade
      dark: '#4a002e',
      light: '#9e1b68',
      contrastText: '#ffffff',
    },
    background: {
      default: '#f8fafc',
      paper: '#ffffff',
    },
    text: {
      primary: '#0f172a',
      secondary: '#64748b',
    },
    divider: '#e2e8f0',
    success: {
      main: '#10b981',
      light: '#d1fae5',
    },
    error: {
      main: '#ef4444',
      light: '#fee2e2',
    },
    warning: {
      main: '#f59e0b',
      light: '#fef3c7',
    },
    info: {
      main: '#540033',
      light: '#fdf2f8',
    },
  },
  typography: {
    fontFamily: ['poppins', 'Arial'],
    h1: { fontWeight: 700, color: '#0f172a' },
    h2: { fontWeight: 700, color: '#0f172a' },
    h3: { fontWeight: 600, color: '#0f172a' },
    h4: { fontWeight: 600, color: '#0f172a' },
    h5: { fontWeight: 600, color: '#0f172a' },
    h6: { fontWeight: 600, color: '#0f172a' },
    button: { textTransform: 'none', fontWeight: 600 },
  },
  shape: {
    borderRadius: 10,
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          padding: '8px 16px',
        },
        containedPrimary: {
          backgroundColor: '#540033',
          color: '#ffffff',
          '&:hover': {
            backgroundColor: '#3d0025',
          },
        },
        outlinedPrimary: {
          color: '#540033',
          borderColor: 'rgba(84, 0, 51, 0.5)',
          '&:hover': {
            borderColor: '#540033',
            backgroundColor: 'rgba(84, 0, 51, 0.04)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          boxShadow: '0 1px 3px 0 rgba(0, 0, 0, 0.05), 0 1px 2px -1px rgba(0, 0, 0, 0.05)',
          border: '1px solid #e2e8f0',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
        },
      },
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  </React.StrictMode>
);
