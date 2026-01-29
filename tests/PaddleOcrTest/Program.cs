using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using System.Diagnostics;
using OpenCvSharp;

Console.WriteLine("=== PaddleOCR 中文识别测试 ===\n");

// 测试图片路径
string testImagePath = "test_image.png";

if (!File.Exists(testImagePath))
{
    Console.WriteLine("错误：找不到测试图片！");
    Console.WriteLine($"请在以下位置放置测试图片：{Path.GetFullPath(testImagePath)}");
    Console.WriteLine("图片应包含中文文字，例如：姓名、年龄、性别等");
    Console.WriteLine("\n按任意键退出...");
    Console.ReadKey();
    return;
}

try
{
    Console.WriteLine("正在初始化 PaddleOCR（中文模型）...");
    Console.WriteLine("首次运行会下载模型文件（约 50MB），请稍候...\n");

    // 使用在线模型（自动下载）
    FullOcrModel model = await OnlineFullModels.ChineseV4.DownloadAsync();

    using PaddleOcrAll ocr = new PaddleOcrAll(model)
    {
        AllowRotateDetection = true,
        Enable180Classification = false,
    };

    Console.WriteLine("初始化完成！\n");
    Console.WriteLine("开始识别图片...");

    // 加载图片
    using Mat image = Cv2.ImRead(testImagePath);

    if (image.Empty())
    {
        Console.WriteLine("错误：无法读取图片！");
        Console.WriteLine("请确认图片格式正确（支持 PNG、JPG）");
        Console.ReadKey();
        return;
    }

    // 开始计时
    Stopwatch sw = Stopwatch.StartNew();

    // 识别图片
    PaddleOcrResult result = ocr.Run(image);

    // 停止计时
    sw.Stop();

    // 输出结果
    Console.WriteLine($"\n✓ 识别完成！耗时：{sw.ElapsedMilliseconds} ms");
    Console.WriteLine($"✓ 识别到 {result.Regions.Length} 个文本区域\n");
    Console.WriteLine("=== 识别结果 ===\n");

    int index = 1;
    foreach (var region in result.Regions)
    {
        Console.WriteLine($"[{index}] 文本：{region.Text}");
        Console.WriteLine($"    置信度：{region.Score:P2}");

        // 获取文本框中心点
        var rect = region.Rect;
        Console.WriteLine($"    中心位置：({rect.Center.X:F0}, {rect.Center.Y:F0})");
        Console.WriteLine($"    大小：{rect.Size.Width:F0} x {rect.Size.Height:F0}");
        Console.WriteLine();
        index++;
    }

    // 评估结果
    Console.WriteLine("=== 评估 ===");
    Console.WriteLine($"识别速度：{(sw.ElapsedMilliseconds <= 3000 ? "✓ 通过" : "✗ 超时")} (目标 ≤ 3000ms，实际 {sw.ElapsedMilliseconds}ms)");

    if (result.Regions.Length > 0)
    {
        double avgScore = result.Regions.Average(r => r.Score);
        Console.WriteLine($"平均置信度：{avgScore:P2} {(avgScore >= 0.85 ? "✓ 通过" : "✗ 偏低")} (目标 ≥ 85%)");
    }
    else
    {
        Console.WriteLine("警告：未识别到任何文本！");
        Console.WriteLine("建议：");
        Console.WriteLine("  1. 确认图片包含清晰的文字");
        Console.WriteLine("  2. 尝试提高图片分辨率");
        Console.WriteLine("  3. 确保文字对比度足够");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ 错误：{ex.Message}");
    Console.WriteLine($"\n详细信息：\n{ex}");
}

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();
