# 炸金花比牌和结算展示优化设计

## 背景

炸金花游戏中存在两个问题需要修复：

1. **比牌输家看牌**：多人对战时进行比牌，输家如果是"闷牌"状态（没看过牌），输了之后不知道自己是什么牌
2. **结算展示**：游戏结束时结算弹窗只显示赢家的牌，应该显示所有参与比牌的玩家的牌

## 需求确认

### 问题1：比牌输家看牌
- 比牌后，如果输家没看过牌，需要给他发送自己的牌信息
- 只给输家自己看，不给其他玩家看

### 问题2：结算展示
- 结算弹窗只显示比牌相关玩家的牌（比牌输家和赢家）
- 不包含主动弃牌的玩家

## 设计方案

### Part 1: 后端修改

#### 1.1 比牌输家看牌事件（GameHub.cs）

在 `ZjhCompare` 方法中，比牌后判断输家是否没看过牌：

```csharp
// 获取输家信息并判断
// 如果输家没看过牌，发送他的牌给他
if (loser != null && !loser.HasLooked)
{
    var loserCards = new ZjhPlayerHandResultDto
    {
        UserId = loser.UserId,
        Nickname = loser.Nickname,
        Hand = loser.Hand.Select(c => CardDto.FromCard(c)).ToList(),
        HandDescription = evaluator.GetHandDescription(...)
    };

    await Clients.User(loserId).SendAsync("ZjhCompareLose", loserCards);
}
```

#### 1.2 修改比牌逻辑（ZjhGameEngine.cs）

比牌输家不再设置 `IsFolded = true`，改用 `IsCompareLose = true`：

```csharp
if (result < 0)
{
    player.IsCompareLose = true;  // 改用 IsCompareLose 标记
    player.LastAction = ZjhAction.Fold;
}
else
{
    targetPlayer.IsCompareLose = true;
    targetPlayer.LastAction = ZjhAction.Fold;
}
```

#### 1.3 修改结算逻辑（ZjhGameService.cs）

修改 `FinishGameAsync` 方法中的判断条件：

```csharp
// 原来的条件：!player.IsFolded && !player.IsOut
// 修改为：比牌输家也要显示
if ((!player.IsFolded && !player.IsOut) || player.IsCompareLose)
{
    result.PlayerHands.Add(new ZjhPlayerHandResultDto { ... });
}
```

### Part 2: 前端修改

#### 2.1 SignalR 事件监听（useSignalR.ts）

添加 `ZjhCompareLose` 事件的监听和导出。

#### 2.2 zjhGame Store（stores/zjhGame.ts）

- 添加 `ZjhPlayerHandResult` 类型
- 添加 `compareLoseResult` 状态
- 添加 `setCompareLoseResult` action

#### 2.3 游戏页面（pages/zjh-game/zjh-game.vue）

添加比牌输家看牌弹窗：

```vue
<view v-if="zjhGameStore.compareLoseResult" class="compare-lose-modal">
  <!-- 展示输家的牌和牌型 -->
</view>
```

## 修改文件清单

### 后端
1. `backend/src/PokerGame.Api/Hubs/GameHub.cs` - 添加比牌输家事件发送
2. `backend/src/PokerGame.Domain/GameLogic/ZjhGameEngine.cs` - 修改比牌逻辑
3. `backend/src/PokerGame.Application/Services/ZjhGameService.cs` - 修改结算逻辑

### 前端
1. `frontend/src/composables/useSignalR.ts` - 添加事件监听
2. `frontend/src/stores/zjhGame.ts` - 添加状态和类型
3. `frontend/src/pages/zjh-game/zjh-game.vue` - 添加比牌输家弹窗

## 注意事项

1. `IsCompareLose` 字段已存在于 `ZjhPlayer` 类中，无需新增
2. 前端需要正确处理 SignalR 事件的生命周期，避免内存泄漏
3. 比牌弹窗需要美观且不遮挡重要游戏信息
