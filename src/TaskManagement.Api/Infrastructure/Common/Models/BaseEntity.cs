namespace TaskManagement.Api.Infrastructure.Common.Models
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public string LastModifiedByUserId { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
