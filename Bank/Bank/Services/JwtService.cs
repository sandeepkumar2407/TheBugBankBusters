/*
using Bank.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bank.Services
{
    public class JwtService : IJwtServices
    {
        private readonly JwtSettings jwtSettings;

        public JwtService(IOptions<JwtSettings> _jwtSettings)
        {
            jwtSettings = _jwtSettings.Value;
        }

        public string GenerateToken(string role, int? userId = null, int? empId = null, int? branchId = null)
        {
            Console.WriteLine($"DEBUG: role={role}, userId={userId}, empId={empId}, branchId={branchId}");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role)
            };
            if(userId.HasValue)
            {
                claims.Add(new Claim("UserId", userId.Value.ToString()));
            }
            if(empId.HasValue)
            {
                claims.Add(new Claim("EmpId", empId.Value.ToString()));
            }
            if(branchId.HasValue)
            {
                claims.Add(new Claim("BranchId", branchId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
*/

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bank.Services
{
    public class JwtService : IJwtServices
    {
        private readonly IConfiguration configuration;

        public JwtService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string GenerateToken(string role, int? userId = null, int? empId = null, int? branchId = null)
        {
            var claims = new List<Claim>
            {
                new Claim("role", role)
            };

            if (userId.HasValue)
            {
                claims.Add(new Claim("UserId", userId.Value.ToString()));
            }
                
            if (empId.HasValue)
            {
                claims.Add(new Claim("EmpId", empId.Value.ToString()));
            }
                
            if (branchId.HasValue)
            {
                claims.Add(new Claim("BranchId", branchId.Value.ToString()));
            }
                
            string strKey = configuration["JwtSettings:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["JwtSettings:Issuer"],
                audience: configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["JwtSettings:ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}