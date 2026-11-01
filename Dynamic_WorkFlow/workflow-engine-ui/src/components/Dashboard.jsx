import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  List,
  ListItem,
  ListItemText,
  IconButton,
  Tooltip,
  CircularProgress,
  Autocomplete,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import VisibilityIcon from '@mui/icons-material/Visibility';
import AddIcon from '@mui/icons-material/Add';
import SettingsIcon from '@mui/icons-material/Settings';
import DynamicFormIcon from '@mui/icons-material/DynamicForm';
import RefreshIcon from '@mui/icons-material/Refresh';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import api from '../services/api';
import { getStoredUser } from '../services/storage';

export default function Dashboard() {
  const [metrics, setMetrics] = useState(null);
  const [workflows, setWorkflows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [openNewWf, setOpenNewWf] = useState(false);
  const [openEditWf, setOpenEditWf] = useState(false);
  const [editingWf, setEditingWf] = useState(null);
  const [openVersions, setOpenVersions] = useState(false);
  const [selectedWf, setSelectedWf] = useState(null);
  const [versions, setVersions] = useState([]);
  const [availableLeaveTypes, setAvailableLeaveTypes] = useState([
    { id: 1, code: 'LV_ANL', name: 'Annual Leave' },
    { id: 3, code: 'LV_SICK', name: 'Sick Leave' },
    { id: 4, code: 'LV_CSL', name: 'Casual Leave' },
    { id: 12, code: 'LV_MTN', name: 'Maternity Leave' },
    { id: 13, code: 'LV_HAJ', name: 'HAJJI Leave' },
    { id: 7, code: 'LV_EMG_PD', name: 'Emergency-Paid' },
    { id: 14, code: 'LV_EMG_UPD', name: 'Emergency-Unpaid' },
  ]);

  // Current user & admin check
  const currentUser = getStoredUser();
  const isAdmin = currentUser?.username?.toLowerCase() === 'admin' || currentUser?.roleName?.toLowerCase()?.includes('admin');

  // Form values (Create)
  const [wfName, setWfName] = useState('');
  const [wfCode, setWfCode] = useState('');
  const [wfDesc, setWfDesc] = useState('');
  const [wfModule, setWfModule] = useState('Leave');
  const [wfCat, setWfCat] = useState('HR');
  const [wfAssociatedLeaveTypes, setWfAssociatedLeaveTypes] = useState([]);
  const [wfTargetWorkType, setWfTargetWorkType] = useState('ALL');

  // Form values (Edit)
  const [editWfName, setEditWfName] = useState('');
  const [editWfDesc, setEditWfDesc] = useState('');
  const [editWfModule, setEditWfModule] = useState('');
  const [editWfCat, setEditWfCat] = useState('');
  const [editWfAssociatedLeaveTypes, setEditWfAssociatedLeaveTypes] = useState([]);
  const [editWfTargetWorkType, setEditWfTargetWorkType] = useState('ALL');

  const navigate = useNavigate();

  const fetchData = async () => {
    try {
      setLoading(true);
      const mRes = await api.get('/dashboard/metrics');
      setMetrics(mRes.data);

      const wRes = await api.get('/workflow');
      setWorkflows(wRes.data);

      try {
        const metaRes = await api.get('/workflow/metadata/leave-and-work-types');
        if (metaRes.data?.leaveTypes && metaRes.data.leaveTypes.length > 0) {
          setAvailableLeaveTypes(metaRes.data.leaveTypes);
        }
      } catch (metaErr) {
        console.warn('Metadata endpoint not reachable, using default leave types', metaErr);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteWorkflow = async (wf) => {
    if (!isAdmin) {
      alert('Only administrators are authorized to delete workflows.');
      return;
    }
    if (confirm(`Are you sure you want to delete workflow "${wf.name}" (${wf.code})?\n\nThis will remove the workflow definition and its associated draft/published versions.`)) {
      try {
        setLoading(true);
        await api.delete(`/workflow/${wf.id}`);
        alert(`Workflow "${wf.name}" deleted successfully.`);
        await fetchData();
      } catch (err) {
        console.error(err);
        alert(err.response?.data?.message || 'Failed to delete workflow.');
      } finally {
        setLoading(false);
      }
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSeed = async () => {
    try {
      setLoading(true);
      await api.post('/admin/seed');
      await fetchData();
    } catch (err) {
      console.error(err);
    }
  };

  const handleSyncEmployees = async () => {
    try {
      setLoading(true);
      const res = await api.post('/admin/sync-qhrm');
      alert(res.data.message || 'Employees synchronized from qHRM successfully!');
      await fetchData();
    } catch (err) {
      console.error(err);
      alert('Sync completed or in progress.');
      await fetchData();
    } finally {
      setLoading(false);
    }
  };

  const handleCreateWorkflow = async () => {
    try {
      await api.post('/workflow', {
        name: wfName,
        code: wfCode,
        description: wfDesc,
        module: wfModule,
        category: wfCat,
        associatedLeaveTypes: wfAssociatedLeaveTypes.join(','),
        targetWorkType: wfTargetWorkType,
      });
      setOpenNewWf(false);
      setWfName('');
      setWfCode('');
      setWfDesc('');
      setWfModule('Leave');
      setWfCat('HR');
      setWfAssociatedLeaveTypes([]);
      setWfTargetWorkType('ALL');
      await fetchData();
    } catch (err) {
      alert(err.response?.data?.message || err.response?.data || 'Failed to create workflow');
    }
  };

  const handleOpenEdit = (wf) => {
    setEditingWf(wf);
    setEditWfName(wf.name || '');
    setEditWfDesc(wf.description || '');
    setEditWfModule(wf.module || 'Leave');
    setEditWfCat(wf.category || 'HR');
    const existingTypes = wf.associatedLeaveTypes
      ? wf.associatedLeaveTypes.split(',').map(s => s.trim()).filter(Boolean)
      : [];
    setEditWfAssociatedLeaveTypes(existingTypes);
    setEditWfTargetWorkType(wf.targetWorkType || 'ALL');
    setOpenEditWf(true);
  };

  const handleUpdateWorkflow = async () => {
    if (!editingWf) return;
    try {
      await api.put(`/workflow/${editingWf.id}`, {
        name: editWfName,
        description: editWfDesc,
        module: editWfModule,
        category: editWfCat,
        isActive: editingWf.isActive,
        associatedLeaveTypes: editWfAssociatedLeaveTypes.join(','),
        targetWorkType: editWfTargetWorkType,
      });
      setOpenEditWf(false);
      setEditingWf(null);
      await fetchData();
    } catch (err) {
      alert(err.response?.data?.message || err.response?.data || 'Failed to update workflow');
    }
  };

  const handleViewVersions = async (wf) => {
    setSelectedWf(wf);
    try {
      const res = await api.get(`/workflow/${wf.id}/versions`);
      setVersions(res.data);
      setOpenVersions(true);
    } catch (err) {
      console.error(err);
    }
  };

  const handlePublish = async (vId) => {
    if (confirm('Are you sure you want to publish this version? This will deactivate the current active version.')) {
      try {
        await api.post(`/workflow/version/${vId}/publish`);
        handleViewVersions(selectedWf);
      } catch (err) {
        alert('Failed to publish');
      }
    }
  };

  const handleClone = async (vId) => {
    try {
      const res = await api.post(`/workflow/version/${vId}/clone`);
      alert(res.data.message);
      handleViewVersions(selectedWf);
    } catch (err) {
      alert('Failed to clone version');
    }
  };

  if (loading && !metrics) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box className="animate-fade-in">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Workflow Analytics
        </Typography>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button variant="outlined" startIcon={<RefreshIcon />} onClick={fetchData}>
            Reload
          </Button>
          <Button variant="outlined" color="info" startIcon={<CloudUploadIcon />} onClick={handleSyncEmployees}>
            Sync qHRM Users
          </Button>
          {workflows.length === 0 && (
            <Button variant="contained" color="secondary" startIcon={<CloudUploadIcon />} onClick={handleSeed}>
              Seed Sample Data
            </Button>
          )}
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpenNewWf(true)}>
            New Workflow
          </Button>
        </Box>
      </Box>

      {/* Metrics Cards - Distributed equally across the full screen width */}
      {metrics && (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, 1fr)',
              md: 'repeat(5, 1fr)',
            },
            gap: 2.5,
            mb: 4,
            width: '100%',
          }}
        >
          {[
            { title: 'Total Volume', value: metrics.totalRequests, color: '#4f46e5' },
            { title: 'InProgress / Pending', value: metrics.pendingRequests, color: '#f59e0b' },
            { title: 'Approved', value: metrics.approvedRequests, color: '#10b981' },
            { title: 'Rejected', value: metrics.rejectedRequests, color: '#ef4444' },
            { title: 'Average Turnaround', value: `${metrics.averageApprovalTimeHours} Hours`, color: '#0284c7' },
          ].map((item, idx) => (
            <Card
              key={idx}
              className="glass-panel"
              sx={{
                borderLeft: `4px solid ${item.color} !important`,
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
                height: '100%',
                minWidth: 0,
                transition: 'transform 0.2s ease, box-shadow 0.2s ease',
                '&:hover': {
                  transform: 'translateY(-2px)',
                  boxShadow: '0 8px 20px rgba(0,0,0,0.06)',
                },
              }}
            >
              <CardContent sx={{ p: 2.5, '&:last-child': { pb: 2.5 } }}>
                <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600, letterSpacing: 0.3, textTransform: 'uppercase', fontSize: '0.72rem' }}>
                  {item.title}
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 700, mt: 1, color: 'text.primary', fontSize: { xs: '1.5rem', lg: '1.75rem' } }}>
                  {item.value}
                </Typography>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      {/* Graphical Dashboard - Distributed into 3 equal columns */}
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: {
            xs: '1fr',
            md: 'repeat(3, 1fr)',
          },
          gap: 3,
          mb: 4,
          width: '100%',
          alignItems: 'stretch',
        }}
      >
        {/* Meaningful Graph 1: Approval Status & Resolution Ratio (Donut Visual & Stats) */}
        <Card className="glass-panel" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <CardContent sx={{ p: 3, display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Approval Status Ratio
              </Typography>
              <Chip
                label={`${Math.round(((metrics?.approvedRequests || 0) / Math.max(1, (metrics?.totalRequests || 1))) * 100)}% Success`}
                color="success"
                size="small"
                sx={{ fontWeight: 600 }}
              />
            </Box>

            {/* Donut Chart Visualization */}
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', my: 2 }}>
              <Box sx={{ position: 'relative', width: 150, height: 150 }}>
                {(() => {
                  const total = Math.max(1, metrics?.totalRequests || 0);
                  const appPct = ((metrics?.approvedRequests || 0) / total) * 100;
                  const penPct = ((metrics?.pendingRequests || 0) / total) * 100;
                  const rejPct = ((metrics?.rejectedRequests || 0) / total) * 100;
                  const circ = 2 * Math.PI * 45; // ~282.74

                  const appDash = (appPct / 100) * circ;
                  const penDash = (penPct / 100) * circ;
                  const rejDash = (rejPct / 100) * circ;

                  const appOffset = 0;
                  const penOffset = -appDash;
                  const rejOffset = -(appDash + penDash);

                  return (
                    <svg width="150" height="150" viewBox="0 0 120 120" style={{ transform: 'rotate(-90deg)' }}>
                      {/* Background track */}
                      <circle cx="60" cy="60" r="45" stroke="rgba(0,0,0,0.06)" strokeWidth="14" fill="transparent" />

                      {/* Approved segment */}
                      {appDash > 0 && (
                        <circle
                          cx="60" cy="60" r="45"
                          stroke="#10b981"
                          strokeWidth="14"
                          fill="transparent"
                          strokeDasharray={`${appDash} ${circ}`}
                          strokeDashoffset={appOffset}
                          strokeLinecap="round"
                        />
                      )}
                      {/* Pending segment */}
                      {penDash > 0 && (
                        <circle
                          cx="60" cy="60" r="45"
                          stroke="#f59e0b"
                          strokeWidth="14"
                          fill="transparent"
                          strokeDasharray={`${penDash} ${circ}`}
                          strokeDashoffset={penOffset}
                          strokeLinecap="round"
                        />
                      )}
                      {/* Rejected segment */}
                      {rejDash > 0 && (
                        <circle
                          cx="60" cy="60" r="45"
                          stroke="#ef4444"
                          strokeWidth="14"
                          fill="transparent"
                          strokeDasharray={`${rejDash} ${circ}`}
                          strokeDashoffset={rejOffset}
                          strokeLinecap="round"
                        />
                      )}
                    </svg>
                  );
                })()}
                <Box
                  sx={{
                    position: 'absolute',
                    top: 0, left: 0, right: 0, bottom: 0,
                    display: 'flex', flexDirection: 'column',
                    justifyContent: 'center', alignItems: 'center'
                  }}
                >
                  <Typography variant="h5" sx={{ fontWeight: 700, color: 'text.primary' }}>
                    {metrics?.totalRequests || 0}
                  </Typography>
                  <Typography variant="caption" sx={{ color: 'text.secondary', fontSize: '0.7rem' }}>
                    Total Requests
                  </Typography>
                </Box>
              </Box>
            </Box>

            {/* Status Breakdown Legend */}
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, mt: 'auto', pt: 2 }}>
              {[
                { label: 'Approved', count: metrics?.approvedRequests || 0, color: '#10b981' },
                { label: 'Pending Approval', count: metrics?.pendingRequests || 0, color: '#f59e0b' },
                { label: 'Rejected', count: metrics?.rejectedRequests || 0, color: '#ef4444' },
              ].map((st, i) => {
                const pct = Math.round((st.count / Math.max(1, metrics?.totalRequests || 1)) * 100);
                return (
                  <Box key={i} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Box sx={{ width: 10, height: 10, borderRadius: '50%', backgroundColor: st.color }} />
                      <Typography variant="body2" sx={{ color: 'text.secondary' }}>{st.label}</Typography>
                    </Box>
                    <Typography variant="body2" sx={{ fontWeight: 600, color: 'text.primary' }}>
                      {st.count} <span style={{ opacity: 0.6, fontSize: '0.75rem' }}>({pct}%)</span>
                    </Typography>
                  </Box>
                );
              })}
            </Box>
          </CardContent>
        </Card>

        {/* Meaningful Graph 2: Workflow Type Breakdown & Adoption */}
        <Card className="glass-panel" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <CardContent sx={{ p: 3, display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
              Workflow Distribution
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.2, mt: 1, flexGrow: 1, justifyContent: (metrics?.workflowStats && metrics.workflowStats.length > 0) ? 'flex-start' : 'center' }}>
              {(metrics?.workflowStats && metrics.workflowStats.length > 0) ? (
                metrics.workflowStats.map((wf, idx) => {
                  const total = Math.max(1, metrics?.totalRequests || 1);
                  const pct = Math.min(100, Math.round((wf.count / total) * 100));
                  const colors = ['#4f46e5', '#0284c7', '#10b981', '#f59e0b', '#ec4899'];
                  const col = colors[idx % colors.length];

                  return (
                    <Box key={idx}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                        <Typography variant="body2" sx={{ fontWeight: 500, color: 'text.primary' }}>
                          {wf.workflowName || 'Workflow'}
                        </Typography>
                        <Typography variant="caption" sx={{ fontWeight: 600, color: col }}>
                          {wf.count} Requests ({pct}%)
                        </Typography>
                      </Box>
                      <Box sx={{ width: '100%', height: 8, backgroundColor: 'rgba(0,0,0,0.06)', borderRadius: 4, overflow: 'hidden' }}>
                        <Box
                          sx={{
                            height: '100%',
                            width: `${Math.max(5, pct)}%`,
                            backgroundColor: col,
                            borderRadius: 4,
                            transition: 'width 0.6s ease-in-out'
                          }}
                        />
                      </Box>
                    </Box>
                  );
                })
              ) : (
                <Box sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
                  <Typography variant="body2">No workflow requests recorded yet</Typography>
                </Box>
              )}
            </Box>
          </CardContent>
        </Card>

        {/* Meaningful Graph 3: Department Wise Volume */}
        <Card className="glass-panel" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <CardContent sx={{ p: 3, display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
              Department Wise Volume
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.2, mt: 1, flexGrow: 1, justifyContent: (metrics?.departmentStats && metrics.departmentStats.length > 0) ? 'flex-start' : 'center' }}>
              {(metrics?.departmentStats && metrics.departmentStats.length > 0) ? (
                metrics.departmentStats.map((dept, idx) => {
                  const total = Math.max(1, metrics?.totalRequests || 1);
                  const pct = Math.min(100, Math.round((dept.count / total) * 100));
                  const colors = ['#8b5cf6', '#0284c7', '#06b6d4', '#10b981', '#f59e0b'];
                  const col = colors[idx % colors.length];

                  return (
                    <Box key={idx}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                        <Typography variant="body2" sx={{ fontWeight: 500, color: 'text.primary' }}>
                          {dept.departmentName || 'General'}
                        </Typography>
                        <Typography variant="caption" sx={{ fontWeight: 600, color: col }}>
                          {dept.count} Requests ({pct}%)
                        </Typography>
                      </Box>
                      <Box sx={{ width: '100%', height: 8, backgroundColor: 'rgba(0,0,0,0.06)', borderRadius: 4, overflow: 'hidden' }}>
                        <Box
                          sx={{
                            height: '100%',
                            width: `${Math.max(5, pct)}%`,
                            backgroundColor: col,
                            borderRadius: 4,
                            transition: 'width 0.6s ease-in-out'
                          }}
                        />
                      </Box>
                    </Box>
                  );
                })
              ) : (
                <Box sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
                  <Typography variant="body2">No department data available</Typography>
                </Box>
              )}
            </Box>
          </CardContent>
        </Card>
      </Box>

      {/* Workflows List Table */}
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>
        Workflow Templates
      </Typography>
      <TableContainer component={Paper} className="glass-panel">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 600 }}>Code</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Workflow Name</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Work Type</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Associated Leave Types</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Module</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Category</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Status</TableCell>
              <TableCell align="right" sx={{ fontWeight: 600 }}>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                  <CircularProgress size={24} sx={{ mr: 1, verticalAlign: 'middle' }} />
                  <Typography variant="body2" component="span" sx={{ color: 'text.secondary', verticalAlign: 'middle' }}>
                    Loading workflow templates...
                  </Typography>
                </TableCell>
              </TableRow>
            ) : workflows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    No workflow templates found.
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              workflows.map((wf) => {
                const mappedCodes = wf.associatedLeaveTypes
                  ? wf.associatedLeaveTypes.split(',').map(s => s.trim()).filter(Boolean)
                  : [];

                return (
                  <TableRow key={wf.id} sx={{ '&:last-child td, &:last-child th': { border: 0 } }}>
                    <TableCell component="th" scope="row" sx={{ fontWeight: 600 }}>
                      {wf.code}
                    </TableCell>
                    <TableCell>{wf.name}</TableCell>
                    <TableCell>
                      {wf.targetWorkType === 'IN_HOUSE' ? (
                        <Chip label="🏢 In-House" color="primary" size="small" variant="filled" sx={{ fontWeight: 600 }} />
                      ) : wf.targetWorkType === 'OUTSOURCED' ? (
                        <Chip label="🌐 Outsourced" color="warning" size="small" variant="filled" sx={{ fontWeight: 600 }} />
                      ) : (
                        <Chip label="👥 All Staff" size="small" variant="outlined" />
                      )}
                    </TableCell>
                    <TableCell>
                      {mappedCodes.length > 0 ? (
                        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                          {mappedCodes.map((code) => {
                            const lvObj = availableLeaveTypes.find(l => l.code === code);
                            return (
                              <Chip
                                key={code}
                                label={lvObj ? `${lvObj.name} (${code})` : code}
                                size="small"
                                color="primary"
                                variant="outlined"
                                sx={{ fontWeight: 500 }}
                              />
                            );
                          })}
                        </Box>
                      ) : (
                        <Typography variant="caption" sx={{ color: 'text.secondary', fontStyle: 'italic' }}>
                          None (Trigger by exact code)
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell><Chip label={wf.module || 'General'} size="small" variant="outlined" /></TableCell>
                    <TableCell>{wf.category || 'N/A'}</TableCell>
                    <TableCell>
                      <Chip
                        label={wf.isActive ? 'Active' : 'Inactive'}
                        color={wf.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
                        <Tooltip title="View Versions">
                          <IconButton size="small" onClick={() => handleViewVersions(wf)}>
                            <VisibilityIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Edit Workflow & Mappings">
                          <IconButton size="small" color="primary" onClick={() => handleOpenEdit(wf)}>
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        {isAdmin && (
                          <Tooltip title="Delete Workflow (Admin only)">
                            <IconButton size="small" color="error" onClick={() => handleDeleteWorkflow(wf)}>
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                      </Box>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Dialog: Create Workflow */}
      <Dialog open={openNewWf} onClose={() => setOpenNewWf(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 600 }}>Create New Workflow</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            <TextField label="Workflow Code" value={wfCode} onChange={(e) => setWfCode(e.target.value)} fullWidth required />
            <TextField label="Workflow Name" value={wfName} onChange={(e) => setWfName(e.target.value)} fullWidth required />
            <TextField label="Description" value={wfDesc} onChange={(e) => setWfDesc(e.target.value)} fullWidth multiline rows={2} />

            {/* Target Work Type selector */}
            <FormControl fullWidth>
              <InputLabel id="wf-worktype-create-label">Applicable Employee Work Type</InputLabel>
              <Select
                labelId="wf-worktype-create-label"
                value={wfTargetWorkType}
                label="Applicable Employee Work Type"
                onChange={(e) => setWfTargetWorkType(e.target.value)}
              >
                <MenuItem value="ALL">👥 All Employees</MenuItem>
                <MenuItem value="IN_HOUSE">🏢 In-House</MenuItem>
                <MenuItem value="OUTSOURCED">🌐 Outsourced</MenuItem>
              </Select>
            </FormControl>

            <Autocomplete
              multiple
              options={availableLeaveTypes}
              getOptionLabel={(option) => `${option.name} (${option.code})`}
              value={availableLeaveTypes.filter(l => wfAssociatedLeaveTypes.includes(l.code))}
              onChange={(e, newVal) => setWfAssociatedLeaveTypes(newVal.map(v => v.code))}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Leave Types "
                  placeholder="Select leave types that use this workflow..."
                  helperText="Applications for any of these leave types will trigger this workflow"
                />
              )}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip
                    {...getTagProps({ index })}
                    key={option.code}
                    label={`${option.name} (${option.code})`}
                    color="primary"
                    size="small"
                  />
                ))
              }
            />
            <TextField label="Module" value={wfModule} onChange={(e) => setWfModule(e.target.value)} fullWidth />
            <TextField label="Category" value={wfCat} onChange={(e) => setWfCat(e.target.value)} fullWidth />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenNewWf(false)}>Cancel</Button>
          <Button onClick={handleCreateWorkflow} variant="contained">Create</Button>
        </DialogActions>
      </Dialog>

      {/* Dialog: Edit Workflow */}
      <Dialog open={openEditWf} onClose={() => setOpenEditWf(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 600 }}>Edit Workflow: {editingWf?.code}</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            <TextField label="Workflow Name" value={editWfName} onChange={(e) => setEditWfName(e.target.value)} fullWidth required />
            <TextField label="Description" value={editWfDesc} onChange={(e) => setEditWfDesc(e.target.value)} fullWidth multiline rows={2} />

            {/* Target Work Type selector */}
            <FormControl fullWidth>
              <InputLabel id="wf-worktype-edit-label">Applicable Employee Work Type</InputLabel>
              <Select
                labelId="wf-worktype-edit-label"
                value={editWfTargetWorkType}
                label="Applicable Employee Work Type"
                onChange={(e) => setEditWfTargetWorkType(e.target.value)}
              >
                <MenuItem value="ALL">👥 All Employees (In-House & Outsourced)</MenuItem>
                <MenuItem value="IN_HOUSE">🏢 In-House Employees </MenuItem>
                <MenuItem value="OUTSOURCED">🌐 Outsourced Employees</MenuItem>
              </Select>
            </FormControl>

            <Autocomplete
              multiple
              options={availableLeaveTypes}
              getOptionLabel={(option) => `${option.name} (${option.code})`}
              value={availableLeaveTypes.filter(l => editWfAssociatedLeaveTypes.includes(l.code))}
              onChange={(e, newVal) => setEditWfAssociatedLeaveTypes(newVal.map(v => v.code))}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Associated Leave Types (Multi-Select)"
                  placeholder="Select leave types..."
                  helperText="Leave applications matching any selected code will route through this workflow"
                />
              )}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip
                    {...getTagProps({ index })}
                    key={option.code}
                    label={`${option.name} (${option.code})`}
                    color="primary"
                    size="small"
                  />
                ))
              }
            />

            <TextField label="Module" value={editWfModule} onChange={(e) => setEditWfModule(e.target.value)} fullWidth />
            <TextField label="Category" value={editWfCat} onChange={(e) => setEditWfCat(e.target.value)} fullWidth />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenEditWf(false)}>Cancel</Button>
          <Button onClick={handleUpdateWorkflow} variant="contained">Save Changes</Button>
        </DialogActions>
      </Dialog>

      {/* Dialog: Versions List */}
      <Dialog open={openVersions} onClose={() => setOpenVersions(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 600 }}>
          {selectedWf?.name} Versions
        </DialogTitle>
        <DialogContent>
          <List>
            {versions.map((ver) => (
              <ListItem
                key={ver.id}
                sx={{
                  borderBottom: '1px solid rgba(255, 255, 255, 0.05)',
                  py: 2,
                  display: 'flex',
                  justifyContent: 'space-between',
                }}
              >
                <ListItemText
                  primary={
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body1" sx={{ fontWeight: 600 }}>
                        Version {ver.versionNumber}
                      </Typography>
                      <Chip
                        label={
                          ver.status === 1 ? 'Draft' :
                            ver.status === 2 ? 'Published' : 'Deactivated'
                        }
                        color={
                          ver.status === 2 ? 'success' :
                            ver.status === 1 ? 'warning' : 'default'
                        }
                        size="small"
                      />
                    </Box>
                  }
                  secondary={`Effective Date: ${ver.effectiveDate ? new Date(ver.effectiveDate).toLocaleDateString() : 'N/A'}`}
                />
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Tooltip title="Visual Steps Designer">
                    <IconButton size="small" onClick={() => { setOpenVersions(false); navigate(`/designer/${ver.id}`); }}>
                      <SettingsIcon fontSize="small" color="primary" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Dynamic Form Builder">
                    <IconButton size="small" onClick={() => { setOpenVersions(false); navigate(`/form-builder/${ver.id}`); }}>
                      <DynamicFormIcon fontSize="small" color="secondary" />
                    </IconButton>
                  </Tooltip>
                  {ver.status === 1 && (
                    <Tooltip title="Publish Version">
                      <IconButton size="small" onClick={() => handlePublish(ver.id)}>
                        <PlayArrowIcon fontSize="small" color="success" />
                      </IconButton>
                    </Tooltip>
                  )}
                  <Tooltip title="Clone to New Version Draft">
                    <IconButton size="small" onClick={() => handleClone(ver.id)}>
                      <ContentCopyIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Box>
              </ListItem>
            ))}
          </List>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenVersions(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
