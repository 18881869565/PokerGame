using SqlSugar;
using PokerGame.Infrastructure.Repository;
using PokerGame.Infrastructure.Database;
using PokerGame.Api.Hubs;
using PokerGame.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器
builder.Services.AddControllers();

// 添加 SignalR
builder.Services.AddSignalR();

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "德州扑克 API",
        Version = "v1",
        Description = "德州扑克棋牌平台后端 API"
    });

    // JWT 认证配置
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT 认证，格式: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 注册 HttpContextAccessor（用于 AOP 获取上下文）
builder.Services.AddHttpContextAccessor();

// 注册 SqlSugar (AddScoped 方式)
builder.Services.AddScoped<ISqlSugarClient>(s =>
{
    var configuration = s.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? configuration["ConnectionStrings:DefaultConnection"];

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("数据库连接字符串未配置");
    }

    SqlSugarClient sqlSugar = new SqlSugarClient(new ConnectionConfig
    {
        ConnectionString = connectionString,
        DbType = DbType.MySql,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    },
    db =>
    {
#if DEBUG
        // SQL 执行日志
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            Console.WriteLine($"[SQL] {sql}");
            if (pars != null && pars.Length > 0)
            {
                Console.WriteLine($"[Parameters] {string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}"))}");
            }
        };
#endif
    });

    return sqlSugar;
});

// 添加仓储
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 添加 Application 层服务
builder.Services.AddScoped<PokerGame.Application.Interfaces.IAuthService, PokerGame.Application.Services.AuthService>();
builder.Services.AddScoped<PokerGame.Application.Interfaces.IUserService, PokerGame.Application.Services.UserService>();
builder.Services.AddScoped<PokerGame.Application.Interfaces.IRoomService, PokerGame.Application.Services.RoomService>();
builder.Services.AddScoped<PokerGame.Application.Interfaces.IFriendService, PokerGame.Application.Services.FriendService>();
builder.Services.AddScoped<PokerGame.Application.Interfaces.IGameService, PokerGame.Application.Services.GameService>();

// 添加 CORS（允许前端跨域访问）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:9000",
                "http://127.0.0.1:9000",
                "http://8.137.12.241:9000",
                "http://192.168.31.100:9000"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 添加 JWT 认证
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSecretKeyForDevelopment123456"))
    };

    // 支持 SignalR 从 URL 读取 access_token
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gameHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// 初始化数据库 - 使用服务提供者获取 ISqlSugarClient
Console.WriteLine("开始初始化数据库...");

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Console.WriteLine($"[DEBUG] ConnectionString: {configuration.GetConnectionString("DefaultConnection") ?? "NULL"}");

        // 创建数据库（如果不存在）
        db.DbMaintenance.CreateDatabase();
        Console.WriteLine("数据库创建/连接成功");

        // 创建表（如果不存在）
        var types = new Type[]
        {
            typeof(User),
            typeof(Friend),
            typeof(Room),
            typeof(RoomPlayer),
            typeof(Game)
        };

        foreach (var type in types)
        {
            var tableName = db.EntityMaintenance.GetEntityInfo(type).DbTableName;
            Console.WriteLine($"检查表: {tableName}");

            if (!db.DbMaintenance.IsAnyTable(tableName))
            {
                db.CodeFirst.InitTables(type);
                Console.WriteLine($"创建表成功: {tableName}");
            }
            else
            {
                Console.WriteLine($"表已存在: {tableName}");
            }
        }

        Console.WriteLine("数据库初始化完成!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"数据库初始化失败: {ex.Message}");
    Console.WriteLine($"内部异常: {ex.InnerException?.Message}");
    throw;
}

// 启用 Swagger (开发环境)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "德州扑克 API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gameHub");

app.Run();
