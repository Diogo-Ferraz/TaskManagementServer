namespace TaskManagement.Auth.Features.Users.Models
{
    public class UserListResponse
    {
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<UserSummaryDto> Items { get; set; } = [];
    }
}
