@echo off
echo ==============================================
echo Building and Publishing Workflow Engine UI...
echo ==============================================

call npm.cmd run build
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    exit /b %ERRORLEVEL%
)

set DEST=D:\My Projects\04_QCS Applications\New HR_Payroll\Publish\WorkflowEngineUI

echo Publishing fresh files to: %DEST%
powershell -NoProfile -Command "if (Test-Path '%DEST%\assets') { Remove-Item '%DEST%\assets' -Recurse -Force }; Copy-Item -Path './dist/*' -Destination '%DEST%' -Recurse -Force"

echo ==============================================
echo Published successfully!
echo To change the API URL in production, edit:
echo %DEST%\config.js
echo ==============================================
