# 弹弹堂登录器 (DDTank Launcher)

基于 WPF (.NET 8) 的弹弹堂游戏登录器，支持 Flash 游戏运行、协议拦截、内存读取和多开。

## 功能特性

- ✅ Flash Player 集成（无需单独安装）
- ✅ 4399 账号登录
- ✅ 服务器选择
- ✅ 多开支持
- ✅ 协议拦截（开发中）
- ✅ 内存读取（开发中）
- ✅ 画面叠加层（开发中）

## 环境要求

- Windows 10/11
- .NET 8 SDK（下载：https://dotnet.microsoft.com/download/dotnet/8.0）

## 快速开始

### 1. 安装 .NET 8 SDK

```powershell
# 检查是否已安装
dotnet --version

# 如果未安装，下载并安装：
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. 打开项目

在 Trae 中打开项目目录：
```
C:\Users\22120\Desktop\DDTankLauncher
```

### 3. 还原依赖

在 Trae 终端执行：
```powershell
dotnet restore
```

### 4. 运行项目

```powershell
dotnet run
```

### 5. 发布项目

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

发布后的文件在：
```
bin\Release\net8.0-windows\win-x64\publish\
```

## 项目结构

```
DDTankLauncher/
├── Views/                    # 窗口界面
│   ├── MainWindow.xaml       # 主窗口
│   └── MainWindow.xaml.cs
├── ViewModels/               # 视图模型（MVVM）
├── Services/                 # 业务服务
│   ├── GameLoginService.cs   # 游戏登录服务
│   ├── ProtocolInterceptorService.cs  # 协议拦截
│   ├── MemoryReaderService.cs        # 内存读取
│   └── MultiInstanceManagerService.cs # 多开管理
├── Helpers/                  # 工具类
│   └── ResourceManager.cs    # 资源管理
├── Resources/                # 资源文件
│   └── flashplayer_sa.exe    # Flash Player（需手动放置）
├── Properties/
│   └── AssemblyInfo.cs
├── App.xaml                  # 应用程序入口
├── App.xaml.cs
└── DDTankLauncher.csproj     # 项目文件
```

## 首次运行

1. 将 `flashplayer_sa.exe` 放入 `Resources` 目录
2. 运行 `dotnet run`
3. 输入 4399 账号和密码
4. 选择服务器
5. 点击登录

## 开发说明

### 添加新服务

1. 在 `Services` 目录创建新类
2. 在 `MainWindow.xaml.cs` 中实例化并使用

### 修改界面

1. 编辑 `Views/MainWindow.xaml`
2. 修改样式和布局

### 协议拦截

`ProtocolInterceptorService` 提供了基础的 HTTP 代理功能，可以：
- 监听游戏请求
- 记录协议数据
- 修改请求/响应

### 内存读取

`MemoryReaderService` 提供了 Windows API 级别的内存读取：
- 附加到游戏进程
- 读取内存数据
- 搜索内存模式

## 常见问题

### Q: Flash Player 未找到

A: 将 `flashplayer_sa.exe` 放入 `Resources` 目录

### Q: 无法登录

A: 检查网络连接，确认账号密码正确

### Q: 多开卡顿

A: 减少同时运行的实例数量，或升级硬件

## License

MIT License
