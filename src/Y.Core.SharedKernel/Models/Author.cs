namespace Y.Core.SharedKernel.Models;
public record Author
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}
