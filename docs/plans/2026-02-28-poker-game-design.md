# 德州扑克棋牌平台设计文档

> 创建日期：2026-02-28

## 一、项目概述

**项目名称**：德州扑克棋牌平台（H5 版）

**核心定位**：免费娱乐性质的德州扑克游戏平台，支持好友对战，无需现金交易。

**技术栈**：
- 后端：.NET 8 Web API + SignalR + MySQL
- 前端：UniApp + Vue 3 + TypeScript + Pinia
- 实时通信：SignalR WebSocket
- 数据库：MySQL/MariaDB
- ORM：SqlSugar

**目标用户**：休闲玩家，想和朋友在线玩德州扑克

**首发功能范围**：
- 经典无限注德州扑克
- 房间创建与加入（房间号/扫码/好友邀请）
- 虚拟筹码系统
- 基础好友系统

**后续扩展方向**：
- 更多棋牌游戏（斗地主、跑得快等）
- 更多平台（微信小程序、App）
- 更多玩法（限注、彩池限注）

## 二、系统架构

**整体架构图**：

```
┌─────────────────────────────────────────────────────┐
│              UniApp H5 前端 (Vue 3)                  │
│  (用户界面、游戏交互、WebSocket 客户端)              │
└─────────────────────┬───────────────────────────────┘
                      │ HTTP + WebSocket
┌─────────────────────▼───────────────────────────────┐
│               .NET 8 Web API                        │
│  ┌─────────────┬─────────────┬─────────────┐       │
│  │ Controllers │  SignalR    │  Middleware │       │
│  │ (REST API)  │  Hubs       │  (认证/日志) │       │
│  └─────────────┴─────────────┴─────────────┘       │
│  ┌─────────────────────────────────────────┐       │
│  │              Services 层                 │       │
│  │  UserService | GameService | RoomService │       │
│  └─────────────────────────────────────────┘       │
│  ┌─────────────────────────────────────────┐       │
│  │           Repositories 层               │       │
│  │  (数据访问、SqlSugar ORM)                │       │
│  └─────────────────────────────────────────┘       │
└─────────────────────┬───────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────┐
│                    MySQL 数据库                      │
│  Users | Friends | Rooms | Games | Transactions     │
└─────────────────────────────────────────────────────┘
```

**分层职责**：
- **Controllers**：处理 HTTP 请求，参数验证，返回 JSON 响应
- **SignalR Hubs**：处理实时游戏通信（出牌、状态同步）
- **Services**：业务逻辑处理，事务协调
- **Repositories**：数据访问，使用 SqlSugar ORM 框架
  - 轻量级高性能 ORM
  - 支持查询表达式、批量操作、多库兼容
  - 文档：https://www.donet5.com/Home/Doc

## 三、核心功能模块

**用户模块**
- 用户注册/登录（账号密码或手机号）
- 用户信息管理（昵称、头像）
- 虚拟筹码管理（初始筹码、每日赠送）

**好友模块**
- 添加好友（搜索用户、发送请求）
- 好友列表管理（接受/拒绝/删除）
- 好友在线状态

**房间模块**
- 创建房间（设置局数、盲注级别）
- 加入房间（房间号/扫码/好友邀请）
- 房间内等待（聊天、准备状态）
- 房主踢人、解散房间

**游戏模块**
- 德州扑克核心逻辑（发牌、下注、比牌）
- 游戏状态机（等待→进行→结算）
- 实时同步玩家操作（通过 SignalR）
- 结算与筹码分配

## 四、数据库表设计

**用户表（Users）**
| 字段 | 类型 | 描述 |
|------|------|------|
| Id | bigint | 主键 |
| Username | varchar(50) | 用户名 |
| PasswordHash | varchar(255) | 密码哈希 |
| Nickname | varchar(50) | 昵称 |
| Avatar | varchar(255) | 头像URL |
| Chips | bigint | 虚拟筹码 |
| DailyGiftAt | datetime | 上次赠送时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

**好友关系表（Friends）**
| 字段 | 类型 | 描述 |
|------|------|------|
| Id | bigint | 主键 |
| UserId | bigint | 用户ID |
| FriendId | bigint | 好友ID |
| Status | int | 状态（待确认/已接受/已拒绝）|
| CreatedAt | datetime | 创建时间 |

