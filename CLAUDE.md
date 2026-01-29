# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**蓝牙刷卡器工具 (Bluetooth Card Reader Tool)** - A Windows desktop application for hospital endoscope sterilization workflow automation. The tool integrates Bluetooth HID card reader monitoring, screen OCR recognition (PaddleOCR), and backend API integration for sterilization verification and information binding.

**Technology Stack**: C# .NET 6, WinForms, PaddleOCR-Sharp, OpenCvSharp4, Raw Input API

**Current Status**: Framework complete, core feature development in progress (30% complete)

## Build & Run Commands

### Development
```bash
# Navigate to main project
cd BluetoothCardReaderTool

# Build the project
dotnet build

# Run the application
dotnet run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

### Testing
```bash
# Run HID keyboard test
cd tests/HidKeyboardTest
dotnet run

# Run OCR test
cd tests/PaddleOcrTest
dotnet run

# Run Bluetooth test
cd tests/BluetoothTest
dotnet run
```

### Publishing
```bash
# Build self-contained single-file executable (66MB)
cd BluetoothCardReaderTool
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

# Output location: bin\Release\net6.0-windows\win-x64\publish\蓝牙刷卡器工具.exe
```

## Architecture

### Project Structure
```
BluetoothCardReaderTool/          # Main application
├── Core/                          # Business logic layer (to be implemented)
│   ├── BluetoothManager.cs       # HID keyboard monitoring via Raw Input API
│   ├── OcrService.cs             # PaddleOCR integration for screen capture
│   └── ApiClient.cs              # HTTP client for V1/V2 backend APIs
├── UI/                            # Presentation layer
│   ├── MainForm.cs               # Main window with 4 tabs (Bluetooth, OCR, Service, Background)
│   └── FloatingBallForm.cs       # Manual card input fallback (to be implemented)
├── Models/                        # Data models
│   └── AppSettings.cs            # Configuration model with nested configs
├── Utils/                         # Utilities
│   ├── ConfigManager.cs          # JSON config persistence
│   └── AutoStartupManager.cs     # Windows startup registry (to be implemented)
└── Program.cs                     # Application entry point

tests/                             # Validation projects (archived)
├── HidKeyboardTest/              # Raw Input API proof-of-concept
├── PaddleOcrTest/                # OCR recognition validation
└── BluetoothTest/                # BLE GATT exploration (deprecated)
```

### Configuration System

**File**: `app_settings.json` (auto-generated in application directory)

**Structure**: Hierarchical configuration with 4 main sections:
- `Bluetooth`: HID device selection, card number parsing rules
- `Ocr`: Field definitions with screen regions, default values, examples
- `Service`: Dual API version support (V1/V2), verification toggle, popup settings
- `Background`: Auto-submit mode, startup behavior, floating ball toggle

**Persistence**: Managed by `ConfigManager` using `System.Text.Json` with UTF-8 encoding

### Business Flow

1. **Card Reading**: HID keyboard device sends 10-digit card number via Raw Input API
2. **Verification** (optional): Call V1/V2 sterilization verification endpoint with card number
3. **OCR Recognition**: Capture screen regions, extract patient info via PaddleOCR
4. **Data Binding**: Submit combined data (card + OCR fields) to binding endpoint
5. **Result Display**: Show success/failure popup with auto-close countdown

### Key Technical Decisions

**HID Monitoring**: Uses Windows Raw Input API instead of BLE GATT because target devices operate as HID keyboards. Device filtering by handle prevents interference from other keyboards.

**OCR Engine**: PaddleOCR-Sharp chosen for offline operation (no cloud API), Chinese text support, and acceptable performance (1808ms recognition time, 97.2% accuracy validated).

**Packaging**: Self-contained deployment includes .NET runtime and native dependencies (OpenCV, PaddleInference) for zero-installation deployment on hospital workstations.

**Dual API Support**: Maintains V1.0 and V2.0 backend configurations simultaneously because hospitals may run different system versions. V2.0 is default.

## Code Reuse from Tests

### HID Keyboard Monitoring
**Source**: `tests/HidKeyboardTest/Form1.cs`

**Reusable Components**:
- Raw Input API P/Invoke declarations and structures
- Device enumeration via `GetRawInputDeviceList`
- Device name retrieval via `GetRawInputDeviceInfo`
- Keyboard input filtering by device handle
- Input buffer management with Enter key detection
- System tray integration with minimize-to-tray behavior

**Target**: `Core/BluetoothManager.cs`

### OCR Recognition
**Source**: `tests/PaddleOcrTest/Program.cs`

**Reusable Components**:
- PaddleOCR initialization with online model download
- Image loading and preprocessing with OpenCvSharp
- Text extraction and confidence scoring
- Result formatting and display

**Target**: `Core/OcrService.cs`

## Development Guidelines

### Adding New Features

When implementing the 4 configuration tabs, follow this pattern:
1. Create UI controls in `MainForm.Designer.cs` (or use designer)
2. Wire up event handlers in `MainForm.cs`
3. Implement business logic in `Core/` classes
4. Update `Models/AppSettings.cs` if new config fields needed
5. Test with `dotnet run` before committing

### Configuration Changes

To add new config fields:
1. Update model classes in `Models/AppSettings.cs`
2. Provide default values in property initializers
3. No migration code needed - `ConfigManager` handles missing fields gracefully

### Native Interop

When adding Windows API calls:
- Use `[DllImport("user32.dll")]` or appropriate DLL
- Define structures with `[StructLayout(LayoutKind.Sequential)]`
- Handle `IntPtr` carefully, check for `IntPtr.Zero`
- Marshal strings with `Marshal.PtrToStringAuto` for device names

### Error Handling

- Log errors to the main log window (TextBox in MainForm)
- Show user-friendly MessageBox for critical errors
- Don't crash on config load failure - use defaults
- Validate API responses before processing

## Important Notes

### PaddleOCR Models
First run downloads models (~100MB) to user directory. Consider pre-packaging models in `models/` folder for offline deployment.

### HID Device Permissions
No special permissions required - Raw Input API works in user context. Device must be paired in Windows Bluetooth settings before tool can monitor it.

### API Endpoints
V2.0 endpoints use placeholder IPs (10.10.5.116). Update in Service Config tab before deployment. V1.0 endpoints pending hospital documentation.

### Chinese Text Support
All UI labels and log messages use Chinese. Config file uses UTF-8 encoding. Assembly name is "蓝牙刷卡器工具".

## Next Development Tasks

**Priority 1 (High)**: Implement Bluetooth Config tab with device selection and HID monitoring
**Priority 2 (High)**: Implement OCR Config tab with field management and region selection
**Priority 3 (Medium)**: Implement Service Config tab with API endpoint configuration
**Priority 4 (Medium)**: Implement Background Config tab with auto-submit and startup options
**Priority 5 (Low)**: Integrate business flow connecting all components

See `docs/project-summary.md` for detailed roadmap and `docs/quick-start.md` for development workflow.
