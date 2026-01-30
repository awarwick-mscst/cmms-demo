namespace CMMS.Shared.DTOs;

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int PartCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
