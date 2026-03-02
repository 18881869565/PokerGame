# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

德州扑克棋牌平台 - 免费娱乐性质的德州扑克游戏平台，支持好友对战。

**技术栈**：
- **后端**: .NET 8 Web API + SignalR + SqlSugar + MySQL
- **前端**: UniApp + Vue 3 + TypeScript + Pinia

## Project Structure

```
├── backend/                          # .NET 8 后端
│   ├── PokerGame.sln                 # 解决方案文件
│   ├── src/
│   │   ├── PokerGame.Api/            # Web API 层
│   │   │   ├── Controllers/          # API 控制器
│   │   │   ├── Hubs/                 # SignalR Hubs
│   │   │   └── Program.cs            # 启动配置
│   │   ├── PokerGame.Application/    # 应用层
│   │   │   ├── Services/             # 业务服务
│   │   │   ├── DTOs/                 # 数据传输对象
│   │   │   └── Interfaces/           # 服务接口
│   │   ├── PokerGame.Domain/         # 领域层
│   │   │   ├── Entities/             # 实体类
│   │   │   ├── Enums/                # 枚举定义
│   │   │   └── GameLogic/            # 游戏核心逻辑
│   │   └── PokerGame.Infrastructure/ # 基础设施层
│   │       ├── Repository/           # SqlSugar 仓储
│   │       └── Database/             # 数据库配置
│   └── tests/
│       └── PokerGame.Tests/          # 单元测试
├── frontend/                         # UniApp 前端
│   ├── src/
│   │   ├── pages/                    # 页面
│   │   ├── components/               # Vue 组件
│   │   ├── composables/              # 组合式函数
│   │   ├── stores/                   # Pinia 状态管理
│   │   ├── api/                      # HTTP 请求封装
│   │   └── utils/                    # 工具函数
│   ├── pages.json                    # 页面路由配置
│   └── manifest.json                 # UniApp 配置
└── docs/
    └── plans/                        # 设计文档
```

## Common Commands

### Backend (.NET 8)
```bash
cd backend

# 构建项目
dotnet build

# 运行项目 (默认 http://localhost:5000)
dotnet run --project src/PokerGame.Api

# 运行测试
dotnet test

# 添加包
dotnet add src/PokerGame.Api package [Package.Name]
```

### Frontend (UniApp + Vue 3)
```bash
cd frontend

# 安装依赖
npm install

# 运行 H5 开发服务器
npm run dev:h5

# 构建 H5 生产版本
npm run build:h5

# 运行微信小程序开发
npm run dev:mp-weixin
```

## Architecture Notes

### 后端架构
- **分层架构**: Controllers → Services → Repositories → SqlSugar
- **ORM**: 使用 SqlSugar，文档 https://www.donet5.com/Home/Doc
- **实时通信**: SignalR 用于游戏实时同步
- **认证**: JWT Bearer Token

### 前端架构
- **框架**: UniApp + Vue 3 Composition API + TypeScript
- **状态管理**: Pinia (user, room, game stores)
- **实时通信**: @microsoft/signalr 客户端
- **组件规范**: `<script setup lang="ts">` 语法

### 游戏状态机
```
Waiting → Starting → PreFlop → Flop → Turn → River → Showdown → Finished
```

## Available Skills

### dotnet-best-practices
- .NET/C# 最佳实践
- 文档、设计模式、依赖注入、异步编程、测试标准

### vue
- Vue 3.5+ 框架
- Composition API、响应式系统、组件模式

## 配置说明

### 数据库连接
修改 `backend/src/PokerGame.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=poker_game;User=root;Password=your_password;"
  }
}
```

### JWT 配置
```json
{
  "Jwt": {
    "Key": "YourSecretKey",
    "Issuer": "PokerGame",
    "Audience": "PokerGameUsers"
  }
}
```
