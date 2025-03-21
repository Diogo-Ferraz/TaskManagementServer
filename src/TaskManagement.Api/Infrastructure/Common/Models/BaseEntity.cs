namespace TaskManagement.Api.Infrastructure.Common.Models
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
