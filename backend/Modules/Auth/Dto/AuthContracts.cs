namespace TicketApi.Modules.Auth.Dto;

public record LoginRequest(string Email, string Password);

public record AdminSessionDto(string Token, string Email);
