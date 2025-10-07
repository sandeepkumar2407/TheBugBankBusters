using Bank.Models;
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
                options.AddPolicy("AllowAngularApp", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers();

            builder.Services.AddDbContext<BankDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("myconn"))
            );

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("AllowAngularApp");

            app.UseHttpsRedirection();

            app.MapControllers();

            app.Run();
        }
    }
}
