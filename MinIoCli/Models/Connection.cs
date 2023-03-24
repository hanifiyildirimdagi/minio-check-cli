namespace MinIoCli.Models;

public record Connection(string Id, string Host, string AccessKey, string SecretKey, bool Secure, string Alias);