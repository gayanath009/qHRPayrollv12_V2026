using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorkflowEngine.Application.DTOs;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.API.Controllers;

[Authorize]
public class AdminController : ApiControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public AdminController(WorkflowDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        await WorkflowDbContextSeed.SeedSampleDataAsync(_context);
        return Ok(new { Message = "Database sample data seed completed successfully." });
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        await SyncQhrmEmployeesAsync();

        var users = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Include(u => u.Designation)
            .Where(u => u.IsActive && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    [HttpPost("sync-qhrm")]
    [HttpGet("sync-qhrm")]
    public async Task<IActionResult> TriggerSyncQhrm()
    {
        await SyncQhrmEmployeesAsync();
        var users = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Include(u => u.Designation)
            .Where(u => !u.IsDeleted && u.IsActive && u.EmpSysId != null)
            .ToListAsync();
        return Ok(new { message = $"Successfully synchronized {users.Count} active employees from qHRM.", count = users.Count });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateOrSyncUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username is required." });

        User? existingUser = null;
        if (request.EmpSysId.HasValue && request.EmpSysId.Value > 0)
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.EmpSysId == request.EmpSysId.Value && !u.IsDeleted);
        }
        if (existingUser == null && !string.IsNullOrWhiteSpace(request.Username))
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower() && !u.IsDeleted);
        }
        if (existingUser == null && !string.IsNullOrWhiteSpace(request.Email))
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted);
        }

        if (existingUser != null)
        {
            if (request.EmpSysId.HasValue) existingUser.EmpSysId = request.EmpSysId.Value;
            if (!string.IsNullOrEmpty(request.FullName)) existingUser.FullName = request.FullName;
            if (!string.IsNullOrEmpty(request.Email)) existingUser.Email = request.Email;
            existingUser.IsActive = true;
            existingUser.IsDeleted = false;
            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
            return Ok(new { id = existingUser.Id, empSysId = existingUser.EmpSysId, username = existingUser.Username, fullName = existingUser.FullName, email = existingUser.Email });
        }

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Employee")
                       ?? await _context.Roles.FirstOrDefaultAsync();

        var newUser = new User
        {
            EmpSysId = request.EmpSysId,
            Username = request.Username,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username : request.FullName,
            Email = string.IsNullOrWhiteSpace(request.Email) ? $"{request.Username}@company.com" : request.Email,
            PasswordHash = "SYNCED_USER",
            RoleId = defaultRole?.Id,
            IsActive = true
        };

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();

        return Ok(new { id = newUser.Id, empSysId = newUser.EmpSysId, username = newUser.Username, fullName = newUser.FullName, email = newUser.Email });
    }


    private async Task SyncQhrmEmployeesAsync()
    {
        try
        {
            // Ensure EmpSysId column exists in Users table
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'EmpSysId') " +
                    "ALTER TABLE Users ADD EmpSysId INT NULL;");
            }
            catch { }

            var qhrConn = _configuration.GetConnectionString("qHRconString")
                ?? _configuration.GetSection("ConnectionStrings")["qHRconString"]
                ?? _configuration.GetSection("connectionstrings")["qhrconstring"];

            if (string.IsNullOrEmpty(qhrConn)) return;

            using var conn = new SqlConnection(qhrConn);
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    e.Emp_Sys_Id,
                    ISNULL(u.Usr_Name, ISNULL(e.Emp_Id, CAST(e.Emp_Sys_Id AS NVARCHAR(20)))) AS Username,
                    LTRIM(RTRIM(CONCAT(ISNULL(e.First_Name, ''), ' ', ISNULL(e.Mdl_Name, ''), ' ', ISNULL(e.Last_Name, '')))) AS Full_Name,
                    ISNULL(j.Ofc_Email, ISNULL(e.Psn_Email, 'emp' + CAST(e.Emp_Sys_Id AS NVARCHAR(20)) + '@company.com')) AS Email,
                    d.Name_Eng AS Dept_Name,
                    dsg.Name_Eng AS Desg_Name,
                    g.Name AS Role_Name,
                    j.Rpt_Mng_Id
                FROM HR_EmpGenInfo e
                LEFT JOIN ADM_User u ON e.Emp_Sys_Id = u.Emp_Sys_Id AND u.Rec_Stat = 1
                LEFT JOIN ADM_UserGroup g ON u.Grp_Id = g.Grp_Id
                LEFT JOIN (
                    SELECT Emp_Sys_Id, Dept_Id, Desig_Id, Ofc_Email, Rpt_Mng_Id,
                           ROW_NUMBER() OVER(PARTITION BY Emp_Sys_Id ORDER BY St_Date DESC) as rn
                    FROM HR_EmpJobAssign
                    WHERE Wk_Stat = 1
                ) j ON e.Emp_Sys_Id = j.Emp_Sys_Id AND j.rn = 1
                LEFT JOIN ORG_Department d ON j.Dept_Id = d.Dept_Id
                LEFT JOIN ORG_Designation dsg ON j.Desig_Id = dsg.Desig_Id
                WHERE e.Curr_Stat = 0";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var existingUsers = await _context.Users.ToListAsync();
            var allRoles = await _context.Roles.ToListAsync();
            var defaultRole = allRoles.FirstOrDefault(r => r.Name == "Employee") ?? allRoles.FirstOrDefault();
            var depts = await _context.Departments.ToListAsync();
            var desgs = await _context.Designations.ToListAsync();
            var managerMappings = new List<(int empSysId, int? rptMngId)>();
            var activeEmpSysIds = new HashSet<int>();

            // Purge dummy demo seed users if we have real qHRM employees (keeping admin for UI login)
            var dummyDemoUsernames = new[] { "it_hod", "employee", "ceo", "fin_mgr", "proc_mgr", "hr_mgr", "devid", "marcus", "robert" };
            var dummyUsers = existingUsers.Where(u => dummyDemoUsernames.Contains(u.Username.ToLower()) || u.FullName.Contains("Gilmour") || u.FullName.Contains("Sterling") || u.FullName.Contains("Vance")).ToList();
            if (dummyUsers.Any())
            {
                _context.Users.RemoveRange(dummyUsers);
                foreach (var d in dummyUsers) existingUsers.Remove(d);
            }

            while (await reader.ReadAsync())
            {
                int empSysId = reader.GetInt32(0);
                activeEmpSysIds.Add(empSysId);

                string username = reader.GetString(1).Trim();
                string fullName = System.Text.RegularExpressions.Regex.Replace(reader.GetString(2).Trim(), @"\s+", " ");
                if (string.IsNullOrWhiteSpace(fullName)) fullName = "Employee " + username;
                string email = reader.GetString(3).Trim();
                string? deptName = reader.IsDBNull(4) ? null : reader.GetString(4).Trim();
                string? desgName = reader.IsDBNull(5) ? null : reader.GetString(5).Trim();
                string? roleName = reader.IsDBNull(6) ? null : reader.GetString(6).Trim();
                int? rptMngId = reader.IsDBNull(7) ? null : reader.GetInt32(7);
                managerMappings.Add((empSysId, rptMngId));

                var user = existingUsers.FirstOrDefault(u => u.EmpSysId.HasValue && u.EmpSysId.Value == empSysId)
                        ?? existingUsers.FirstOrDefault(u => !u.EmpSysId.HasValue && string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase))
                        ?? existingUsers.FirstOrDefault(u => !u.EmpSysId.HasValue && !string.IsNullOrWhiteSpace(email) && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

                Department? dept = null;
                if (!string.IsNullOrEmpty(deptName))
                {
                    dept = depts.FirstOrDefault(d => string.Equals(d.Name, deptName, StringComparison.OrdinalIgnoreCase));
                    if (dept == null)
                    {
                        dept = new Department { Name = deptName, Code = deptName.Length > 6 ? deptName.Substring(0, 6).ToUpper() : deptName.ToUpper() };
                        _context.Departments.Add(dept);
                        depts.Add(dept);
                    }
                }

                Designation? desg = null;
                if (!string.IsNullOrEmpty(desgName))
                {
                    desg = desgs.FirstOrDefault(d => string.Equals(d.Name, desgName, StringComparison.OrdinalIgnoreCase));
                    if (desg == null)
                    {
                        desg = new Designation { Name = desgName, Code = desgName.Length > 6 ? desgName.Substring(0, 6).ToUpper() : desgName.ToUpper() };
                        _context.Designations.Add(desg);
                        desgs.Add(desg);
                    }
                }

                Role? userRole = null;
                if (!string.IsNullOrEmpty(roleName))
                {
                    userRole = allRoles.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Code, roleName, StringComparison.OrdinalIgnoreCase));
                }

                if (user != null)
                {
                    user.EmpSysId = empSysId;
                    user.Username = username;
                    user.FullName = fullName;
                    user.Email = email;
                    user.IsActive = true;
                    user.IsDeleted = false;
                    if (dept != null) user.DepartmentId = dept.Id;
                    if (desg != null) user.DesignationId = desg.Id;
                    if (userRole != null) user.RoleId = userRole.Id;
                    _context.Users.Update(user);
                }
                else
                {
                    var newUser = new User
                    {
                        EmpSysId = empSysId,
                        Username = username,
                        FullName = fullName,
                        Email = email,
                        PasswordHash = "SYNCED_FROM_QHRM",
                        RoleId = userRole?.Id ?? defaultRole?.Id,
                        Department = dept,
                        Designation = desg,
                        IsActive = true
                    };

                    _context.Users.Add(newUser);
                    existingUsers.Add(newUser);
                }
            }

            // Deactivate any employees who are no longer active in qHRM (Curr_Stat != 0 or terminated)
            var inactiveUsers = existingUsers.Where(u => u.EmpSysId.HasValue && !activeEmpSysIds.Contains(u.EmpSysId.Value)).ToList();
            foreach (var inUser in inactiveUsers)
            {
                inUser.IsActive = false;
                inUser.IsDeleted = true;
                _context.Users.Update(inUser);
            }

            await _context.SaveChangesAsync();

            // Second pass: Map ManagerId based on Rpt_Mng_Id
            var allSyncedUsers = await _context.Users.ToListAsync();
            foreach (var mapping in managerMappings)
            {
                if (mapping.rptMngId.HasValue && mapping.rptMngId.Value > 0)
                {
                    var empUser = allSyncedUsers.FirstOrDefault(u => u.EmpSysId == mapping.empSysId);
                    var mgrUser = allSyncedUsers.FirstOrDefault(u => u.EmpSysId == mapping.rptMngId.Value);
                    if (empUser != null && mgrUser != null && empUser.ManagerId != mgrUser.Id)
                    {
                        empUser.ManagerId = mgrUser.Id;
                        _context.Users.Update(empUser);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SyncQhrmEmployees Error] {ex.Message}");
        }
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles.Where(r => r.IsActive && !r.IsDeleted).ToListAsync();
        return Ok(roles.Select(r => new { r.Id, r.Name, r.Code }));
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var depts = await _context.Departments.Where(d => d.IsActive && !d.IsDeleted).ToListAsync();
        return Ok(depts.Select(d => new { d.Id, d.Name, d.Code }));
    }
}

