# 蓝牙刷卡器工具 - 项目总结文档

**项目名称**：蓝牙刷卡器工具（Windows）
**项目代号**：BluetoothCardReaderTool
**当前阶段**：正式开发阶段 - 框架搭建完成
**文档日期**：2026-01-29
**文档版本**：1.0

---

## 1. 项目概述

### 1.1 项目背景

医院内窥镜洗消流程需要将蓝牙刷卡器读取的卡号、屏幕 OCR 识别的患者信息与后端系统（V1.0/V2.0）进行验证和绑定，实现洗消记录的自动化追溯。

### 1.2 项目目标

开发一个 Windows 桌面工具，实现：
- 蓝牙刷卡器后台监听（HID 键盘模式）
- 屏幕 OCR 识别（PaddleOCR）
- 洗消验证接口调用
- 信息绑定接口调用
- 配置持久化
- 免安装部署

### 1.3 技术栈

| 技术项 | 选型 | 版本 |
|--------|------|------|
| 开发语言 | C# | .NET 6 |
| GUI 框架 | WinForms | .NET 6 内置 |
| OCR 引擎 | PaddleOCR-Sharp | 3.0.1 |
| 图像处理 | OpenCvSharp4 | 4.11.0 |
| 蓝牙监听 | Raw Input API | Windows 原生 |
| HTTP 请求 | HttpClient | .NET 6 内置 |
| 配置存储 | System.Text.Json | .NET 6 内置 |
| 打包方式 | Self-contained | .NET 6 |

---

## 2. 项目进度

### 2.1 已完成阶段

#### ✅ 阶段 0：需求分析（已完成）
- [x] 需求澄清（3 轮）
- [x] PRD 文档编写
- [x] 技术选型确认

**交付物**：
- `docs/prds/bluetooth-card-reader-tool-v1.0-prd.md`

#### ✅ 阶段 1：技术验证（已完成）
- [x] PaddleOCR 识别验证（识别速度 1808ms，准确率 97.20%）
- [x] HID 键盘监听验证（后台监听、设备过滤）
- [x] Self-contained 打包验证（66MB，免安装）

**交付物**：
- `docs/reports/tech-validation-report.md`
- `tests/PaddleOcrTest/`（OCR 测试代码）
- `tests/BluetoothTest/`（BLE 测试代码）
- `tests/HidKeyboardTest/`（HID 测试代码）

#### ✅ 阶段 2：项目搭建（已完成）
- [x] 创建正式项目
- [x] 配置 NuGet 依赖
- [x] 搭建项目结构
- [x] 实现配置模型
- [x] 实现配置管理器
- [x] 实现主窗体框架（四个标签页）
- [x] 集成系统托盘
- [x] 集成日志窗口

**交付物**：
- `BluetoothCardReaderTool/`（正式项目）

### 2.2 当前阶段

**阶段 3：核心功能开发（进行中）**

**待实现功能**：
- [ ] 蓝牙配置页（设备选择、HID 监听）
- [ ] OCR 配置页（字段管理、区域定位、识别测试）
- [ ] 服务配置页（接口配置、验证开关）
- [ ] 后台配置页（自启动、浮球输入、自动提交）
- [ ] 业务流程集成（刷卡 → 验证 → OCR → 绑定）

**预计时间**：10-15 天

---

## 3. 项目结构

### 3.1 目录结构

```
5t/
├── BluetoothCardReaderTool/       # 正式项目
│   ├── Core/                      # 核心业务逻辑
│   ├── UI/                        # 用户界面
│   │   ├── MainForm.cs           # 主窗体（已完成）
│   │   └── MainForm.Designer.cs  # 设计器文件
│   ├── Models/                    # 数据模型
│   │   └── AppSettings.cs        # 配置模型（已完成）
│   ├── Utils/                     # 工具类
│   │   └── ConfigManager.cs      # 配置管理器（已完成）
│   ├── Program.cs                 # 程序入口（已完成）
│   └── BluetoothCardReaderTool.csproj
│
├── docs/                          # 项目文档
│   ├── prds/                      # PRD 文档
│   │   └── bluetooth-card-reader-tool-v1.0-prd.md
│   ├── reports/                   # 验证报告
│   │   └── tech-validation-report.md
│   ├── setup-guide-notepad.md     # 开发环境指南
│   └── tech-validation-plan.md    # 技术验证计划
│
├── tests/                         # 测试项目（已归档）
│   ├── PaddleOcrTest/            # OCR 测试
│   ├── BluetoothTest/            # BLE 测试
│   └── HidKeyboardTest/          # HID 测试
│
└── README.md                      # 项目说明
```

