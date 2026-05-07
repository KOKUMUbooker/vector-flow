using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;

namespace VectorFlow.Api.Hubs;

/// <summary>
/// Real-time hub scoped to a single project.
/// Clients join a project group on entering a project board and leave on exit.
/// All issue and comment mutations broadcast to the group so every connected
/// client sees changes without polling.
///
/// Auth: JWT cookie is validated by the JwtBearer handler — the same
/// OnMessageReceived hook that reads the cookie for REST requests also
/// applies to SignalR's HTTP handshake.
/// </summary>
[Authorize]
public class ProjectHub(AppDbContext db) : Hub
{
    // ── Client → Server ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by the client when it navigates to a project board.
    /// Adds the connection to a SignalR group keyed by project ID.
    /// Validates workspace membership before admitting the connection.
    /// </summary>
    public async Task JoinProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var projectGuid))
        {
            await Clients.Caller.SendAsync("Error", "Invalid project ID.");
            return;
        }

        var userId = GetUserId();

        if (!await IsMemberAsync(projectGuid, userId))
        {
            await Clients.Caller.SendAsync("Error", "You are not a member of this workspace.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupKey(projectGuid));
    }

    /// <summary>
    /// Called by the client when navigating away from a project board.
    /// Removes the connection from the group so it stops receiving broadcasts.
    /// </summary>
    public async Task LeaveProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var projectGuid)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupKey(projectGuid));
    }

    // ── Disconnect cleanup ────────────────────────────────────────────────────

    /// <summary>
    /// SignalR removes connections from all groups automatically on disconnect,
    /// so no manual cleanup is needed here. This override exists for logging
    /// or future presence tracking.
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception) =>
        base.OnDisconnectedAsync(exception);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetUserId() =>
        Context.UserIdentifier
        ?? throw new HubException("User not authenticated.");

    private async Task<bool> IsMemberAsync(Guid projectId, string userId)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project is null) return false;

        return await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
    }

    /// <summary>
    /// Consistent group key format used by the hub and all broadcaster calls.
    /// Centralised here so a typo in one place doesn't cause silent mismatches.
    /// </summary>
    public static string GroupKey(Guid projectId) => $"project:{projectId}";
}