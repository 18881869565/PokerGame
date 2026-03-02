using PokerGame.Application.DTOs;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;

namespace PokerGame.Application.Interfaces;

/// <summary>
/// 游戏服务接口
/// </summary>
public interface IGameService
{
    /// <summary>
    /// 开始游戏
    /// </summary>
    Task<(bool Success, string Message, GameStateDto? State)> StartGameAsync(long roomId, long userId);

    /// <summary>
    /// 玩家操作
    /// </summary>
    Task<(bool Success, string Message, GameStateDto? State, GameEventType EventType)> PlayerActionAsync(long roomId, long userId, PlayerAction action, long amount = 0);

    /// <summary>
    /// 获取游戏状态
    /// </summary>
    GameStateDto? GetGameState(long roomId);

    /// <summary>
    /// 获取玩家的游戏状态（包含自己的底牌）
    /// </summary>
    GameStateDto? GetGameStateForPlayer(long roomId, long userId);

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
    GameResultDto? GetGameResult(long roomId);
}
