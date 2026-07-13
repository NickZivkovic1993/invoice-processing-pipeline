using LedgerFlow.Api;
using LedgerFlow.Infrastructure.Persistence;
using LedgerFlow.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LedgerFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

var queue = app.MapGroup("/api/exceptions").WithTags("Exception queue");

// The review backlog: invoices that could not auto-post, newest first.
queue.MapGet("/", async (LedgerFlowDbContext db, string? status) =>
{
    var wanted = ParseStatus(status) ?? InvoiceStatus.NeedsReview;
    var items = await db.Invoices
        .Include(i => i.Exceptions)
        .Where(i => i.Status == wanted)
        .OrderByDescending(i => i.ReceivedAt)
        .ToListAsync();
    return Results.Ok(items.Select(ExceptionQueueItem.From));
});

queue.MapGet("/{id:guid}", async (Guid id, LedgerFlowDbContext db) =>
{
    var record = await db.Invoices.Include(i => i.Exceptions).FirstOrDefaultAsync(i => i.Id == id);
    return record is null ? Results.NotFound() : Results.Ok(ExceptionQueueItem.From(record));
});

// Reviewer overrides the match and releases the invoice to the ERP.
queue.MapPost("/{id:guid}/approve", (Guid id, ResolveRequest _, LedgerFlowDbContext db) =>
    ResolveAsync(id, InvoiceStatus.Posted, db));

// Reviewer rejects the invoice; it will not be posted.
queue.MapPost("/{id:guid}/reject", (Guid id, ResolveRequest _, LedgerFlowDbContext db) =>
    ResolveAsync(id, InvoiceStatus.Rejected, db));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static async Task<IResult> ResolveAsync(Guid id, InvoiceStatus status, LedgerFlowDbContext db)
{
    var record = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id);
    if (record is null)
    {
        return Results.NotFound();
    }

    if (record.Status != InvoiceStatus.NeedsReview)
    {
        return Results.Conflict(new { message = $"Invoice is already {record.Status}." });
    }

    record.Status = status;
    record.ResolvedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { record.Id, status = record.Status.ToString() });
}

static InvoiceStatus? ParseStatus(string? status) =>
    Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;

// Exposed so the API can be driven from integration tests / the SPA dev proxy.
public partial class Program;
