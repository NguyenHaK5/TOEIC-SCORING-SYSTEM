namespace Toeic.Domain.Common;

/// <summary>
/// Base class cho các entity có Id kiểu Guid + audit timestamps.
/// Không map trực tiếp — mỗi entity kế thừa và tự khai báo thêm field riêng.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
