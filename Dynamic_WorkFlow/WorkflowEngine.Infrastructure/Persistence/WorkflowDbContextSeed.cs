using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Infrastructure.Persistence;

public static class WorkflowDbContextSeed
{
    public static async Task SeedSampleDataAsync(WorkflowDbContext context)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'EmpSysId') " +
                "ALTER TABLE Users ADD EmpSysId INT NULL;");
        }
        catch { }

        // 1. Seed Company
        Company? company = null;
        if (!await context.Companies.AnyAsync())
        {
            company = new Company
            {
                Name = "Antigravity Global Qatar",
                Code = "AGG-QA",
                Description = "Headquarters in Qatar"
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();
        }
        else
        {
            company = await context.Companies.FirstAsync();
        }

        // 2. Seed Branch
        Branch? branch = null;
        if (!await context.Branches.AnyAsync())
        {
            branch = new Branch
            {
                Name = "Doha Branch",
                Code = "DOH-BR",
                CompanyId = company.Id
            };
            await context.Branches.AddAsync(branch);
            await context.SaveChangesAsync();
        }
        else
        {
            branch = await context.Branches.FirstAsync();
        }

        // 3. Seed Roles
        var roles = new Dictionary<string, Role>();
        if (!await context.Roles.AnyAsync())
        {
            var roleNames = new[] { "Admin", "Manager", "Employee", "HR", "Finance", "Procurement", "CEO" };
            foreach (var rName in roleNames)
            {
                var role = new Role
                {
                    Name = $"{rName} Role",
                    Code = rName.ToUpper(),
                    Description = $"{rName} security access level"
                };
                await context.Roles.AddAsync(role);
                roles.Add(rName.ToUpper(), role);
            }
            await context.SaveChangesAsync();
        }
        else
        {
            var dbRoles = await context.Roles.ToListAsync();
            foreach (var r in dbRoles)
            {
                roles[r.Code] = r;
            }
        }

        // 4. Seed Departments
        var depts = new Dictionary<string, Department>();
        if (!await context.Departments.AnyAsync())
        {
            var deptNames = new[] { "IT", "HR", "Finance", "Procurement", "CEO Office" };
            foreach (var dName in deptNames)
            {
                var dept = new Department
                {
                    Name = dName,
                    Code = dName.ToUpper().Replace(" ", "")
                };
                await context.Departments.AddAsync(dept);
                depts.Add(dept.Code, dept);
            }
            await context.SaveChangesAsync();
        }
        else
        {
            var dbDepts = await context.Departments.ToListAsync();
            foreach (var d in dbDepts)
            {
                depts[d.Code] = d;
            }
        }

        // 5. Seed Designations
        var desigs = new Dictionary<string, Designation>();
        if (!await context.Designations.AnyAsync())
        {
            var desigData = new Dictionary<string, string>
            {
                { "CEO", "Chief Executive Officer" },
                { "HOD", "Head of Department" },
                { "MGR", "Manager" },
                { "ENG", "Software Engineer" },
                { "EXEC", "Executive" },
                { "ANL", "Financial Analyst" }
            };
            foreach (var kvp in desigData)
            {
                var desig = new Designation
                {
                    Code = kvp.Key,
                    Name = kvp.Value
                };
                await context.Designations.AddAsync(desig);
                desigs.Add(kvp.Key, desig);
            }
            await context.SaveChangesAsync();
        }
        else
        {
            var dbDesigs = await context.Designations.ToListAsync();
            foreach (var d in dbDesigs)
            {
                desigs[d.Code] = d;
            }
        }

        // 6. Seed Users
        var users = new Dictionary<string, User>();
        if (!await context.Users.AnyAsync())
        {
            var passHash = HashPassword("Password123!");

            // CEO
            var ceo = new User
            {
                Username = "ceo",
                Email = "ceo@antigravity.qa",
                PasswordHash = passHash,
                FullName = "Marcus Sterling",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["CEO"].Id,
                DepartmentId = depts["CEOOFFICE"].Id,
                DesignationId = desigs["CEO"].Id
            };
            await context.Users.AddAsync(ceo);
            await context.SaveChangesAsync(); // To get CEO ID
            users["ceo"] = ceo;

            // HR Manager
            var hrMgr = new User
            {
                Username = "hr_mgr",
                Email = "hrmgr@antigravity.qa",
                PasswordHash = passHash,
                FullName = "Sarah Jenkins",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["HR"].Id,
                DepartmentId = depts["HR"].Id,
                DesignationId = desigs["HOD"].Id,
                ManagerId = ceo.Id
            };
            await context.Users.AddAsync(hrMgr);
            users["hr_mgr"] = hrMgr;

            // Finance Manager
            var finMgr = new User
            {
                Username = "fin_mgr",
                Email = "finmgr@antigravity.qa",
                PasswordHash = passHash,
                FullName = "Robert Vance",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["FINANCE"].Id,
                DepartmentId = depts["FINANCE"].Id,
                DesignationId = desigs["HOD"].Id,
                ManagerId = ceo.Id
            };
            await context.Users.AddAsync(finMgr);
            users["fin_mgr"] = finMgr;

            // Procurement Manager
            var procMgr = new User
            {
                Username = "proc_mgr",
                Email = "procmgr@antigravity.qa",
                PasswordHash = passHash,
                FullName = "Thomas Miller",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["PROCUREMENT"].Id,
                DepartmentId = depts["PROCUREMENT"].Id,
                DesignationId = desigs["HOD"].Id,
                ManagerId = ceo.Id
            };
            await context.Users.AddAsync(procMgr);
            users["proc_mgr"] = procMgr;

            // IT HOD
            var itHod = new User
            {
                Username = "it_hod",
                Email = "ithod@antigravity.qa",
                PasswordHash = passHash,
                FullName = "Devid Gilmour",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["MANAGER"].Id,
                DepartmentId = depts["IT"].Id,
                DesignationId = desigs["HOD"].Id,
                ManagerId = ceo.Id
            };
            await context.Users.AddAsync(itHod);
            await context.SaveChangesAsync();
            users["it_hod"] = itHod;

            // Software Engineer (Employee)
            var emp = new User
            {
                Username = "employee",
                Email = "emp@antigravity.qa",
                PasswordHash = passHash,
                FullName = "John Doe",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["EMPLOYEE"].Id,
                DepartmentId = depts["IT"].Id,
                DesignationId = desigs["ENG"].Id,
                ManagerId = itHod.Id
            };
            await context.Users.AddAsync(emp);

            // Admin
            var admin = new User
            {
                Username = "admin",
                Email = "admin@antigravity.qa",
                PasswordHash = passHash,
                FullName = "System Administrator",
                CompanyId = company.Id,
                BranchId = branch.Id,
                RoleId = roles["ADMIN"].Id,
                DepartmentId = depts["IT"].Id,
                DesignationId = desigs["MGR"].Id
            };
            await context.Users.AddAsync(admin);

            // System
            var system = new User
            {
                Username = "system",
                Email = "system@antigravity.qa",
                PasswordHash = passHash,
                FullName = "System Automation",
                RoleId = roles["ADMIN"].Id,
                IsActive = false
            };
            await context.Users.AddAsync(system);

            await context.SaveChangesAsync();
            
            var dbUsers = await context.Users.ToListAsync();
            foreach (var u in dbUsers)
            {
                users[u.Username] = u;
            }
        }
        else
        {
            var dbUsers = await context.Users.ToListAsync();
            foreach (var u in dbUsers)
            {
                users[u.Username] = u;
            }
        }

        // 7. Seed Notification Templates
        if (!await context.NotificationTemplates.AnyAsync())
        {
            await context.NotificationTemplates.AddRangeAsync(
                new NotificationTemplate
                {
                    Name = "Approval Request Notification",
                    Code = "APPROVAL_REQ",
                    Subject = "Action Required: Approval Request for {InstanceNumber}",
                    Body = "Dear {ApproverName},\n\nYou have a pending approval request for {InstanceNumber} ({WorkflowName}) submitted by {RequesterName}.\n\nPlease login to the Workflow portal to review and take action.\n\nBest regards,\nWorkflow Engine",
                    Type = "Email"
                },
                new NotificationTemplate
                {
                    Name = "Workflow Complete Notification",
                    Code = "WORKFLOW_COMPLETE",
                    Subject = "Workflow Status Update: {InstanceNumber} is {Status}",
                    Body = "Dear {RequesterName},\n\nWe are pleased to inform you that your request {InstanceNumber} has been successfully {Status}.\n\nDetails:\nWorkflow: {WorkflowName}\nCompleted Date: {EndDate}\n\nBest regards,\nWorkflow Engine",
                    Type = "Email"
                }
            );
            await context.SaveChangesAsync();
        }

        // 8. Seed Sick Leave Workflow
        if (!await context.WorkflowDefinitions.AnyAsync())
        {
            var sickLeaveDef = new WorkflowDefinition
            {
                Name = "Sick Leave Request",
                Code = "SickLeave",
                Description = "Sick leave request approval workflow routing through Employee, HR Manager, Reporting Manager, BU Head, and Finance.",
                Module = "HR",
                Category = "Leave Management"
            };
            await context.WorkflowDefinitions.AddAsync(sickLeaveDef);
            await context.SaveChangesAsync();

            var sickLeaveVersion = new WorkflowVersion
            {
                WorkflowDefinitionId = sickLeaveDef.Id,
                VersionNumber = 1,
                Status = WorkflowStatus.Published,
                EffectiveDate = DateTime.UtcNow,
                Priority = 1
            };
            await context.WorkflowVersions.AddAsync(sickLeaveVersion);
            await context.SaveChangesAsync();

            var kaushalya = await context.Users.FirstOrDefaultAsync(u => u.Username == "4000151" || u.FullName.Contains("Kaushalya"));
            var ashhar = await context.Users.FirstOrDefaultAsync(u => u.Username == "4000622" || u.FullName.Contains("Ashhar"));
            var empRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "EMPLOYEE");

            var step1 = new WorkflowStep
            {
                WorkflowVersionId = sickLeaveVersion.Id,
                StepNumber = 1,
                Name = "Employee",
                ApprovalType = ApprovalType.RoleBased,
                TargetRoleId = empRole?.Id.ToString(),
                Sequence = 1,
                IsMandatory = true,
                SLAHours = 24,
                EmailNotification = true,
                RejectAllowed = true,
                ReturnAllowed = true,
                DelegateAllowed = true
            };
            var step2 = new WorkflowStep
            {
                WorkflowVersionId = sickLeaveVersion.Id,
                StepNumber = 2,
                Name = "HR Manager",
                ApprovalType = ApprovalType.SpecificUser,
                TargetUserId = kaushalya?.Id.ToString(),
                Sequence = 2,
                IsMandatory = true,
                SLAHours = 24,
                EmailNotification = true,
                RejectAllowed = true,
                ReturnAllowed = true,
                DelegateAllowed = true
            };
            var step3 = new WorkflowStep
            {
                WorkflowVersionId = sickLeaveVersion.Id,
                StepNumber = 3,
                Name = "Reporting Manager",
                ApprovalType = ApprovalType.ManagerHierarchy,
                Sequence = 3,
                IsMandatory = true,
                SLAHours = 24,
                EmailNotification = true,
                RejectAllowed = true,
                ReturnAllowed = true,
                DelegateAllowed = true
            };
            var step4 = new WorkflowStep
            {
                WorkflowVersionId = sickLeaveVersion.Id,
                StepNumber = 4,
                Name = "BU Head",
                ApprovalType = ApprovalType.DepartmentHead,
                Sequence = 4,
                IsMandatory = true,
                SLAHours = 24,
                EmailNotification = true,
                RejectAllowed = true,
                ReturnAllowed = true,
                DelegateAllowed = true
            };
            var step5 = new WorkflowStep
            {
                WorkflowVersionId = sickLeaveVersion.Id,
                StepNumber = 5,
                Name = "Finance",
                ApprovalType = ApprovalType.SpecificUser,
                TargetUserId = ashhar?.Id.ToString(),
                Sequence = 5,
                IsMandatory = true,
                SLAHours = 24,
                EmailNotification = true,
                RejectAllowed = true,
                ReturnAllowed = true,
                DelegateAllowed = true
            };

            await context.WorkflowSteps.AddRangeAsync(step1, step2, step3, step4, step5);
            await context.SaveChangesAsync();

            var sickForm = new WorkflowForm
            {
                WorkflowVersionId = sickLeaveVersion.Id,
                Name = "Sick Leave Request Form",
                Description = "Standard employee sick leave application form with medical certificate attachment."
            };
            await context.WorkflowForms.AddAsync(sickForm);
            await context.SaveChangesAsync();

            await context.WorkflowFields.AddRangeAsync(
                new WorkflowField { FormId = sickForm.Id, Name = "LeaveType", Label = "Leave Type", Type = FieldType.TextBox, IsRequired = true, Order = 1 },
                new WorkflowField { FormId = sickForm.Id, Name = "StartDate", Label = "Start Date", Type = FieldType.Date, IsRequired = true, Order = 2 },
                new WorkflowField { FormId = sickForm.Id, Name = "EndDate", Label = "End Date", Type = FieldType.Date, IsRequired = true, Order = 3 },
                new WorkflowField { FormId = sickForm.Id, Name = "TotalDays", Label = "Total Number of Days", Type = FieldType.Number, IsRequired = true, Order = 4 },
                new WorkflowField { FormId = sickForm.Id, Name = "Reason", Label = "Medical Reason / Symptoms", Type = FieldType.TextArea, IsRequired = false, Order = 5 },
                new WorkflowField { FormId = sickForm.Id, Name = "MedicalCertificate", Label = "Medical Certificate / Sick Report", Type = FieldType.Attachment, IsRequired = false, Order = 6 }
            );
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
