using FusionComms.DTOs;
using FusionComms.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface IUserService
    {
        public SignInManager<User> SignInManager { get; }
        public UserManager<User> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }

        ValueTask<User> CheckUser(string userId);
        ValueTask<AuthResponse> AuthorizeAsync(LoginModel login);

        ValueTask<bool> DeleteUser(User user);

        ValueTask<bool> AddUser(User user, string password = null);

        ValueTask<List<User>> GetUsers();

        ValueTask<bool> AddClaimsToRole(ClaimRequest[] requests);

        ValueTask<bool> AddClaimsToUser(ClaimRequestForUser[] requests);

        ValueTask<bool> RemoveClaimsFromUser(ClaimRequestForUser[] requests);

        ValueTask<List<string>> GetClaimsByUser(User user);

        ValueTask<List<string>> GetClaimsByRole(IdentityRole role);
    }
    public class UserService : IUserService
    {
        private readonly IRepository Repository;

        public UserManager<User> UserManager { get; }
        public SignInManager<User> SignInManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }

        private readonly JWT _jwt;

        public UserService(IRepository repository, UserManager<User> userManager,
            SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager,
            IOptions<JWT> options)
        {
            Repository = repository;
            UserManager = userManager;
            SignInManager = signInManager;
            RoleManager = roleManager;
            _jwt = options.Value;
        }

        public async ValueTask<AuthResponse> AuthorizeAsync(LoginModel login)
        {
            var user = await UserManager.FindByEmailAsync(login.UserName) ?? await UserManager.FindByNameAsync(login.UserName);

            if (user is null)
            {
                throw new KeyNotFoundException("User account not found.");
            }

            return await GenerateTokenAsync(user);
        }

        public async ValueTask<AuthResponse> GenerateTokenAsync(User user, DateTime? expiry = default)
        {
            if (expiry is null)
            {
                expiry = DateTime.UtcNow.AddYears(1);
            }

            var key = Encoding.ASCII.GetBytes(_jwt.SigningKey);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleName)
            };

            var userClaims = await UserManager.GetClaimsAsync(user);

            //if (userClaims?.Count > 0)
            //{
            //    claims.AddRange(userClaims.Select(claim => new Claim(ClaimTypes.Role, claim.Value)));
            //}

            var jwtToken = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,
                IssuedAt = DateTime.UtcNow,
                Expires = expiry,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(jwtToken);

            return new AuthResponse
            {
                Token = tokenHandler.WriteToken(token),
                Claims = userClaims.Select(c => c.Value),
                UserId = user.Id,
            };
        }

        public async ValueTask<bool> DeleteUser(User user)
        {
            if (user is null)
                return false;

            var result = await UserManager.DeleteAsync(user);

            return result.Succeeded;
        }

        public async ValueTask<bool> AddUser(User user, string password = null)
        {
            if (user is null)
                return false;
            
            if (password is null)
            {
                var result = await UserManager.CreateAsync(user);

                return result.Succeeded;
            }
            var resultWithPassword = await UserManager.CreateAsync(user, password);

            return resultWithPassword.Succeeded;
        }

        public async ValueTask<bool> AddClaimsToRole(ClaimRequest[] requests)
        {
            foreach (var request in requests)
            {
                var role = await RoleManager.FindByNameAsync(request.RoleName);

                if (role is null)
                {
                    throw new Exception($"Role [{role}] does not exist.");
                }

                var claims = await RoleManager.GetClaimsAsync(role);
                foreach (var claim in request.Claims)
                {
                    if (!claims.Any(c => c.Value == claim))
                    {
                        await RoleManager.AddClaimAsync(role, new Claim(ClaimTypes.Actor, claim));


                        //Repository.DbContext.RoleClaims.Add(new IdentityRoleClaim<string>
                        //{
                        //    ClaimType = ClaimTypes.Actor,
                        //    ClaimValue = claim,
                        //    RoleId = role.Id,
                        //});

                        //await userService.RoleManager.AddClaimAsync(role, new Claim(ClaimTypes.Actor, claim));
                    }
                }
            }

            return true;
        }

        public async ValueTask<bool> AddClaimsToUser(ClaimRequestForUser[] requests)
        {
            foreach(var request in requests)
            {
                var user = await UserManager.FindByNameAsync(request.UserName);

                if(user is null)
                {
                    throw new Exception($"User [{request.UserName}] does not exist");
                }

                var userClaims = await UserManager.GetClaimsAsync(user);
                foreach(var claim in request.Claims)
                {
                    if(!userClaims.Any(c => c.Value == claim))
                    {
                        await UserManager.AddClaimAsync(user, new Claim(ClaimTypes.Actor, claim));
                    }
                }
            }
            return true;
        }

        public async ValueTask<bool> RemoveClaimsFromUser(ClaimRequestForUser[] requests)
        {
            foreach (var request in requests)
            {
                var user = await UserManager.FindByNameAsync(request.UserName);

                if (user is null)
                {
                    throw new Exception($"User [{request.UserName}] does not exist");
                }

                var userClaims = await UserManager.GetClaimsAsync(user);
                foreach (var claim in request.Claims)
                {
                    if (!userClaims.Any(c => c.Value == claim))
                    {
                        await UserManager.RemoveClaimAsync(user, new Claim(ClaimTypes.Actor, claim));
                    }
                }
            }
            return true;
        }



        public async ValueTask<List<string>> GetClaimsByUser(User user)
        {
            return (await UserManager.GetClaimsAsync(user)).Select(c => c.Value).ToList();
        }

        public async ValueTask<List<string>> GetClaimsByRole(IdentityRole role)
        {
            return (await RoleManager.GetClaimsAsync(role)).Select(c => c.Value).ToList();
        }

        public async ValueTask<User> CheckUser(string userId)
        {
            return await UserManager.FindByIdAsync(userId);
        }

        public async ValueTask<List<User>> GetUsers()
        {
            return await UserManager.Users.ToListAsync();
        }
    }
}