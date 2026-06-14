# 弹弹堂登录器 (DDTank Launcher)

基于 WPF (.NET 10) 的弹弹堂游戏登录器，支持 Flash 游戏运行、账号管理和多开。

## 功能特性

- ✅ Flash Player 集成（绿色安装，无需单独安装）
- ✅ 4399 账号登录（AES 加密密码）
- ✅ 验证码处理
- ✅ 账号管理（兼容36脚本大厅格式）
- ✅ 分组管理（1-10小组）
- ✅ 一键导入36脚本大厅账号
- ✅ 多开支持
- ✅ 游戏窗口类名：`MacromediaFlashPlayerActiveX`（脚本兼容）

## 环境要求

- Windows 10/11
- .NET 10 SDK（下载：https://dotnet.microsoft.com/download/dotnet/10.0）

## 快速开始

### 1. 安装 .NET 10 SDK

```powershell
# 检查是否已安装
dotnet --version
```

### 2. 克隆项目

```bash
git clone https://github.com/2212018862/DDTankLauncher.git
cd DDTankLauncher
```

### 3. 运行项目

```powershell
dotnet run
```

### 4. 发布项目

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

## 项目结构

```
DDTankLauncher/
├── Views/                          # 窗口界面
│   ├── MainWindow.xaml/.cs         # 主窗口（账号列表、分组按钮）
│   ├── AddAccountWindow.xaml/.cs   # 添加账号（含一键导入）
│   ├── CaptchaWindow.xaml/.cs      # 验证码输入
│   ├── FlashGameHost.cs            # Flash 游戏宿主
│   └── ...
├── Services/                       # 业务服务
│   ├── AccountManager36.cs         # 账号管理（兼容36脚本大厅）
│   ├── Login4399Service.cs         # 4399登录（AES加密）
│   └── PasswordCrypto36.cs         # 密码加密
├── Models/                         # 数据模型
│   └── GameAccount.cs
├── Resources/                      # 资源文件
│   ├── flashplayer_sa.exe          # Flash Player
│   ├── flash/
│   │   ├── Flash64_34_0_0_323.ocx  # Flash.ocx (64位)
│   │   └── Flash64.manifest        # 激活上下文清单
│   └── Loading.swf                 # 游戏加载页面
├── App.xaml/.cs                    # 应用程序入口
└── DDTankLauncher.csproj           # 项目文件
```

## 使用说明

### 登录游戏

1. 点击 **添加账号** 输入4399账号密码
2. 或点击 **一键导入36脚本大厅账号**
3. 双击账号卡片登录

### 分组管理

- 点击 **1-10** 按钮切换分组
- 再次点击取消选中，显示全部账号

### 发布打包

```powershell
dotnet publish DDTankLauncher.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## 技术栈

- WPF (.NET 10)
- Flash.ocx ActiveX（AtlAxWin + 激活上下文）
- AES 加密（4399密码加密）

## License

MIT License
