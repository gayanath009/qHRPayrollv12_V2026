using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Common;

namespace WorkflowEngine.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Designation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User : BaseEntity
{
    public int? EmpSysId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    
    public Guid? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    
    public Guid? RoleId { get; set; }
    public Role? Role { get; set; }
    
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }
    
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public ICollection<User> DirectReports { get; set; } = new List<User>();
}
