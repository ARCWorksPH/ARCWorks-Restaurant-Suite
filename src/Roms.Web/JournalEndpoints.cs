using Roms.Application;
using Roms.Domain;
using Microsoft.AspNetCore.Antiforgery;

namespace Roms.Web;

public static class JournalEndpoints
{
    public static WebApplication MapJournalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/private-journal")
            .RequireAuthorization();

        group.MapGet("/key-envelope", async (HttpContext context, IJournalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetKeyEnvelopeAsync(context.User, cancellationToken)));

        group.MapPut("/key-envelope", async (JournalKeyEnvelopeWrite value, HttpContext context,
            IJournalService service, CancellationToken cancellationToken) =>
            await ExecuteAsync(async () =>
            {
                await service.SaveKeyEnvelopeAsync(context.User, value, cancellationToken);
                return Results.NoContent();
            })).WithMetadata(new RequireAntiforgeryTokenAttribute());

        group.MapGet("/entries", async (bool? deleted, HttpContext context, IJournalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetMineAsync(context.User, deleted == true, cancellationToken)));

        group.MapPost("/entries", async (JournalEntryWrite value, HttpContext context, IJournalService service,
            CancellationToken cancellationToken) =>
            await ExecuteAsync(async () => Results.Ok(new
            {
                id = await service.CreateAsync(context.User, value, cancellationToken)
            }))).WithMetadata(new RequireAntiforgeryTokenAttribute());

        group.MapPut("/entries/{id:guid}", async (Guid id, JournalEntryWrite value, HttpContext context,
            IJournalService service, CancellationToken cancellationToken) =>
            await ExecuteAsync(async () =>
            {
                await service.UpdateAsync(context.User, id, value, cancellationToken);
                return Results.NoContent();
            })).WithMetadata(new RequireAntiforgeryTokenAttribute());

        group.MapPost("/entries/{id:guid}/delete", async (Guid id, JournalVersionWrite value, HttpContext context,
            IJournalService service, CancellationToken cancellationToken) =>
            await ExecuteAsync(async () =>
            {
                await service.SoftDeleteAsync(context.User, id, value.ExpectedVersion, cancellationToken);
                return Results.NoContent();
            })).WithMetadata(new RequireAntiforgeryTokenAttribute());

        group.MapPost("/entries/{id:guid}/restore", async (Guid id, JournalVersionWrite value, HttpContext context,
            IJournalService service, CancellationToken cancellationToken) =>
            await ExecuteAsync(async () =>
            {
                await service.RestoreAsync(context.User, id, value.ExpectedVersion, cancellationToken);
                return Results.NoContent();
            })).WithMetadata(new RequireAntiforgeryTokenAttribute());

        group.MapDelete("/entries/{id:guid}", async (Guid id, long expectedVersion, HttpContext context,
            IJournalService service, CancellationToken cancellationToken) =>
            await ExecuteAsync(async () =>
            {
                await service.PermanentlyDiscardAsync(context.User, id, expectedVersion, cancellationToken);
                return Results.NoContent();
            })).WithMetadata(new RequireAntiforgeryTokenAttribute());

        return app;
    }

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (DomainException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

public sealed record JournalVersionWrite(long ExpectedVersion);
