using ForaChallenge.Application.Repositories;
using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.Enums;
using ForaChallenge.Domain.ValueObjects;
using ForaChallenge.Infrastructure;
using ForaChallenge.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForaDbContext>();
    db.Database.Migrate();

    var cikRepo = scope.ServiceProvider.GetRequiredService<ICikImportRepository>();

    if (await cikRepo.CountAsync() == 0)
    {
        var path = app.Configuration["ciks:filePath"] ?? Path.Combine(AppContext.BaseDirectory, "Data", "ciks.json");

        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path);
            var ciks = JsonSerializer.Deserialize<int[]>(json) ?? [];
            var now = DateTime.Now;
            var entities = ciks
                .Select(c => new CikImport { Cik = Cik.From(c), Status = CikImportStatus.Pending, CreatedAt = now })
                .ToList();
            await cikRepo.AddRangeAsync(entities);
        }
    }

}

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
