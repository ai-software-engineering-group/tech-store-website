﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace STech.Data.Models;

public partial class Role
{
    public string RoleId { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
