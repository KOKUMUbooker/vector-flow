# USER-FLOWS : 
## Auth
### Registration & login
User visits app → lands on login page
Clicks "Sign up" → fills display name, email, password → submits
Server creates account → sends verification email with token link
User clicks link → email verified → redirected to login
User logs in → server sets HttpOnly cookies → redirected to workspace selector
On page reload → client calls GET /auth/me → auth state restored silently

## Auth
### Forgot password
User clicks "Forgot password" → enters email
Server sends reset link with short-lived token (15 min)
User clicks link → enters new password → submits
Server validates token → updates password → invalidates all refresh tokens for that user
User redirected to login

## Workspace
### Create workspace
Logged-in user clicks "Create workspace" → enters name
Server creates workspace → auto-assigns user as Owner via WorkspaceMember
User redirected to new workspace dashboard

## Membership
### Invite a member
Owner/Admin opens workspace settings → Members tab → enters email → sends invite
Server creates Invitation record (Pending) → sends email with invite link
Recipient clicks link → if not registered, prompted to sign up first
Recipient accepts → WorkspaceMember created with Member role → Invitation marked Accepted
Recipient declines → Invitation marked Declined
Admin can cancel a pending invitation before it's acted on

## Workspace
### Manage members
Owner/Admin opens Members tab → sees all members with their roles
Admin can promote a Member to Admin
Owner can demote any Admin back to Member (Admins cannot demote other Admins)
Owner/Admin can remove a Member from the workspace
Owner role cannot be transferred or removed

## Project
### Create & manage project
Owner/Admin clicks "New project" → enters name, description, key prefix (e.g. VF)
Server creates project → IssueCounter starts at 0
User lands on empty Kanban board for the project
Admin can rename or delete the project from project settings

## Issue
### Create issue
User clicks "New issue" on board or list view
Fills title (required), description (markdown), type, priority, assignee, due date, labels
Server increments IssueCounter → assigns key (e.g. VF-43) → creates ActivityLog entry
SignalR broadcasts new issue to all connected clients in that project
Issue appears in Backlog column of Kanban board in real time

## Issue
### Kanban board interaction
User views board → columns: Backlog, Todo, In Progress, In Review, Done
User drags issue card to a different column
Client calls PATCH /api/issues/{id}/status with new status
Server updates status → appends ActivityLog (StatusChanged, from → to)
SignalR broadcasts IssueUpdated event → all connected clients update their board in real time

## Issue
## View & edit issue detail
User clicks issue card → navigates to /projects/{id}/issues/{key}
Sees title, description, status, priority, assignee, labels, due date
Any field can be edited inline → change saved immediately on blur/confirm
Activity log shown chronologically — all changes with actor and timestamp
Comment thread below activity log

## Issue
### Comment on issue
User types comment in markdown editor → submits
Server creates Comment → SignalR broadcasts to all clients on that issue detail page
Author can edit or delete their own comment
Edited comments show "edited" indicator

## Label
### Manage labels
Admin opens project settings → Labels tab
Creates label with name and hex color
Labels are scoped to a project — not shared across projects
Labels can be assigned to issues from the issue detail or create issue form
Deleting a label removes it from all issues silently

## Auth
### Logout
User clicks logout → client calls POST /auth/logout
Server revokes refresh token in DB → clears both cookies
Client calls NotifyLoggedOut() on AuthStateProvider
User redirected to login page
