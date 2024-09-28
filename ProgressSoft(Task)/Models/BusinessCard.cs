using System;
using System.Collections.Generic;

namespace ProgressSoft_Task_.Models;

public partial class BusinessCard
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public byte[]? Photo { get; set; }

    public string? Address { get; set; }

    
}
