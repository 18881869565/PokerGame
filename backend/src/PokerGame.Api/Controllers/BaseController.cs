using Microsoft.AspNetCore.Mvc;

namespace PokerGame.Api.Controllers;

/// <summary>
/// 基础控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    protected long CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    /// <summary>
    /// 返回成功结果
    /// </summary>
    protected IActionResult Success<T>(T data, string message = "操作成功")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// 返回成功结果（无数据）
    /// </summary>
    protected IActionResult Success(string message = "操作成功")
    {
        return Ok(new ApiResponse
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// 返回失败结果
    /// </summary>
    protected IActionResult Fail(string message, int code = 400)
    {
        return BadRequest(new ApiResponse
        {
            Success = false,
            Message = message,
            Code = code
        });
    }
}

/// <summary>
/// API 响应模型
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Code { get; set; } = 200;
}

/// <summary>
/// API 响应模型（带数据）
/// </summary>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}