### 3.2 代码结构

#### 已实现的类

| 类名 | 路径 | 功能 | 状态 |
|------|------|------|------|
| `AppSettings` | Models/AppSettings.cs | 配置数据模型 | ✅ 完成 |
| `ConfigManager` | Utils/ConfigManager.cs | 配置读写管理 | ✅ 完成 |
| `MainForm` | UI/MainForm.cs | 主窗体（四个标签页框架） | ✅ 完成 |

#### 待实现的类

| 类名 | 路径 | 功能 | 优先级 |
|------|------|------|--------|
| `BluetoothManager` | Core/BluetoothManager.cs | HID 键盘监听 | 🔴 高 |
| `OcrService` | Core/OcrService.cs | OCR 识别服务 | 🔴 高 |
| `ApiClient` | Core/ApiClient.cs | HTTP 接口调用 | 🟡 中 |
| `AutoStartupManager` | Utils/AutoStartupManager.cs | 开机自启动 | 🟢 低 |
| `FloatingBallForm` | UI/FloatingBallForm.cs | 浮球输入窗口 | 🟢 低 |

---

## 4. 技术验证结果

### 4.1 验证总结

| 验证项 | 目标 | 实际结果 | 状态 |
|--------|------|----------|------|
| OCR 识别速度 | ≤ 3000ms | 1808ms | ✅ 优秀 |
| OCR 识别准确率 | ≥ 85% | 97.20% | ✅ 优秀 |
| 后台监听 | 支持 | 支持 | ✅ 通过 |
| 设备过滤 | 支持 | 支持 | ✅ 通过 |
| 打包体积 | ≤ 150MB | 66MB | ✅ 优秀 |
| 免安装运行 | 支持 | 支持 | ✅ 通过 |

**结论**：✅ 所有技术验证通过，技术方案完全可行

### 4.2 可复用代码

测试项目中的以下代码可以直接复用到正式项目：

| 测试项目 | 可复用代码 | 目标位置 |
|----------|-----------|----------|
| `HidKeyboardTest` | HID 监听、设备选择 | `Core/BluetoothManager.cs` |
| `PaddleOcrTest` | OCR 识别逻辑 | `Core/OcrService.cs` |
| `HidKeyboardTest` | 系统托盘、日志窗口 | `UI/MainForm.cs`（已集成） |

---

## 5. 配置说明

### 5.1 配置文件

**文件路径**：`app_settings.json`（程序运行时自动生成）

**配置结构**：
```json
{
  "Bluetooth": {
    "LastDeviceHandle": "",
    "LastDeviceName": "",
    "HidKeywords": "Bluetooth;HID",
    "CardLength": 10,
    "RequireEnter": false
  },
  "Ocr": {
    "Fields": []
  },
  "Service": {
    "Version": "V2.0",
    "V1": {
      "VerifyUrl": "",
      "BindUrl": ""
    },
    "V2": {
      "VerifyUrl": "http://10.10.5.116:62102/api/diagnosis/lk-application/getSingleApplicationDetail/{card_number}",
      "BindUrl": "http://10.10.5.116:62102/api/diagnosis/lk-application/bindSingleDiagnosis"
    },
    "EnableVerify": true,
    "ShowResultPopup": true
  },
  "Background": {
    "SubmitMode": "manual",
    "Countdown": 5,
    "AutoStart": false,
    "EnableFloatingBall": false,
    "EnableService": true
  }
}
```

### 5.2 打包配置

**打包命令**：
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

**输出位置**：
```
bin\Release\net6.0-windows\win-x64\publish\蓝牙刷卡器工具.exe
```

---

## 6. 开发环境

### 6.1 必需软件

- **操作系统**：Windows 10 22H2 或更高
- **.NET 6 SDK**：6.0.428 或更高
- **文本编辑器**：Notepad++（或 Visual Studio 2022）

### 6.2 开发命令

