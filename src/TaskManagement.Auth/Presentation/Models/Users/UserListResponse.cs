namespace TaskManagement.Auth.Presentation.Models.Users
{
    public class UserListResponse
    {
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<UserSummaryDto> Items { get; set; } = [];
    }
}
