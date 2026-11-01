import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, TextField, Button, Typography, Alert, Container, CircularProgress } from '@mui/material';
import portalLogo from '../assets/portal.png';
import api from '../services/api';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await api.post('/auth/login', { username, password });
      localStorage.setItem('token', res.data.token);
      localStorage.setItem('refreshToken', res.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(res.data.user));
      navigate('/dashboard');
    } catch (err) {
      console.error('Login error details:', err);
      const serverMsg = err.response?.data?.message || err.response?.data?.title;
      if (serverMsg) {
        setError(serverMsg);
      } else if (err.message) {
        setError(`${err.message} — Connecting to: ${api.defaults.baseURL}/auth/login`);
      } else {
        setError('Login failed. Please check username and password.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)',
        px: 2,
      }}
    >
      <Container maxWidth="xs">
        <Box
          className="glass-panel animate-fade-in"
          sx={{
            p: 4,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            textAlign: 'center',
          }}
        >
          <Box
            component="img"
            src={portalLogo}
            alt="Workflow Portal Logo"
            sx={{
              maxHeight: 120,
              maxWidth: 350,
              objectFit: 'contain',
              mb: 2,
            }}
          />

          {/* <Typography variant="h5" component="h1" sx={{ fontWeight: 700, mb: 1, color: '#540033' }}>
            QCS Workflow Portal
          </Typography> */}
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 3 }}>
            Sign in to manage dynamic workflow processes
          </Typography>

          {error && (
            <Alert severity="error" sx={{ width: '100%', mb: 3, textAlign: 'left', borderRadius: 2 }}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleLogin} sx={{ width: '100%' }}>
            <TextField
              margin="normal"
              required
              fullWidth
              id="username"
              label="Username"
              name="username"
              autoComplete="username"
              autoFocus
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <TextField
              margin="normal"
              required
              fullWidth
              name="password"
              label="Password"
              type="password"
              id="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              disabled={loading}
              sx={{
                mt: 3,
                mb: 2,
                py: 1.5,
                background: 'linear-gradient(90deg, #540033 0%, #700044 100%)',
                boxShadow: '0 4px 14px 0 rgba(84, 0, 51, 0.3)',
                '&:hover': {
                  background: 'linear-gradient(90deg, #3d0025 0%, #540033 100%)',
                },
              }}
            >
              {loading ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
            </Button>
          </Box>
        </Box>
      </Container>
    </Box>
  );
}
