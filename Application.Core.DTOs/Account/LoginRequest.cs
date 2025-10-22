﻿namespace Application.Core.DTOs.Account;

public sealed record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}