```bash
# 进入项目目录
cd C:\Users\Administrator\Desktop\5t\BluetoothCardReaderTool

# 编译项目
dotnet build

# 运行项目
dotnet run

# 发布项目
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

## 7. 下一步开发计划

### 7.1 优先级排序

#### 🔴 高优先级（核心功能）

1. **实现蓝牙配置页**
   - 复用 `HidKeyboardTest` 代码
   - 实现设备选择、HID 监听
   - 实现卡号识别和显示
   - 预计时间：2-3 天

2. **实现 OCR 配置页**
   - 复用 `PaddleOcrTest` 代码
   - 实现字段管理（新增、编辑、删除）
   - 实现区域定位（截图选区）
   - 实现识别测试
   - 预计时间：3-4 天

3. **实现服务配置页**
   - 实现系统版本切换（V1/V2）
   - 实现接口 URL 配置
   - 实现验证开关
   - 预计时间：1-2 天

#### 🟡 中优先级（业务流程）

4. **实现 ApiClient**
   - 实现 HTTP 请求封装
   - 实现验证接口调用
   - 实现绑定接口调用
   - 预计时间：2-3 天

5. **集成业务流程**
   - 刷卡 → 验证 → OCR → 绑定
   - 实现弹窗提示
   - 实现错误处理
   - 预计时间：2-3 天

#### 🟢 低优先级（辅助功能）

6. **实现后台配置页**
   - 实现自动提交配置
   - 实现开机自启动
   - 实现浮球输入
   - 预计时间：2-3 天

7. **测试与优化**
   - 集成测试
   - 性能优化
   - 用户体验优化
   - 预计时间：3-5 天

### 7.2 里程碑

| 里程碑 | 目标 | 预计完成时间 |
|--------|------|--------------|
| M1 | 蓝牙配置页完成 | 第 3 天 |
| M2 | OCR 配置页完成 | 第 7 天 |
| M3 | 服务配置页完成 | 第 9 天 |
| M4 | 业务流程集成完成 | 第 14 天 |
| M5 | 全功能测试通过 | 第 21 天 |

---

## 8. 风险与问题

### 8.1 已知风险

| 风险项 | 影响 | 缓解措施 | 状态 |
|--------|------|----------|------|
| V1.0 接口规范不完整 | 高 | 先实现 V2.0，V1.0 待医院提供文档 | ⚠️ 待处理 |
| OCR 区域选择实现复杂 | 中 | 使用截图工具库简化实现 | ⚠️ 待处理 |
| PaddleOCR 模型下载 | 低 | 首次运行自动下载，或预打包模型 | ✅ 已规划 |

### 8.2 待确认事项

- [ ] V1.0 系统接口详细规范
- [ ] OCR 字段的具体列表
- [ ] 医院现场测试环境配置

---

## 9. 文档清单

### 9.1 已完成文档

| 文档名称 | 路径 | 状态 |
|----------|------|------|
| PRD 文档 | docs/prds/bluetooth-card-reader-tool-v1.0-prd.md | ✅ |
| 技术验证报告 | docs/reports/tech-validation-report.md | ✅ |
| 技术验证计划 | docs/tech-validation-plan.md | ✅ |
| 开发环境指南 | docs/setup-guide-notepad.md | ✅ |
| 项目总结文档 | docs/project-summary.md | ✅ |

### 9.2 待编写文档

- [ ] 用户手册
- [ ] 开发文档
- [ ] 部署文档
- [ ] API 接口文档

---

## 10. 团队与联系

### 10.1 项目角色

| 角色 | 负责人 | 职责 |
|------|--------|------|
| 产品经理 | 用户 | 需求确认、验收测试 |
| 开发工程师 | Claude Code | 代码开发、技术实现 |
| 测试工程师 | 用户 | 功能测试、问题反馈 |

### 10.2 沟通方式

- **开发进度**：每日更新
- **问题反馈**：即时沟通
- **需求变更**：及时确认

---

## 11. 附录

### 11.1 快速启动命令

```bash
# 进入项目目录
cd C:\Users\Administrator\Desktop\5t\BluetoothCardReaderTool

# 运行项目
dotnet run
```

### 11.2 常用命令

```bash
# 编译
dotnet build

# 清理
dotnet clean

# 还原依赖
dotnet restore

# 发布
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### 11.3 项目统计

- **代码行数**：~500 行（框架代码）
- **文件数量**：8 个代码文件
- **依赖包数量**：6 个 NuGet 包
- **项目大小**：~200KB（源代码）
- **打包大小**：~66MB（发布后）

---

**文档生成时间**：2026-01-29
**文档版本**：1.0
**下次更新**：完成蓝牙配置页后
