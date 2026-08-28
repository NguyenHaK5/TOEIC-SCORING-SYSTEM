using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Common.Exceptions;

namespace Toeic.Api.Controllers;

/// <summary>Base controller dùng chung cho mọi controller cần đọc UserId hiện tại từ JWT claim.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (sub is null || !Guid.TryParse(sub, out var userId))
            {
                throw new UnauthorizedAppException("Access token không hợp lệ.");
            }
            return userId;
        }
    }
}