**房间表（Rooms）**
| 字段 | 类型 | 描述 |
|------|------|------|
| Id | bigint | 主键 |
| RoomCode | varchar(6) | 6位房间号 |
| OwnerId | bigint | 房主ID |
| MaxPlayers | int | 最大玩家数 |
| SmallBlind | int | 小盲注 |
| BigBlind | int | 大盲注 |
| Status | int | 状态（等待中/进行中/已结束）|
| CreatedAt | datetime | 创建时间 |

**房间玩家表（RoomPlayers）**
| 字段 | 类型 | 描述 |
|------|------|------|
| Id | bigint | 主键 |
| RoomId | bigint | 房间ID |
| UserId | bigint | 用户ID |
| SeatIndex | int | 座位号 |
| Chips | bigint | 带入筹码 |
| IsReady | bool | 是否准备 |
| IsOnline | bool | 是否在线 |

**游戏对局表（Games）**
| 字段 | 类型 | 描述 |
|------|------|------|
| Id | bigint | 主键 |
| RoomId | bigint | 房间ID |
| StartTime | datetime | 开始时间 |
| EndTime | datetime | 结束时间 |
| WinnerId | bigint | 赢家ID |
| Pot | bigint | 底池 |

## 五、SignalR 实时通信

**GameHub（游戏中心）**

连接管理：
- 用户上线/下线、重连处理

房间操作：
- JoinRoom(roomCode) - 加入房间
- LeaveRoom() - 离开房间
- ReadyGame() - 准备游戏

游戏操作：
- Bet(amount) - 下注
- Fold() - 弃牌
- Check() - 过牌
- Raise(amount) - 加注
- AllIn() - 全押

**前端监听事件**
| 事件 | 描述 |
|------|------|
| PlayerJoined | 玩家加入 |
| PlayerLeft | 玩家离开 |
| GameStarted | 游戏开始 |
| GameEnded | 游戏结束 |
| PlayerAction | 玩家操作通知 |
| TurnChanged | 轮次变更 |
| CardsDealt | 发牌通知 |
| ChipsUpdated | 筹码更新 |

**通信流程示例**：
```
1. 玩家A调用 JoinRoom(roomCode)
2. 服务端验证后广播 PlayerJoined(playerInfo)
3. 所有房间内玩家收到通知，更新界面
4. 游戏开始后，服务端推送 CardsDealt 给每位玩家
5. 轮到玩家时，推送 TurnChanged(currentPlayerId)
```

## 六、REST API 接口设计

**用户相关**
| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/auth/register | 用户注册 |
| POST | /api/auth/login | 用户登录 |
| GET | /api/user/profile | 获取用户信息 |
| PUT | /api/user/profile | 更新用户信息 |
| POST | /api/user/daily-gift | 领取每日筹码 |

**好友相关**
| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/friend/request | 发送好友请求 |
| POST | /api/friend/accept/{id} | 接受好友请求 |
| DELETE | /api/friend/{id} | 删除好友 |
| GET | /api/friend/list | 获取好友列表 |

**房间相关**
| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/room/create | 创建房间 |
| GET | /api/room/{roomCode} | 获取房间信息 |
| POST | /api/room/qrcode/{roomCode} | 生成房间二维码 |

## 七、UniApp 前端架构

**技术栈**
- 框架：Vue 3 + TypeScript
- 跨平台：UniApp（编译到 H5/小程序/App）
- 状态管理：Pinia
- 实时通信：@microsoft/signalr 客户端

