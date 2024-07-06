﻿using System;
using System.Collections.Generic;

namespace STech.Data.Models;

public partial class UserAddress
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string RecipientName { get; set; } = null!;

    public string? RecipientPhone { get; set; }

    public string Address { get; set; } = null!;

    public string Ward { get; set; } = null!;

    public string District { get; set; } = null!;

    public string Province { get; set; } = null!;

    public bool? IsDefault { get; set; }

    public virtual User User { get; set; } = null!;
}