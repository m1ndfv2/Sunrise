# User clan endpoint

Added a public profile endpoint to fetch clan details for a user:

- `GET /user/{id}/clan`

## Response

Returns `200 OK` with `ClanDetailsResponse` when the user belongs to a clan.

```json
{
  "clan": {
    "id": 1,
    "name": "Sunrise",
    "avatar_url": "https://cdn.sunrise.test/clan.png",
    "description": "Top players only",
    "tag": "SRS",
    "total_pp": 12345.67,
    "created_at": "2026-02-16T12:00:00Z"
  },
  "members": [
    {
      "user": { "id": 1001 },
      "role": "creator",
      "pp": 9000.0
    },
    {
      "user": { "id": 1002 },
      "role": "member",
      "pp": 3345.67
    }
  ]
}
```

## Errors

Returns `404 Not Found` when:

- user does not exist;
- user is not in a clan;
- user's clan cannot be found.

This endpoint is public and does not require authorization.
