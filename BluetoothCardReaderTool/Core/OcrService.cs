using System.Drawing;
using System.Drawing.Imaging;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using BluetoothCardReaderTool.Models;

namespace BluetoothCardReaderTool.Core;

/// <summary>
/// OCR 识别结果
/// </summary>
public class OcrResult
{
    public string Text { get; set; } = "";
    public double Confidence { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
}

/// <summary>
/// OCR 识别服务
/// </summary>
public class OcrService : IDisposable
{
    private PaddleOcrAll? _ocr;
    private bool _isInitialized;
    private readonly object _lock = new object();

    /// <summary>
    /// 初始化 OCR 引擎（异步）
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;

            try
            {
                // 使用在线模型（自动下载）
                FullOcrModel model = OnlineFullModels.ChineseV4.DownloadAsync().Result;

                _ocr = new PaddleOcrAll(model)
                {
                    AllowRotateDetection = true,
                    Enable180Classification = false,
                };

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"初始化 PaddleOCR 失败: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 截取整个屏幕
    /// </summary>
    public Bitmap CaptureScreen()
    {
        // 获取主屏幕尺寸
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    /// <summary>
    /// 裁剪图像指定区域
    /// </summary>
    public Bitmap CropImage(Bitmap source, OcrRegion region)
    {
        // 边界检查
        int x = Math.Max(0, Math.Min(region.X, source.Width - 1));
        int y = Math.Max(0, Math.Min(region.Y, source.Height - 1));
        int width = Math.Max(1, Math.Min(region.Width, source.Width - x));
        int height = Math.Max(1, Math.Min(region.Height, source.Height - y));

        var rect = new Rectangle(x, y, width, height);
        var cropped = new Bitmap(width, height);

        using (var graphics = Graphics.FromImage(cropped))
        {
            graphics.DrawImage(source, new Rectangle(0, 0, width, height), rect, GraphicsUnit.Pixel);
        }

        return cropped;
    }

    /// <summary>
    /// 识别指定区域的文本
    /// </summary>
    public OcrResult RecognizeRegion(Bitmap screenshot, OcrRegion region)
    {
        if (!_isInitialized || _ocr == null)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "OCR 引擎未初始化"
            };
        }

        try
        {
            // 裁剪区域
            using var cropped = CropImage(screenshot, region);

            // 转换为 Mat
            using var mat = BitmapConverter.ToMat(cropped);

            // 识别
            var result = _ocr.Run(mat);

            if (result.Regions.Length == 0)
            {
                return new OcrResult
                {
                    Success = true,
                    Text = "",
                    Confidence = 0
                };
            }

            // 提取所有文本和平均置信度
            var texts = result.Regions.Select(r => r.Text).ToArray();
            var avgConfidence = result.Regions.Average(r => r.Score);

            return new OcrResult
            {
                Success = true,
                Text = string.Join(" ", texts),
                Confidence = avgConfidence
            };
        }
        catch (Exception ex)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = $"识别失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 识别整张图片
    /// </summary>
    public OcrResult RecognizeImage(Bitmap image)
    {
        if (!_isInitialized || _ocr == null)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "OCR 引擎未初始化"
            };
        }

        try
        {
            // 转换为 Mat
            using var mat = BitmapConverter.ToMat(image);

            // 识别
            var result = _ocr.Run(mat);

            if (result.Regions.Length == 0)
            {
                return new OcrResult
                {
                    Success = true,
                    Text = "",
                    Confidence = 0
                };
            }

            // 提取所有文本和平均置信度
            var texts = result.Regions.Select(r => r.Text).ToArray();
            var avgConfidence = result.Regions.Average(r => r.Score);

            return new OcrResult
            {
                Success = true,
                Text = string.Join(" ", texts),
                Confidence = avgConfidence
            };
        }
        catch (Exception ex)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = $"识别失败: {ex.Message}"
            };
        }
    }

    public void Dispose()
    {
        _ocr?.Dispose();
        _isInitialized = false;
    }
}
