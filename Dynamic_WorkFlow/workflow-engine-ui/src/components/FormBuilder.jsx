import React, { useEffect, useState } from 'react';
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
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import SaveIcon from '@mui/icons-material/Save';
import api from '../services/api';

export default function FormBuilder() {
  const { versionId } = useParams();
  const navigate = useNavigate();

  const [formName, setFormName] = useState('Dynamic Request Form');
  const [formDesc, setFormDesc] = useState('');
  const [fields, setFields] = useState([]);
  const [selectedField, setSelectedField] = useState(null);
  const [loading, setLoading] = useState(true);

  // Field Edit Form States
  const [fName, setFName] = useState('');
  const [fLabel, setFLabel] = useState('');
  const [fType, setFType] = useState(1); // TextBox
  const [fPlaceholder, setFPlaceholder] = useState('');
  const [fRequired, setFRequired] = useState(false);
  const [fReadOnly, setFReadOnly] = useState(false);
  const [fOptionsRaw, setFOptionsRaw] = useState(''); // Comma-separated options

  const loadData = async () => {
    try {
      setLoading(true);
      const res = await api.get(`/form/version/${versionId}`);
      if (res.data) {
        setFormName(res.data.name);
        setFormDesc(res.data.description || '');
        setFields(res.data.fields.sort((a, b) => a.order - b.order));
      }
    } catch (err) {
      if (err.response?.status !== 404) {
        console.error(err);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [versionId]);

  const handleFieldClick = (field) => {
    setSelectedField(field);
    setFName(field.name);
    setFLabel(field.label);
    setFType(field.type);
    setFPlaceholder(field.placeholder || '');
    setFRequired(field.isRequired);
    setFReadOnly(field.isReadOnly);
    
    // Parse options back to string format
    if (field.options && field.options.length > 0) {
      setFOptionsRaw(field.options.map(o => `${o.label}:${o.value}`).join(', '));
    } else {
      setFOptionsRaw('');
    }
  };

  const handleAddField = () => {
    const nextOrder = fields.length > 0 ? Math.max(...fields.map(f => f.order)) + 1 : 1;
    const newField = {
      id: `temp-${Date.now()}`,
      name: `field_${nextOrder}`,
      label: `Custom Field ${nextOrder}`,
      type: 1, // TextBox
      placeholder: 'Enter text...',
      isRequired: false,
      isReadOnly: false,
      order: nextOrder,
      options: [],
    };
    setFields([...fields, newField]);
    handleFieldClick(newField);
  };

  const handleDeleteField = (id) => {
    setFields(fields.filter(f => f.id !== id).map((f, idx) => ({ ...f, order: idx + 1 })));
    setSelectedField(null);
  };

  const handleSaveFieldProperties = () => {
    if (!selectedField) return;

    // Parse options
    const parsedOptions = fOptionsRaw
      .split(',')
      .map(str => str.trim())
      .filter(str => str.includes(':'))
      .map(str => {
        const [label, val] = str.split(':');
        return { label: label.trim(), value: val.trim() };
      });

    const updated = fields.map(f => {
      if (f.id === selectedField.id) {
        return {
          ...f,
          name: fName,
          label: fLabel,
          type: fType,
          placeholder: fPlaceholder,
          isRequired: fRequired,
          isReadOnly: fReadOnly,
          options: parsedOptions,
        };
      }
      return f;
    });

    setFields(updated);
    alert('Field properties updated in preview context.');
  };

  const handleSaveFormSchema = async () => {
    try {
      setLoading(true);
      await api.post('/form', {
        workflowVersionId: versionId,
        name: formName,
        description: formDesc,
        fields: fields.map(f => ({
          name: f.name,
          label: f.label,
          type: f.type,
          placeholder: f.placeholder,
          defaultValue: f.defaultValue,
          isRequired: f.isRequired,
          isReadOnly: f.isReadOnly,
          order: f.order,
          options: f.options
        }))
      });
      alert('Form layout schema saved successfully.');
    } catch (err) {
      alert('Failed to save form template');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box className="animate-fade-in">
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 4 }}>
        <IconButton onClick={() => navigate('/dashboard')}>
          <ArrowBackIcon />
        </IconButton>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>
            Form Template Builder
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            Design user data intake form layout schema
          </Typography>
        </Box>
        <Box sx={{ flexGrow: 1 }} />
        <Button variant="contained" startIcon={<SaveIcon />} onClick={handleSaveFormSchema}>
          Save Form Template
        </Button>
      </Box>

      <Grid container spacing={4}>
        {/* Form Live Preview Canvas */}
        <Grid item xs={12} md={7}>
          <Paper className="glass-panel" sx={{ p: 4, minHeight: '60vh' }}>
            <TextField
              variant="standard"
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              placeholder="Form Name"
              inputProps={{ style: { fontSize: 24, fontWeight: 700 } }}
              fullWidth
              sx={{ mb: 1 }}
            />
            <TextField
              variant="standard"
              value={formDesc}
              onChange={(e) => setFormDesc(e.target.value)}
              placeholder="Add description details..."
              inputProps={{ style: { fontSize: 14 } }}
              fullWidth
              sx={{ mb: 4 }}
            />
            <Divider sx={{ mb: 4, borderColor: 'rgba(255,255,255,0.08)' }} />

            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
              {fields.map((field) => (
                <Box
                  key={field.id}
                  onClick={() => handleFieldClick(field)}
                  sx={{
                    p: 2.5,
                    border: selectedField?.id === field.id ? '2.5px solid #540033' : '1px solid #e2e8f0',
                    borderRadius: 2.5,
                    backgroundColor: selectedField?.id === field.id ? 'rgba(84, 0, 51, 0.04)' : '#ffffff',
                    cursor: 'pointer',
                    position: 'relative',
                    transition: 'all 0.15s',
                    '&:hover': {
                      boxShadow: '0 4px 14px 0 rgba(84, 0, 51, 0.08)'
                    }
                  }}
                >
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1.5 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600, color: 'text.primary' }}>
                      {field.label} {field.isRequired && <span style={{ color: '#ef4444' }}>*</span>}
                    </Typography>
                    <IconButton size="small" color="error" onClick={(e) => { e.stopPropagation(); handleDeleteField(field.id); }}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Box>

                  {/* Rendering dummy controls for designer visual context */}
                  {field.type === 1 && (
                    <TextField placeholder={field.placeholder || ''} fullWidth disabled variant="outlined" size="small" />
                  )}
                  {field.type === 2 && (
                    <Select fullWidth disabled size="small" value="">
                      <MenuItem value="">{field.placeholder || 'Select option...'}</MenuItem>
                    </Select>
                  )}
                  {field.type === 3 && (
                    <TextField type="date" fullWidth disabled variant="outlined" size="small" />
                  )}
                  {field.type === 4 && (
                    <TextField type="number" placeholder={field.placeholder || '0'} fullWidth disabled variant="outlined" size="small" />
                  )}
                  {field.type === 7 && (
                    <TextField multiline rows={2} placeholder={field.placeholder || ''} fullWidth disabled variant="outlined" size="small" />
                  )}
                  {field.type === 9 && (
                    <Box sx={{ p: 2, border: '1px dashed rgba(255,255,255,0.15)', borderRadius: 2, textAlign: 'center', backgroundColor: 'rgba(0,0,0,0.15)' }}>
                      Upload Attachments Mockup
                    </Box>
                  )}
                  {field.type === 10 && (
                    <Box sx={{ p: 2, border: '1px solid rgba(255,255,255,0.08)', borderRadius: 2, textAlign: 'center', color: '#9ca3af', backgroundColor: 'rgba(0,0,0,0.15)' }}>
                      [ Digital Signature Panel ]
                    </Box>
                  )}
                </Box>
              ))}

              <Button
                variant="outlined"
                startIcon={<AddIcon />}
                onClick={handleAddField}
                sx={{ borderStyle: 'dashed', borderWidth: 2, py: 1.5 }}
              >
                Add Field
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* Field Editor Property Sidebar Panel */}
        <Grid item xs={12} md={5}>
          {selectedField ? (
            <Paper className="glass-panel" sx={{ p: 3, position: 'sticky', top: 96 }}>
              <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
                Field Configurations
              </Typography>
              <Divider sx={{ mb: 3, borderColor: 'rgba(255,255,255,0.08)' }} />

              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
                <TextField
                  label="Field Code Name (e.g. LeaveDays)"
                  value={fName}
                  onChange={(e) => setFName(e.target.value)}
                  fullWidth
                  required
                />
                <TextField
                  label="Display Label Name (e.g. Leave Days)"
                  value={fLabel}
                  onChange={(e) => setFLabel(e.target.value)}
                  fullWidth
                  required
                />

                <FormControl fullWidth>
                  <InputLabel id="field-type-label">Control Type</InputLabel>
                  <Select
                    labelId="field-type-label"
                    value={fType}
                    label="Control Type"
                    onChange={(e) => setFType(e.target.value)}
                  >
                    <MenuItem value={1}>TextBox</MenuItem>
                    <MenuItem value={2}>DropDown Selector</MenuItem>
                    <MenuItem value={3}>Date Picker</MenuItem>
                    <MenuItem value={4}>Number Input</MenuItem>
                    <MenuItem value={7}>TextArea Box</MenuItem>
                    <MenuItem value={9}>Attachment Upload</MenuItem>
                    <MenuItem value={10}>Digital Signature Area</MenuItem>
                  </Select>
                </FormControl>

                <TextField
                  label="Placeholder Text"
                  value={fPlaceholder}
                  onChange={(e) => setFPlaceholder(e.target.value)}
                  fullWidth
                />

                {fType === 2 && (
                  <TextField
                    label="DropDown Options (Comma separated e.g. Yes:Y, No:N)"
                    value={fOptionsRaw}
                    onChange={(e) => setFOptionsRaw(e.target.value)}
                    helperText="Format as Label:Value, separated by commas."
                    fullWidth
                    multiline
                    rows={2}
                  />
                )}

                <FormControlLabel
                  control={<Switch checked={fRequired} onChange={(e) => setFRequired(e.target.checked)} />}
                  label="Mark field as Required"
                />

                <FormControlLabel
                  control={<Switch checked={fReadOnly} onChange={(e) => setFReadOnly(e.target.checked)} />}
                  label="Mark field as ReadOnly"
                />

                <Button
                  variant="contained"
                  onClick={handleSaveFieldProperties}
                  sx={{ mt: 2 }}
                >
                  Apply Field Changes
                </Button>
              </Box>
            </Paper>
          ) : (
            <Paper className="glass-panel" sx={{ p: 4, textAlign: 'center', color: 'text.secondary' }}>
              Select a field control in the preview canvas to configure its settings
            </Paper>
          )}
        </Grid>
      </Grid>
    </Box>
  );
}
