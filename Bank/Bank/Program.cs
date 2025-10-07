
using Bank.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Bank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                //options.AddPolicy("AllowAll",
                //    builder => 
                //        builder
                //        .AllowAnyOrigin()
                //        .AllowAnyHeader()
                //        .AllowAnyMethod()
                //        .AllowCredentials()
                //);

                options.AddPolicy("AllowSpecificOrigin",
                builder =>
                {
                    builder.WithOrigins("http://localhost:4200") 
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); 
                });
            });



            // Add services to the container.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //Add Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.LoginPath = "/api/Login"; // Redirect unauthenticated requests
                            options.AccessDeniedPath = "/api/Login/AccessDenied";
                            options.Cookie.HttpOnly = true;
                            options.Cookie.SameSite = SameSiteMode.None; //  for Angular
                            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //  HTTPS only
                        });


            //Add Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("BankAdmin", policy => policy.RequireRole("BankAdmin"));
                options.AddPolicy("BranchManager", policy => policy.RequireRole("BranchManager"));
                options.AddPolicy("Staff", policy => policy.RequireRole("Staff"));
            });

            builder.Services.AddControllers();
            builder.Services.AddDbContext<BankDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("myconn")));
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseSession();

            app.UseHttpsRedirection();

            app.UseCors("AllowSpecificOrigin");

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}