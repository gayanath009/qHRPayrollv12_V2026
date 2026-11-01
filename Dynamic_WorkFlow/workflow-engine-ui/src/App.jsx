import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import WorkflowDesigner from './components/WorkflowDesigner';
import FormBuilder from './components/FormBuilder';
import TaskInbox from './components/TaskInbox';

import { getStoredToken, getStoredUser, clearAuthStorage } from './services/storage';

const ProtectedRoute = ({ children }) => {
  const token = getStoredToken();
  const user = getStoredUser();
  if (!token || !user?.username) {
    clearAuthStorage();
    return <Navigate to="/login" replace />;
  }
  return children;
};

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="inbox" element={<TaskInbox />} />
          <Route path="designer/:versionId" element={<WorkflowDesigner />} />
          <Route path="form-builder/:versionId" element={<FormBuilder />} />
        </Route>

        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
