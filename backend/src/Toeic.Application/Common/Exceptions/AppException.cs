namespace Toeic.Application.Common.Exceptions;

/// <summary>
/// Base cho mọi exception nghiệp vụ do Application layer chủ động throw.
/// Api layer (ExceptionHandlingMiddleware) bắt loại này và map sang HTTP status + ProblemDetails,
/// tách biệt với exception hệ thống không mong muốn (500).
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    protected AppException(string message) : base(message) { }
}

/// <summary>400 — input không hợp lệ hoặc vi phạm business rule đơn giản.</summary>
public class ValidationAppException : AppException
{
    public override int StatusCode => 400;
    public IDictionary<string, string[]>? Errors { get; }

    public ValidationAppException(string message) : base(message) { }
    public ValidationAppException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}

/// <summary>401 — sai username/password, token không hợp lệ/hết hạn.</summary>
public class UnauthorizedAppException : AppException
{
    public override int StatusCode => 401;
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>403 — đã xác thực nhưng không đủ quyền (vd Candidate cố tạo tài khoản ADMIN).</summary>
public class ForbiddenAppException : AppException
{
    public override int StatusCode => 403;
    public ForbiddenAppException(string message) : base(message) { }
}

/// <summary>404 — không tìm thấy resource.</summary>
public class NotFoundAppException : AppException
{
    public override int StatusCode => 404;
    public NotFoundAppException(string message) : base(message) { }
}

/// <summary>409 — trùng dữ liệu (username/email đã tồn tại...).</summary>
public class ConflictAppException : AppException
{
    public override int StatusCode => 409;
    public ConflictAppException(string message) : base(message) { }
}
