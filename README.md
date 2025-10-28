# 🎵 WPF Music Player Demo

## 一、项目概述
**WPF Music Player Demo** 是一个基于 **WPF (.NET)** 的桌面音乐播放器，灵感来源于 **Spotify**。  
该项目旨在实现一个基础但完整的音乐播放体验，包含 **音乐播放、播放队列管理、歌单管理、播放顺序切换** 等核心功能，  
并在 **界面交互** 上力求高度还原 Spotify 的动效与交互体验。

---

## 二、功能实现

### 🎧 音乐播放
- 支持本地音乐文件播放  
- 实现播放、暂停、上一曲、下一曲等基础操作  

### 📜 音乐队列管理
- 可显示当前播放歌单中的曲目  
- 支持队列中曲目的动态展示与切换  

### 🎶 歌单管理
- 支持创建、重命名与删除歌单  
- 可将音乐添加至指定歌单，实现播放列表管理  

### 🔁 播放顺序切换
- 支持多种播放模式：
  - 顺序播放  
  - 单曲循环  
  - 全部循环  
  - 随机播放  
- 采用 **策略模式（Strategy Pattern）** 实现灵活切换  

### ✨ 前端交互动画
- 高度还原 Spotify 风格的界面与动效  
- 播放按钮、搜索框、菜单、歌单操作均实现平滑动画反馈  
- 优化 UI 细节与操作流畅度，提升用户体验  

---

## 三、技术架构

### 🖥 前端
- 使用 **WPF + XAML** 构建界面  
- 自定义控件样式与动画，打造现代化 UI  
- 实现响应式布局与动态视觉反馈  

### ⚙️ 后端逻辑

#### **Domain 层**
- 定义核心实体类：`MusicTrack`, `Playlist`  
- 实现播放模式策略与播放队列管理逻辑  

#### **Infrastructure 层**
- 封装底层服务：  
  - 音频播放（基于 `NAudio`）  
  - 文件读取与元数据解析（基于 `TagLib#`）  
- 提供统一接口层，保证播放稳定性  

#### **Services 层**
- 管理音乐列表与歌单  
- 控制播放逻辑与状态同步  
- 实现前后端逻辑解耦  

#### **MVVM 架构**
- **View**：纯界面逻辑与动画  
- **ViewModel**：数据绑定与命令处理  
- **Model (Domain)**：核心业务对象  

#### **辅助工具 (Common)**
- 颜色管理、窗口辅助类  
- 时间格式扩展、值转换器等通用组件  

---

## 四、技术亮点
1. 🎨 **精细化的前端动画实现** —— 高度还原 Spotify 的交互体验，动画流畅自然。  
2. 🧩 **灵活的播放模式策略设计** —— 便于扩展更多播放模式。  
3. 🎼 **完善的队列与歌单管理机制** —— 提升播放逻辑的可控性与灵活度。  
4. 🧱 **分层清晰的架构设计** —— 各模块职责明确，易于扩展与维护。  

---

## 🛠️ 技术栈
| 模块 | 技术 |
|------|------|
| 前端 | WPF / XAML |
| 架构 | MVVM |
| 音频播放 | NAudio |
| 元数据读取 | TagLib# |
| 编程语言 | C# (.NET) |

---

## 📂 项目结构
```plaintext
WPFMusicPlayerDemo/
├── App.xaml
├── MainWindow.xaml
│
├── Assets/                              # 静态资源文件
│   ├── Images/                          # 背景图、Logo等静态图片
│   └── Icons/                           # 播放控制、菜单、搜索等UI图标
│
├── Common/                              # 公共辅助类与工具
│   ├── BackgroundColorManager.cs        # 背景颜色主题管理
│   ├── ColorHelper.cs                   # 颜色转换与计算辅助
│   ├── ProgressToWidthMultiConverter.cs # 播放进度转换器
│   ├── TimeSpanExtensions.cs            # 时间格式扩展（00:00 样式）
│   └── WindowHelper.cs                  # 窗口控制辅助（拖拽、阴影等）
│
├── Domain/                              # 核心业务领域逻辑
│   ├── Entities/                        # 实体定义
│   │   ├── MusicTrack.cs                # 音乐实体类（曲目信息）
│   │   └── Playlist.cs                  # 歌单实体类
│   │
│   ├── PlayMode/                        # 播放模式策略
│   │   ├── IPlayModeStrategy.cs         # 播放模式策略接口
│   │   ├── PlayMode.cs                  # 播放模式枚举
│   │   ├── RepeatOneMode.cs             # 单曲循环模式
│   │   ├── SequentialMode.cs            # 顺序播放模式
│   │   ├── ShuffleMode.cs               # 随机播放模式
│   │   └── StopAfterCurrentMode.cs      # 当前曲目播放后停止
│   │
│   ├── Queue/                           # 播放队列逻辑
│   │   ├── DefaultQueueManager.cs       # 默认队列管理实现
│   │   ├── IPlayQueueManager.cs         # 队列管理接口
│   │   └── QueueItem.cs                 # 队列项定义
│   │
│   └── Services/                        # 业务服务接口与实现
│       ├── IMusicListManager.cs         # 音乐列表管理接口
│       ├── IPlaylistManager.cs          # 歌单管理接口
│       ├── MusicListManager.cs          # 音乐列表管理实现
│       ├── MusicPlayerController.cs     # 播放控制核心逻辑
│       ├── PlaylistManager.cs           # 歌单管理实现
│       └── TrackFactory.cs              # 曲目对象工厂（封装创建逻辑）
│
├── Infrastructure/                      # 底层技术实现层
│   ├── Audio/                           # 音频处理相关
│   │   ├── Equalizers/                  # 均衡器实现
│   │   │   ├── EqualizerSampleProvider.cs
│   │   │   ├── IEqualizer.cs
│   │   │   └── MultiChannelEqualizer.cs
│   │   ├── Filters/                     # 音频滤波器模块
│   │   │   ├── BiQuadFilterAdapter.cs
│   │   │   └── IFilter.cs
│   │   └── Player/                      # 播放器封装
│   │       ├── IAudioPlayer.cs          # 音频播放器接口
│   │       └── NAudioPlayer.cs          # 基于 NAudio 的播放器实现
│   │
│   ├── File/                            # 文件操作
│   │   ├── IAudioFileService.cs         # 音频文件访问接口
│   │   └── NAudioFileService.cs         # 音频文件服务实现
│   │
│   └── Metadata/                        # 音乐文件元数据（标签）读取
│       ├── IMetadataService.cs          # 元数据服务接口
│       └── TagLibMetadataService.cs     # 基于 TagLib# 的元数据读取实现
│
├── UI/                                  # UI 样式与值转换器
│   ├── Converters/                      # 前端绑定转换器
│   │   ├── AddOneConverter.cs
│   │   ├── BoolToVisibilityInverseConverter.cs
│   │   └── HalfValueConverter.cs
│   └── Styles/                          # 按钮与交互样式定义
│       ├── Buttons.xaml
│       ├── Buttons2.xaml
│       └── Buttons3.xaml
│
├── ViewModels/                          # MVVM 视图模型层
│   └── MainViewModel.cs                 # 主界面数据绑定与逻辑处理
│
├── Views/                               # 界面视图层
│   ├── CloudDriveView.xaml              # 云盘视图（预留）
│   ├── MenuList.xaml                    # 侧边菜单栏
│   └── PlaylistView.xaml                # 歌单展示与操作界面
│   
├── App.xaml.cs
├── AssemblyInfo.cs
└── WPFMusicPlayerDemo.sln               # 解决方案文件

