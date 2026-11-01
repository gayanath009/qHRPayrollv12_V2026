/**
 * Safe localStorage utilities to prevent JSON.parse crashes from corrupted or non-JSON data
 */

export function getStoredUser() {
  try {
    const raw = localStorage.getItem('user');
    if (!raw || raw === 'undefined' || raw === 'null' || typeof raw !== 'string') {
      return {};
    }
    const trimmed = raw.trim();
    if (!trimmed.startsWith('{') && !trimmed.startsWith('[')) {
      // If it was stored as a raw username string, e.g. "admin"
      return { username: trimmed };
    }
    const parsed = JSON.parse(trimmed);
    return parsed && typeof parsed === 'object' ? parsed : {};
  } catch (err) {
    console.warn('Corrupted user session in localStorage, clearing:', err);
    try {
      localStorage.removeItem('user');
    } catch (_) {}
    return {};
  }
}

export function setStoredUser(user) {
  try {
    if (!user) {
      localStorage.removeItem('user');
    } else {
      localStorage.setItem('user', JSON.stringify(user));
    }
  } catch (err) {
    console.error('Failed to save user to localStorage:', err);
  }
}

export function getStoredToken() {
  try {
    const token = localStorage.getItem('token');
    if (!token || token === 'undefined' || token === 'null') {
      return null;
    }
    return token;
  } catch (_) {
    return null;
  }
}

export function clearAuthStorage() {
  try {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  } catch (_) {}
}
