# Clan delete endpoint and admin command

## Delete own clan (creator only)

- `DELETE /clan`
- Authorization: required
- Access: clan creator only

### Responses

- `200 OK` — clan deleted.
- `400 Bad Request` — user is not in clan.
- `403 Forbidden` — insufficient privileges (not creator) or restricted user.
- `404 Not Found` — user or clan not found.

## Delete any clan (administration command)

There is no admin HTTP endpoint for deleting arbitrary clans.
Instead, administrators can use chat command:

- `!deleteclan <clanId>`
- Required privileges: `Admin`
