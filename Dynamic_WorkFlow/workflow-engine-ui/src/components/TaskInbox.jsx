import React, { useEffect, useState } from 'react';
import {
  Box,
  Tabs,
  Tab,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Checkbox,
  FormControlLabel,
  Divider,
  List,
  ListItem,
  ListItemText,
  Paper,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import ReplyIcon from '@mui/icons-material/Reply';
import SwapHorizIcon from '@mui/icons-material/SwapHoriz';
import HistoryIcon from '@mui/icons-material/History';
import AddIcon from '@mui/icons-material/Add';
import api from '../services/api';

export default function TaskInbox() {
  const [activeTab, setActiveTab] = useState(0);
  const [pendingTasks, setPendingTasks] = useState([]);
  const [completedTasks, setCompletedTasks] = useState([]);
  const [myRequests, setMyRequests] = useState([]);
  const [users, setUsers] = useState([]);
  const [workflows, setWorkflows] = useState([]);

  // Detail View Dialogs
  const [openActionDialog, setOpenActionDialog] = useState(false);
  const [selectedTask, setSelectedTask] = useState(null);
  const [instanceDetails, setInstanceDetails] = useState(null);
  
  // Action inputs
  const [comment, setComment] = useState('');
  const [signatureChecked, setSignatureChecked] = useState(false);
  const [delegateUserId, setDelegateUserId] = useState('');
  
  // Submit new request dialog
  const [openNewRequest, setOpenNewRequest] = useState(false);
  const [selectedWorkflowId, setSelectedWorkflowId] = useState('');
  const [formTemplate, setFormTemplate] = useState(null);
  const [formData, setFormData] = useState({});

  // History timeline dialog
  const [openHistoryDialog, setOpenHistoryDialog] = useState(false);
  const [historyTimeline, setHistoryTimeline] = useState([]);

  const loadInbox = async () => {
    try {
      const pRes = await api.get('/inbox/pending');
      setPendingTasks(pRes.data);

      const cRes = await api.get('/inbox/completed');
      setCompletedTasks(cRes.data);

      const mRes = await api.get('/inbox/requests');
      setMyRequests(mRes.data);

      const uRes = await api.get('/admin/users');
      setUsers(uRes.data);

      const wRes = await api.get('/workflow');
      // Only show active workflows in new request picker
      setWorkflows(wRes.data.filter(w => w.isActive));
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    loadInbox();
  }, []);

  const handleTaskClick = async (task) => {
    setSelectedTask(task);
    setComment('');
    setSignatureChecked(false);
    setDelegateUserId('');
    try {
      const res = await api.get(`/workflowexecution/instance/${task.instanceId}`);
      setInstanceDetails(res.data);
      setOpenActionDialog(true);
    } catch (err) {
      console.error(err);
    }
  };

  const handleAction = async (actionType) => {
    if (!selectedTask) return;
    
    // Digital Signature Check
    const sigStr = signatureChecked ? 'DIGITALLY_SIGNED_BY_USER' : null;

    try {
      if (actionType === 'Approve') {
        await api.post('/workflowexecution/approve', {
          approvalId: selectedTask.approvalId,
          comment,
          digitalSignature: sigStr,
        });
      } else if (actionType === 'Reject') {
        await api.post('/workflowexecution/reject', {
          approvalId: selectedTask.approvalId,
          comment,
        });
      } else if (actionType === 'Return') {
        await api.post('/workflowexecution/return', {
          approvalId: selectedTask.approvalId,
          comment,
        });
      } else if (actionType === 'Delegate') {
        if (!delegateUserId) {
          alert('Please select a user to delegate to.');
          return;
        }
        await api.post('/workflowexecution/delegate', {
          approvalId: selectedTask.approvalId,
          delegateUserId,
          comment,
        });
      }

      setOpenActionDialog(false);
      loadInbox();
    } catch (err) {
      alert(err.response?.data?.message || 'Action execution failed.');
    }
  };

  const handleViewHistory = async (instanceId) => {
    try {
      const res = await api.get(`/workflowexecution/history/${instanceId}`);
      setHistoryTimeline(res.data);
      setOpenHistoryDialog(true);
    } catch (err) {
      console.error(err);
    }
  };

  const handleWorkflowSelect = async (wfId) => {
    setSelectedWorkflowId(wfId);
    setFormData({});
    try {
      // Find the published version ID
      const verRes = await api.get(`/workflow/${wfId}/versions`);
      const publishedVer = verRes.data.find(v => v.status === 2);
      if (publishedVer) {
        // Load form template
        const formRes = await api.get(`/form/version/${publishedVer.id}`);
        setFormTemplate(formRes.data);
      } else {
        alert('This workflow does not have a published template version.');
        setFormTemplate(null);
      }
    } catch (err) {
      console.error(err);
      setFormTemplate(null);
    }
  };

  const handleFormChange = (fieldName, val) => {
    setFormData({
      ...formData,
      [fieldName]: val.toString(),
    });
  };

  const handleNewRequestSubmit = async () => {
    const selectedWfObj = workflows.find(w => w.id === selectedWorkflowId);
    if (!selectedWfObj) return;

    try {
      await api.post('/workflowexecution/start', {
        workflowCode: selectedWfObj.code,
        inputData: formData,
      });
      setOpenNewRequest(false);
      loadInbox();
      alert('Workflow request submitted successfully.');
    } catch (err) {
      alert('Failed to submit workflow request.');
    }
  };

  return (
    <Box className="animate-fade-in">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Tasks & Inbox
        </Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => { setOpenNewRequest(true); setFormTemplate(null); setSelectedWorkflowId(''); }}>
          New Request
        </Button>
      </Box>

      <Box sx={{ borderBottom: 1, borderColor: 'rgba(255, 255, 255, 0.08)', mb: 3 }}>
        <Tabs value={activeTab} onChange={(e, val) => setActiveTab(val)} textColor="primary" indicatorColor="primary">
          <Tab label={`Pending Approvals (${pendingTasks.length})`} sx={{ fontWeight: 600 }} />
          <Tab label="Action Sent History" sx={{ fontWeight: 600 }} />
          <Tab label="My Submitted Requests" sx={{ fontWeight: 600 }} />
        </Tabs>
      </Box>

      {/* Tab 0: Pending Tasks */}
      {activeTab === 0 && (
        <Grid container spacing={2}>
          {pendingTasks.length === 0 ? (
            <Grid item xs={12}>
              <Paper className="glass-panel" sx={{ p: 4, textAlign: 'center', color: 'text.secondary' }}>
                You have no pending tasks.
              </Paper>
            </Grid>
          ) : (
            pendingTasks.map((task) => (
              <Grid item xs={12} key={task.approvalId}>
                <Card
                  onClick={() => handleTaskClick(task)}
                  sx={{
                    cursor: 'pointer',
                    backgroundColor: 'rgba(15,23,42,0.3)',
                    border: '1px solid rgba(255,255,255,0.08)',
                    transition: 'all 0.2s',
                    '&:hover': {
                      transform: 'translateY(-2px)',
                      boxShadow: '0 4px 20px rgba(99,102,241,0.12)',
                      borderColor: 'rgba(99, 102, 241, 0.4)',
                    },
                  }}
                >
                  <CardContent sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                        {task.workflowName} ({task.instanceNumber})
                      </Typography>
                      <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
                        Step: <span style={{ color: '#540033', fontWeight: 700 }}>{task.currentStepName}</span> • Submitted by {task.requesterName}
                      </Typography>
                    </Box>
                    <Box sx={{ textAlign: 'right' }}>
                      <Chip label={`SLA Due: ${new Date(task.dueDate).toLocaleString()}`} color="warning" size="small" variant="outlined" />
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))
          )}
        </Grid>
      )}

      {/* Tab 1: Completed Approvals */}
      {activeTab === 1 && (
        <Grid container spacing={2}>
          {completedTasks.length === 0 ? (
            <Grid item xs={12}>
              <Paper className="glass-panel" sx={{ p: 4, textAlign: 'center', color: 'text.secondary' }}>
                No completed approval logs.
              </Paper>
            </Grid>
          ) : (
            completedTasks.map((task, idx) => (
              <Grid item xs={12} key={idx}>
                <Card sx={{ backgroundColor: 'rgba(15,23,42,0.15)', border: '1px solid rgba(255,255,255,0.05)' }}>
                  <CardContent sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                        {task.workflowName} ({task.instanceNumber})
                      </Typography>
                      <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
                        Step Evaluated: {task.currentStepName} • Submitted by {task.requesterName}
                      </Typography>
                    </Box>
                    <Box>
                      <Chip label="Processed" color="success" size="small" />
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))
          )}
        </Grid>
      )}

      {/* Tab 2: My Submitted Requests */}
      {activeTab === 2 && (
        <Grid container spacing={2}>
          {myRequests.length === 0 ? (
            <Grid item xs={12}>
              <Paper className="glass-panel" sx={{ p: 4, textAlign: 'center', color: 'text.secondary' }}>
                You have not submitted any workflow requests.
              </Paper>
            </Grid>
          ) : (
            myRequests.map((req) => (
              <Grid item xs={12} key={req.id}>
                <Card sx={{ backgroundColor: 'rgba(15,23,42,0.3)', border: '1px solid rgba(255,255,255,0.08)' }}>
                  <CardContent sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                        {req.workflowName} ({req.instanceNumber})
                      </Typography>
                      <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
                        Start Date: {new Date(req.startDate).toLocaleString()}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                      <Chip
                        label={
                          req.status === 1 ? 'InProgress' :
                          req.status === 2 ? 'Approved' :
                          req.status === 3 ? 'Rejected' :
                          req.status === 4 ? 'Returned' : 'Cancelled'
                        }
                        color={
                          req.status === 2 ? 'success' :
                          req.status === 3 ? 'error' :
                          req.status === 1 ? 'warning' : 'default'
                        }
                        size="small"
                      />
                      <IconButton size="small" onClick={() => handleViewHistory(req.id)}>
                        <HistoryIcon />
                      </IconButton>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))
          )}
        </Grid>
      )}

      {/* Dialog: Task Action Dialog */}
      <Dialog open={openActionDialog} onClose={() => setOpenActionDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 600 }}>
          Evaluate Request: {selectedTask?.instanceNumber}
        </DialogTitle>
        <DialogContent dividers sx={{ borderColor: 'rgba(255,255,255,0.08)' }}>
          {instanceDetails && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
              <Typography variant="subtitle2" sx={{ color: 'primary.main', fontWeight: 600 }}>
                SUBMITTED DATA
              </Typography>
              <Grid container spacing={2}>
                {instanceDetails.data.map((d, idx) => (
                  <Grid item xs={6} key={idx}>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                      {d.fieldName}
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 500 }}>
                      {d.fieldValue || 'N/A'}
                    </Typography>
                  </Grid>
                ))}
              </Grid>

              <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />

              <TextField
                label="Approver Decision Comments"
                placeholder="Enter rationale for approval, rejection, or return..."
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                multiline
                rows={3}
                fullWidth
              />

              <FormControlLabel
                control={<Checkbox checked={signatureChecked} onChange={(e) => setSignatureChecked(e.target.checked)} />}
                label="I authorize and sign this transaction electronically."
              />

              <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
              
              <Box>
                <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mb: 1 }}>
                  DELEGATION OPTIONS (IF ASSIGNING TASK TO PROXY)
                </Typography>
                <FormControl fullWidth size="small">
                  <InputLabel id="delegate-user-label">Select Proxy Approver</InputLabel>
                  <Select
                    labelId="delegate-user-label"
                    value={delegateUserId}
                    label="Select Proxy Approver"
                    onChange={(e) => setDelegateUserId(e.target.value)}
                  >
                    {users.map((u) => (
                      <MenuItem key={u.id} value={u.id}>{u.fullName}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenActionDialog(false)}>Cancel</Button>
          <Button onClick={() => handleAction('Return')} color="warning" startIcon={<ReplyIcon />}>Return</Button>
          <Button onClick={() => handleAction('Reject')} color="error" startIcon={<CancelIcon />}>Reject</Button>
          {delegateUserId ? (
            <Button onClick={() => handleAction('Delegate')} color="info" startIcon={<SwapHorizIcon />}>Delegate</Button>
          ) : (
            <Button onClick={() => handleAction('Approve')} color="success" startIcon={<CheckCircleIcon />} variant="contained">Approve</Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Dialog: Submit New Request */}
      <Dialog open={openNewRequest} onClose={() => setOpenNewRequest(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 600 }}>Submit New Workflow Request</DialogTitle>
        <DialogContent dividers sx={{ borderColor: 'rgba(255,255,255,0.08)' }}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, mt: 1 }}>
            <FormControl fullWidth>
              <InputLabel id="wf-select-label">Select Workflow Template</InputLabel>
              <Select
                labelId="wf-select-label"
                value={selectedWorkflowId}
                label="Select Workflow Template"
                onChange={(e) => handleWorkflowSelect(e.target.value)}
              >
                {workflows.map((w) => (
                  <MenuItem key={w.id} value={w.id}>{w.name}</MenuItem>
                ))}
              </Select>
            </FormControl>

            {formTemplate && (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
                <Typography variant="subtitle2" sx={{ color: 'primary.main', fontWeight: 600 }}>
                  {formTemplate.name}
                </Typography>
                
                {formTemplate.fields.map((f) => (
                  <Box key={f.id}>
                    {f.type === 1 && (
                      <TextField
                        label={f.label}
                        placeholder={f.placeholder || ''}
                        required={f.isRequired}
                        fullWidth
                        onChange={(e) => handleFormChange(f.name, e.target.value)}
                      />
                    )}
                    {f.type === 2 && (
                      <FormControl fullWidth required={f.isRequired}>
                        <InputLabel>{f.label}</InputLabel>
                        <Select
                          label={f.label}
                          value={formData[f.name] || ''}
                          onChange={(e) => handleFormChange(f.name, e.target.value)}
                        >
                          {f.options.map((opt) => (
                            <MenuItem key={opt.id} value={opt.value}>{opt.label}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    )}
                    {f.type === 3 && (
                      <TextField
                        label={f.label}
                        type="date"
                        required={f.isRequired}
                        InputLabelProps={{ shrink: true }}
                        fullWidth
                        onChange={(e) => handleFormChange(f.name, e.target.value)}
                      />
                    )}
                    {f.type === 4 && (
                      <TextField
                        label={f.label}
                        type="number"
                        placeholder={f.placeholder || ''}
                        required={f.isRequired}
                        fullWidth
                        onChange={(e) => handleFormChange(f.name, e.target.value)}
                      />
                    )}
                    {f.type === 7 && (
                      <TextField
                        label={f.label}
                        multiline
                        rows={2}
                        placeholder={f.placeholder || ''}
                        required={f.isRequired}
                        fullWidth
                        onChange={(e) => handleFormChange(f.name, e.target.value)}
                      />
                    )}
                  </Box>
                ))}
              </Box>
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenNewRequest(false)}>Cancel</Button>
          <Button onClick={handleNewRequestSubmit} variant="contained" color="primary" disabled={!formTemplate}>Submit Request</Button>
        </DialogActions>
      </Dialog>

      {/* Dialog: History Timeline Viewer */}
      <Dialog open={openHistoryDialog} onClose={() => setOpenHistoryDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 600 }}>Workflow Execution Timeline</DialogTitle>
        <DialogContent dividers sx={{ borderColor: 'rgba(255,255,255,0.08)' }}>
          <List sx={{ py: 0 }}>
            {historyTimeline.map((item, idx) => (
              <ListItem key={item.id} sx={{ px: 0, py: 2, borderBottom: idx < historyTimeline.length - 1 ? '1px solid rgba(255,255,255,0.05)' : 'none' }}>
                <ListItemText
                  primary={
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="body1" sx={{ fontWeight: 600 }}>
                        {item.stepName} - {item.action}
                      </Typography>
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        {new Date(item.actionDate).toLocaleString()}
                      </Typography>
                    </Box>
                  }
                  secondary={
                    <Box sx={{ mt: 0.5 }}>
                      <Typography variant="body2" sx={{ color: 'text.primary', fontWeight: 500 }}>
                        Action by: {item.performedByName}
                      </Typography>
                      {item.comment && (
                        <Typography variant="caption" sx={{ display: 'block', mt: 0.5, p: 1, borderRadius: 1.5, backgroundColor: 'rgba(84, 0, 51, 0.04)', color: 'text.secondary', borderLeft: '3px solid #540033' }}>
                          Comment: {item.comment}
                        </Typography>
                      )}
                    </Box>
                  }
                />
              </ListItem>
            ))}
          </List>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenHistoryDialog(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
