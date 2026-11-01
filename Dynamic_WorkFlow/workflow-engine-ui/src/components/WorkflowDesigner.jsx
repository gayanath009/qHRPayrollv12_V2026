import React, { useEffect, useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Button,
  Grid,
  Card,
  CardContent,
  IconButton,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Switch,
  FormControlLabel,
  Paper,
  Divider,
  Chip,
  Tooltip,
  Autocomplete,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import SaveIcon from '@mui/icons-material/Save';
import api from '../services/api';

export default function WorkflowDesigner() {
  const { versionId } = useParams();
  const navigate = useNavigate();

  const [version, setVersion] = useState(null);
  const [steps, setSteps] = useState([]);
  const [selectedStep, setSelectedStep] = useState(null);
  const [roles, setRoles] = useState([]);
  const [users, setUsers] = useState([]);
  const [leaveTypes, setLeaveTypes] = useState([]);
  const [loading, setLoading] = useState(true);

  // Form Fields for Step Properties
  const [stepName, setStepName] = useState('');
  const [appType, setAppType] = useState(11); // Default to SpecificUser
  const [slaHours, setSlaHours] = useState(24);
  const [isMandatory, setIsMandatory] = useState(true);
  const [parallelApp, setParallelApp] = useState(false);
  const [emailNotif, setEmailNotif] = useState(true);
  const [smsNotif, setSmsNotif] = useState(false);
  const [commentsMandatory, setCommentsMandatory] = useState(false);
  const [attachmentsMandatory, setAttachmentsMandatory] = useState(false);
  
  const [targetUserId, setTargetUserId] = useState('');
  const [targetRoleId, setTargetRoleId] = useState('');
  const [targetWorkType, setTargetWorkType] = useState('ALL');

  const updateSelectedStepProperty = (field, value) => {
    if (!selectedStep) return;
    setSelectedStep(prev => prev ? { ...prev, [field]: value } : null);
    setSteps(prevSteps => prevSteps.map(s => {
      if (s.id === selectedStep.id) {
        return { ...s, [field]: value };
      }
      return s;
    }));
  };

  const loadData = async (preserveSelectedId = null) => {
    try {
      setLoading(true);
      const targetId = preserveSelectedId || selectedStep?.id;
      const vRes = await api.get(`/workflow/version/${versionId}`);
      setVersion(vRes.data);
      const loadedSteps = (vRes.data.steps || []).sort((a, b) => a.sequence - b.sequence);
      setSteps(loadedSteps);

      const rRes = await api.get('/admin/roles');
      setRoles(rRes.data || []);

      const uRes = await api.get('/admin/users');
      setUsers(uRes.data || []);

      const metaRes = await api.get('/workflow/metadata/leave-and-work-types').catch(() => null);
      if (metaRes?.data?.leaveTypes) {
        setLeaveTypes(metaRes.data.leaveTypes);
      }

      if (targetId && loadedSteps.length > 0) {
        const match = loadedSteps.find(s => String(s.id).toLowerCase() === String(targetId).toLowerCase()) ||
                      loadedSteps.find(s => s.stepNumber === selectedStep?.stepNumber) ||
                      loadedSteps[0];
        if (match) {
          handleStepClick(match);
        }
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [versionId]);

  const handleStepClick = (step) => {
    setSelectedStep(step);
    setStepName(step.name || '');
    setAppType(step.approvalType !== undefined ? Number(step.approvalType) : 11);
    setSlaHours(step.slaHours !== undefined ? Number(step.slaHours) : 24);
    setIsMandatory(step.isMandatory !== false);
    setParallelApp(!!step.parallelApproval);
    setEmailNotif(step.emailNotification !== false);
    setSmsNotif(!!step.smsNotification);
    setCommentsMandatory(!!step.commentsMandatory);
    setAttachmentsMandatory(!!step.attachmentMandatory);
    setTargetUserId(step.targetUserId ? String(step.targetUserId).toLowerCase() : '');
    setTargetRoleId(step.targetRoleId ? String(step.targetRoleId).toLowerCase() : '');
    setTargetWorkType(step.targetWorkType || 'ALL');
  };

  const handleAddStep = () => {
    const nextSequence = steps.length > 0 ? Math.max(...steps.map(s => s.sequence)) + 1 : 1;
    const newStep = {
      id: `temp-${Date.now()}`,
      stepNumber: nextSequence,
      name: `Step ${nextSequence} - Approval`,
      approvalType: 11,
      sequence: nextSequence,
      isMandatory: true,
      parallelApproval: false,
      slaHours: 24,
      emailNotification: true,
      smsNotification: false,
      commentsMandatory: false,
      attachmentMandatory: false,
      targetUserId: '',
      targetRoleId: '',
      targetWorkType: 'ALL',
      isNew: true,
    };
    const updated = [...steps, newStep];
    setSteps(updated);
    handleStepClick(newStep);
  };

  const handleDeleteStep = (id) => {
    const updated = steps.filter(s => s.id !== id).map((s, idx) => ({
      ...s,
      sequence: idx + 1,
      stepNumber: idx + 1
    }));
    setSteps(updated);
    if (selectedStep?.id === id) {
      setSelectedStep(null);
    }
  };

  const getCurrentMergedSteps = () => {
    return steps.map(s => {
      if (selectedStep && s.id === selectedStep.id) {
        return {
          ...s,
          name: stepName && stepName.trim() !== '' ? stepName.trim() : `Step ${s.stepNumber || s.sequence} - Approval`,
          approvalType: Number(appType),
          slaHours: parseInt(slaHours) || 24,
          isMandatory: isMandatory,
          parallelApproval: parallelApp,
          emailNotification: emailNotif,
          smsNotification: smsNotif,
          commentsMandatory: commentsMandatory,
          attachmentMandatory: attachmentsMandatory,
          targetUserId: targetUserId || null,
          targetRoleId: targetRoleId || null,
          targetWorkType: targetWorkType || 'ALL',
        };
      }
      return s;
    });
  };

  const saveStepsToServer = async (stepsList) => {
    try {
      setLoading(true);
      const listToSave = stepsList || getCurrentMergedSteps();
      const payload = listToSave.map(s => ({
        id: (s.id && !String(s.id).startsWith('temp-')) ? s.id : null,
        name: s.name && s.name.trim() !== '' ? s.name.trim() : `Step ${s.stepNumber || s.sequence} - Approval`,
        stepNumber: s.stepNumber || s.sequence,
        approvalType: Number(s.approvalType),
        sequence: s.sequence,
        isMandatory: s.isMandatory !== false,
        parallelApproval: !!s.parallelApproval,
        slaHours: parseInt(s.slaHours) || 24,
        emailNotification: s.emailNotification !== false,
        smsNotification: !!s.smsNotification,
        commentsMandatory: !!s.commentsMandatory,
        attachmentMandatory: !!s.attachmentMandatory,
        targetUserId: s.targetUserId ? String(s.targetUserId) : null,
        targetRoleId: s.targetRoleId ? String(s.targetRoleId) : null,
        targetWorkType: s.targetWorkType || 'ALL',
      }));

      const res = await api.put(`/workflow/version/${versionId}/steps`, payload);
      alert(res.data?.message || 'Workflow steps and assigned approvers saved successfully.');
      await loadData(selectedStep?.id);
    } catch (err) {
      console.error(err);
      const errMsg = err.response?.data?.message || err.response?.data?.title || err.response?.data?.details || err.message || 'Failed to save workflow steps.';
      alert(errMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveStepProperties = async () => {
    const updated = getCurrentMergedSteps();
    setSteps(updated);
    await saveStepsToServer(updated);
  };

  const handleSaveAll = async () => {
    const updated = getCurrentMergedSteps();
    setSteps(updated);
    await saveStepsToServer(updated);
  };

  const associatedLeaveList = useMemo(() => {
    if (!version?.associatedLeaveTypes) return [];
    const codes = String(version.associatedLeaveTypes)
      .split(',')
      .map(c => c.trim())
      .filter(Boolean);

    return codes.map(code => {
      const match = leaveTypes.find(lt => String(lt.code).toUpperCase() === code.toUpperCase());
      return {
        code,
        name: match?.name ? `${match.name} Leave` : code
      };
    });
  }, [version?.associatedLeaveTypes, leaveTypes]);

  const targetWorkTypeBadge = useMemo(() => {
    const wt = version?.targetWorkType;
    if (wt === 'IN_HOUSE' || wt === '1') return { label: '🏢 In-House Employees Only (WrktypId = 1)', color: 'info' };
    if (wt === 'OUTSOURCED' || wt === '2') return { label: '🌐 Outsourced Employees Only (WrktypId = 2)', color: 'warning' };
    return { label: '👥 All Employees (In-House & Outsourced)', color: 'default' };
  }, [version?.targetWorkType]);

  return (
    <Box className="animate-fade-in">
      <Paper
        className="glass-panel"
        sx={{
          p: 2.5,
          mb: 3.5,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 2,
          border: '1px solid rgba(255, 255, 255, 0.08)'
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
          <IconButton onClick={() => navigate('/dashboard')} sx={{ mt: 0.5 }}>
            <ArrowBackIcon />
          </IconButton>
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                Visual Step Designer
              </Typography>
              {version?.workflowName && (
                <Chip
                  label={version.workflowName}
                  color="primary"
                  sx={{ fontWeight: 700, fontSize: '0.9rem' }}
                />
              )}
              {version?.workflowCode && (
                <Chip
                  label={version.workflowCode}
                  variant="outlined"
                  size="small"
                  sx={{ fontWeight: 600 }}
                />
              )}
              <Chip
                label={`v${version?.versionNumber || 1}`}
                size="small"
                variant="outlined"
                color="secondary"
              />
              <Chip
                label={targetWorkTypeBadge.label}
                size="small"
                variant="outlined"
                sx={{ fontSize: '0.78rem' }}
              />
            </Box>

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap', mt: 1.2 }}>
              <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 600 }}>
                Associated Leave Types:
              </Typography>
              {associatedLeaveList.length > 0 ? (
                associatedLeaveList.map(item => (
                  <Chip
                    key={item.code}
                    label={`${item.name} (${item.code})`}
                    size="small"
                    sx={{
                      backgroundColor: 'rgba(2, 136, 209, 0.12)',
                      color: '#0288d1',
                      border: '1px solid rgba(2, 136, 209, 0.3)',
                      fontWeight: 600,
                      borderRadius: '6px'
                    }}
                  />
                ))
              ) : (
                <Chip
                  label="All Leave Types"
                  size="small"
                  variant="outlined"
                  sx={{ color: 'text.secondary' }}
                />
              )}
            </Box>
          </Box>
        </Box>

        <Box sx={{ display: 'flex', gap: 1.5, ml: 'auto' }}>
          <Button variant="contained" startIcon={<SaveIcon />} onClick={handleSaveAll}>
            Save Layout
          </Button>
        </Box>
      </Paper>

      <Grid container spacing={4}>
        {/* Step Flow Canvas */}
        <Grid item xs={12} md={7}>
          <Paper className="glass-panel" sx={{ p: 4, minHeight: '60vh', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Box
              sx={{
                width: 80,
                height: 40,
                borderRadius: 2,
                backgroundColor: 'success.main',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontWeight: 600,
                color: '#fff',
                mb: 2,
              }}
            >
              START
            </Box>

            {steps.map((step, idx) => (
              <Box key={step.id} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: '100%' }}>
                <Box className="node-connector" />

                <Card
                  onClick={() => handleStepClick(step)}
                  sx={{
                    width: '80%',
                    maxWidth: 400,
                    cursor: 'pointer',
                    border: selectedStep?.id === step.id ? '2px solid #540033' : '1px solid #e2e8f0',
                    backgroundColor: selectedStep?.id === step.id ? 'rgba(84, 0, 51, 0.05)' : '#ffffff',
                    transition: 'all 0.2s',
                    '&:hover': {
                      transform: 'translateY(-2px)',
                      boxShadow: '0 4px 16px rgba(84, 0, 51, 0.12)',
                    },
                  }}
                >
                  <CardContent sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', py: 2, '&:last-child': { pb: 2 } }}>
                    <Box>
                      <Typography variant="caption" sx={{ color: 'primary.main', fontWeight: 700 }}>
                        STEP {step.stepNumber}
                      </Typography>
                      <Typography variant="body1" sx={{ fontWeight: 600, mt: 0.5 }}>
                        {step.name || `Step ${step.stepNumber} - Approval`}
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
                        <Chip
                          label={
                            step.approvalType === 11 ? (users.find(u => String(u.id).toLowerCase() === String(step.targetUserId).toLowerCase())?.fullName || 'Specific User') :
                            step.approvalType === 10 ? (roles.find(r => String(r.id).toLowerCase() === String(step.targetRoleId).toLowerCase())?.name || 'Role Based') :
                            step.approvalType === 9 ? 'Department Head (HOD)' : 'Manager Hierarchy'
                          }
                          size="small"
                          color="secondary"
                          variant="outlined"
                        />
                        <Chip label={`${step.slaHours || 24}h SLA`} size="small" variant="outlined" />
                        {step.targetWorkType === 'IN_HOUSE' && (
                          <Chip label="🏢 In-House" size="small" color="primary" variant="filled" sx={{ fontWeight: 600, fontSize: '0.7rem' }} />
                        )}
                        {step.targetWorkType === 'OUTSOURCED' && (
                          <Chip label="🌐 Outsourced" size="small" color="warning" variant="filled" sx={{ fontWeight: 600, fontSize: '0.7rem' }} />
                        )}
                      </Box>
                    </Box>
                    <IconButton color="error" size="small" onClick={(e) => { e.stopPropagation(); handleDeleteStep(step.id); }}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </CardContent>
                </Card>
              </Box>
            ))}

            <Box className="node-connector" />
            
            <Button
              variant="outlined"
              startIcon={<AddIcon />}
              onClick={handleAddStep}
              sx={{ borderStyle: 'dashed', borderWidth: 2, px: 4, mb: 3 }}
            >
              Add Step
            </Button>

            <Box
              sx={{
                width: 80,
                height: 40,
                borderRadius: 2,
                backgroundColor: 'error.main',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontWeight: 600,
                color: '#fff',
              }}
            >
              END
            </Box>
          </Paper>
        </Grid>

        {/* Step Property Editor Panel */}
        <Grid item xs={12} md={5}>
          {selectedStep ? (
            <Paper className="glass-panel" sx={{ p: 3, position: 'sticky', top: 96 }}>
              <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
                Step Configurations
              </Typography>
              <Divider sx={{ mb: 3, borderColor: 'rgba(255,255,255,0.08)' }} />

              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
                <TextField
                  label="Step Name"
                  value={stepName}
                  onChange={(e) => {
                    const val = e.target.value;
                    setStepName(val);
                    updateSelectedStepProperty('name', val);
                  }}
                  fullWidth
                  required
                />

                {/* Target Work Type routing */}
                <FormControl fullWidth>
                  <InputLabel id="target-worktype-label">Applicable Employee Work Type</InputLabel>
                  <Select
                    labelId="target-worktype-label"
                    value={targetWorkType}
                    label="Applicable Employee Work Type"
                    onChange={(e) => {
                      const val = e.target.value;
                      setTargetWorkType(val);
                      updateSelectedStepProperty('targetWorkType', val);
                    }}
                  >
                    <MenuItem value="ALL">👥 All Employees (In-House & Outsourced)</MenuItem>
                    <MenuItem value="IN_HOUSE">🏢 In-House Employees Only (WrktypId = 1)</MenuItem>
                    <MenuItem value="OUTSOURCED">🌐 Outsourced Employees Only (WrktypId = 2)</MenuItem>
                  </Select>
                </FormControl>

                <FormControl fullWidth>
                  <InputLabel id="app-type-label">Approval Type</InputLabel>
                  <Select
                    labelId="app-type-label"
                    value={appType}
                    label="Approval Type"
                    onChange={(e) => {
                      const val = Number(e.target.value);
                      setAppType(val);
                      updateSelectedStepProperty('approvalType', val);
                    }}
                  >
                    <MenuItem value={11}>Specific User</MenuItem>
                    <MenuItem value={10}>Role Based</MenuItem>
                    <MenuItem value={9}>Department Head (HOD)</MenuItem>
                    <MenuItem value={8}>Manager Hierarchy</MenuItem>
                  </Select>
                </FormControl>

                {/* Specific User Target (Searchable Dropdown with qHRM Employees) */}
                {appType === 11 && (
                  <Autocomplete
                    id="target-user-autocomplete"
                    options={users}
                    getOptionLabel={(option) => {
                      if (!option) return '';
                      const idText = option.empSysId ? `[SysID: ${option.empSysId} / ${option.username}]` : (option.username ? `[${option.username}]` : '');
                      const deptText = option.departmentName ? `• ${option.departmentName}` : '';
                      return `${option.fullName || 'Employee'} ${idText} ${deptText}`.trim();
                    }}
                    value={users.find((u) => String(u.id).toLowerCase() === String(targetUserId).toLowerCase()) || null}
                    onChange={(event, newValue) => {
                      const val = newValue ? String(newValue.id).toLowerCase() : '';
                      setTargetUserId(val);
                      updateSelectedStepProperty('targetUserId', val);
                    }}
                    isOptionEqualToValue={(option, value) => String(option.id).toLowerCase() === String(value?.id).toLowerCase()}
                    renderInput={(params) => (
                      <TextField
                        {...params}
                        label="Select User / Employee (Searchable by SysID / Emp ID / Name)"
                        placeholder="Search by SysID, username, name, email, or dept..."
                        fullWidth
                        helperText={users.length > 0 ? `${users.length} employee(s) synchronized from qHRM` : 'Loading employees...'}
                      />
                    )}
                    renderOption={(props, option) => {
                      const { key, ...otherProps } = props;
                      return (
                        <Box component="li" key={key || option.id} {...otherProps} sx={{ py: 1 }}>
                          <Box sx={{ display: 'flex', flexDirection: 'column' }}>
                            <Typography variant="body2" sx={{ fontWeight: 600, color: 'text.primary' }}>
                              {option.fullName || option.username} <span style={{ color: '#0288d1', fontWeight: 500 }}>[{option.empSysId ? `SysID: ${option.empSysId} • ` : ''}{option.username}]</span>
                            </Typography>
                            <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                              {option.departmentName ? `Dept: ${option.departmentName}` : ''} {option.designationName ? `• ${option.designationName}` : ''} {option.email ? `• ${option.email}` : ''}
                            </Typography>
                          </Box>
                        </Box>
                      );
                    }}
                  />
                )}

                {/* Role Based Target */}
                {appType === 10 && (
                  <FormControl fullWidth>
                    <InputLabel id="target-role-label">Select Role</InputLabel>
                    <Select
                      labelId="target-role-label"
                      value={targetRoleId}
                      label="Select Role"
                      onChange={(e) => {
                        const val = e.target.value;
                        setTargetRoleId(val);
                        updateSelectedStepProperty('targetRoleId', val);
                      }}
                    >
                      {roles.map((r) => (
                        <MenuItem key={r.id} value={r.id}>
                          {r.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}

                <TextField
                  label="SLA Hours (Timeout)"
                  type="number"
                  value={slaHours}
                  onChange={(e) => {
                    const val = parseInt(e.target.value) || 24;
                    setSlaHours(val);
                    updateSelectedStepProperty('slaHours', val);
                  }}
                  fullWidth
                />

                <FormControlLabel
                  control={<Switch checked={isMandatory} onChange={(e) => {
                    const val = e.target.checked;
                    setIsMandatory(val);
                    updateSelectedStepProperty('isMandatory', val);
                  }} />}
                  label="Step is Mandatory"
                />

                <FormControlLabel
                  control={<Switch checked={parallelApp} onChange={(e) => {
                    const val = e.target.checked;
                    setParallelApp(val);
                    updateSelectedStepProperty('parallelApproval', val);
                  }} />}
                  label="Parallel Approvals Required"
                />

                <Divider sx={{ my: 1, borderColor: 'rgba(255,255,255,0.08)' }} />

                <Typography variant="body2" sx={{ fontWeight: 600, color: 'primary.main' }}>
                  Execution Constraints
                </Typography>

                <FormControlLabel
                  control={<Switch checked={commentsMandatory} onChange={(e) => {
                    const val = e.target.checked;
                    setCommentsMandatory(val);
                    updateSelectedStepProperty('commentsMandatory', val);
                  }} />}
                  label="Comments are Mandatory"
                />

                <FormControlLabel
                  control={<Switch checked={attachmentsMandatory} onChange={(e) => {
                    const val = e.target.checked;
                    setAttachmentsMandatory(val);
                    updateSelectedStepProperty('attachmentMandatory', val);
                  }} />}
                  label="Attachments are Mandatory"
                />

                <Button
                  variant="contained"
                  startIcon={<SaveIcon />}
                  onClick={handleSaveStepProperties}
                  sx={{ mt: 2 }}
                >
                  Update Step
                </Button>
              </Box>
            </Paper>
          ) : (
            <Paper className="glass-panel" sx={{ p: 4, textAlign: 'center', color: 'text.secondary' }}>
              Select a step in the flow to configure its properties
            </Paper>
          )}
        </Grid>
      </Grid>
    </Box>
  );
}
