using PokerGame.Application.DTOs;
using PokerGame.Domain.Enums;

namespace PokerGame.Application.Interfaces;

/// <summary>
/// 扎金花游戏服务接口
/// </summary>
public interface IZjhGameService
{
    /// <summary>
    /// 开始游戏
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State)> StartGameAsync(long roomId, long userId);

    /// <summary>
    /// 玩家看牌
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State)> LookCardsAsync(long roomId, long userId);

    /// <summary>
    /// 玩家下注
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State)> BetAsync(long roomId, long userId);

    /// <summary>
    /// 玩家加注
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State)> RaiseAsync(long roomId, long userId, long newBetAmount);

    /// <summary>
    /// 玩家比牌
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State, ZjhGameResultDto? Result, ZjhCompareResultDto? CompareResult)> CompareAsync(long roomId, long userId, long targetUserId);

    /// <summary>
    /// 玩家弃牌
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State, ZjhGameResultDto? Result)> FoldAsync(long roomId, long userId);

    /// <summary>
    /// 玩家全押
    /// </summary>
    Task<(bool Success, string Message, ZjhGameStateDto? State)> AllInAsync(long roomId, long userId);

    /// <summary>
    /// 获取游戏状态
    /// </summary>
    ZjhGameStateDto? GetGameState(long roomId);

    /// <summary>
    /// 获取玩家的游戏状态（包含自己的手牌）
    /// </summary>
    ZjhGameStateDto? GetGameStateForPlayer(long roomId, long userId);

    /// <summary>
    /// 获取玩家可用操作
    /// </summary>
    ZjhAvailableActionsDto? GetAvailableActions(long roomId, long userId);

    /// <summary>
    /// 玩家断开连接处理
    /// </summary>
    Task HandlePlayerDisconnectAsync(long roomId, long userId);

    /// <summary>
    /// 检查房间是否有正在进行的游戏
    /// </summary>
    bool HasActiveGame(long roomId);

    /// <summary>
    /// 获取游戏结果
    /// </summary>
    ZjhGameResultDto? GetGameResult(long roomId);

    /// <summary>
    /// 结束游戏并重置房间状态
    /// </summary>
    Task EndGameAndResetRoomAsync(long roomId);
}
