﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using STech.Data.Models;
using STech.Data.ViewModels;
using STech.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STech.Services.Services
{
    public class UserService : IUserService
    {
        private readonly StechDbContext _context;
        public UserService(StechDbContext context) => _context = context;

        public async Task<User?> GetUser(LoginVM login)
        {
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == login.UserName);

            if(user != null && user.PasswordHash == login.Password.HashPasswordMD5(user.RandomKey))
            {
                return user;
            }

            return null;
        }

        public async Task<User?> GetUserById(string id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<bool> IsExist(string username)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user != null;
        }

        public async Task<bool> IsEmailExist(string email)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null;
        }

        public async Task<bool> CreateUser(RegisterVM register)
        {
            if(register.RegPassword != register.ConfirmPassword) return false;

            string randomKey = UserUtils.GenerateRandomString(20);

            Role? role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == "user");

            if(role == null || role.RoleId == null) return false;

            User user = new User()
            {
                UserId = UserUtils.GenerateRandomId(40),
                Username = register.RegUserName,
                PasswordHash = register.RegPassword.HashPasswordMD5(randomKey),
                Email = register.Email,
                RandomKey = randomKey,
                IsActive = true,
                RoleId = role.RoleId,
            };

            await _context.Users.AddAsync(user);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}