**项目结构**
```
frontend/
├── src/
│   ├── pages/                 # 页面（UniApp 页面路由）
│   │   ├── index.vue          # 首页
│   │   ├── login.vue          # 登录注册
│   │   ├── lobby.vue          # 大厅
│   │   ├── room.vue           # 房间等待
│   │   └── game.vue           # 游戏桌面
│   ├── components/            # Vue 组件
│   │   ├── Card.vue           # 扑克牌组件
│   │   ├── Chip.vue           # 筹码组件
│   │   ├── PlayerSeat.vue     # 玩家座位
│   │   └── ActionBar.vue      # 操作按钮栏
│   ├── composables/           # 组合式函数
│   │   ├── useSignalR.ts      # SignalR 连接封装
│   │   └── useGame.ts         # 游戏逻辑封装
│   ├── stores/                # Pinia 状态管理
│   │   ├── user.ts            # 用户状态
│   │   ├── room.ts            # 房间状态
│   │   └── game.ts            # 游戏状态
│   ├── api/                   # HTTP 请求
│   │   └── index.ts           # Axios 封装
│   └── utils/                 # 工具函数
├── manifest.json              # UniApp 配置
└── pages.json                 # 页面路由配置
```

**Vue 组件规范**
- 使用 `<script setup lang="ts">` 语法
- Props 使用 `defineProps` + TypeScript 接口
- 事件使用 `defineEmits`
- 复用逻辑提取为 composables

## 八、游戏流程与状态机

**德州扑克一局流程**
```
1. 等待玩家入座 → 人满或房主开始
2. 发底牌（每人2张）→ 首轮下注（盲注已下）
3. 翻牌（3张公共牌）→ 第二轮下注
4. 转牌（1张公共牌）→ 第三轮下注
5. 河牌（1张公共牌）→ 最后一轮下注
6. 摊牌比大小 → 结算筹码 → 本局结束
```

**游戏状态枚举**
| 状态 | 描述 |
|------|------|
| Waiting | 等待玩家 |
| Starting | 游戏开始中 |
| PreFlop | 发底牌阶段 |
| Flop | 翻牌阶段 |
| Turn | 转牌阶段 |
| River | 河牌阶段 |
| Showdown | 摊牌阶段 |
| Finished | 本局结束 |

**玩家操作状态**
| 状态 | 描述 |
|------|------|
| Waiting | 等待轮次 |
| MyTurn | 轮到我操作 |
| Folded | 已弃牌 |
| AllIn | 已全押 |
| OutOfChips | 筹码不足 |

## 九、后端项目结构

**.NET 8 项目结构**
```
backend/
├── PokerGame.sln                    # 解决方案文件
├── src/
│   ├── PokerGame.Api/               # Web API 层
│   │   ├── Controllers/             # API 控制器
│   │   ├── Hubs/                    # SignalR Hubs
│   │   ├── Middleware/              # 中间件
│   │   └── Program.cs               # 启动配置
│   ├── PokerGame.Application/       # 应用层
│   │   ├── Services/                # 业务服务
│   │   ├── DTOs/                    # 数据传输对象
│   │   └── Interfaces/              # 服务接口
│   ├── PokerGame.Domain/            # 领域层
│   │   ├── Entities/                # 实体类
│   │   ├── Enums/                   # 枚举定义
│   │   └── GameLogic/               # 游戏核心逻辑
│   └── PokerGame.Infrastructure/    # 基础设施层
│       ├── Repository/              # SqlSugar 仓储
│       ├── Database/                # 数据库配置
│       └── Configurations/          # 外部配置
└── tests/
    └── PokerGame.Tests/             # 单元测试
```

**项目依赖关系**
```
Api → Application → Domain
Api → Infrastructure → Domain
```

## 十、开发计划与扩展

**开发阶段规划**

**第一阶段：基础框架（1-2周）**
- 后端项目搭建（.NET 8 + SqlSugar + MySQL）
- 前端项目搭建（UniApp + Vue 3 + Pinia）
- 用户注册/登录功能
- 基础 API 接口

**第二阶段：房间系统（1-2周）**
- 创建/加入房间
- 房间号分享、二维码生成
- 好友系统基础功能
- SignalR 连接与房间同步

**第三阶段：游戏核心（2-3周）**
- 德州扑克核心逻辑
- 游戏状态机实现
- SignalR 实时游戏同步
- 前端游戏界面与交互

**第四阶段：完善优化（1-2周）**
- 测试与 Bug 修复
- UI/UX 优化
- 每日筹码赠送

**后续扩展方向**
- 新增游戏：斗地主、跑得快
- 新增平台：微信小程序、App
- 新增功能：排行榜、成就系统、表情互动
