﻿using System;
using System.Collections.Generic;

namespace STech.Data.Models;

public partial class Customer
{
    public string CustomerId { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}