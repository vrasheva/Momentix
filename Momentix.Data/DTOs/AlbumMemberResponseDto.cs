namespace Momentix.Data.DTOs;

public class AlbumMemberResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool CanUpload { get; set; }
    public bool IsOwner { get; set; }
    public string Initials => FullName.Length >= 2
        ? $"{FullName[0]}{FullName.Split(' ').LastOrDefault()?[0]}"
        : FullName.Length == 1 ? FullName[0].ToString() : "?";
    public string RoleText => IsOwner ? "Собственик" :
                              CanUpload ? "Може да качва" : "Само преглед";
}