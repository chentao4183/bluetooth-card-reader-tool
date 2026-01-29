using System.Drawing;
using System.Windows.Forms;

namespace BluetoothCardReaderTool.UI;

/// <summary>
/// 截图和区域选择工具
/// </summary>
public class ScreenshotForm : Form
{
    private Bitmap? _screenshot;
    private System.Drawing.Point _startPoint;
    private System.Drawing.Point _endPoint;
    private bool _isSelecting;
    private Rectangle _selectedRegion;

    public ScreenshotForm()
    {
        InitializeComponent();
        CaptureScreen();
    }

    private void InitializeComponent()
    {
        // 设置窗体为全屏无边框
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.Cursor = Cursors.Cross;
        this.DoubleBuffered = true;
        this.BackColor = Color.Black;

        // 设置为半透明
        this.Opacity = 0.3;
    }

    private void CaptureScreen()
    {
        // 获取主屏幕尺寸
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

        _screenshot = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(_screenshot))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        // 设置背景图
        this.BackgroundImage = _screenshot;
        this.BackgroundImageLayout = ImageLayout.Stretch;
        this.Opacity = 1.0;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
        {
            _isSelecting = true;
            _startPoint = e.Location;
            _endPoint = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isSelecting)
        {
            _endPoint = e.Location;
            this.Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Left && _isSelecting)
        {
            _isSelecting = false;
            _endPoint = e.Location;

            // 计算选择的矩形
            int x = Math.Min(_startPoint.X, _endPoint.X);
            int y = Math.Min(_startPoint.Y, _endPoint.Y);
            int width = Math.Abs(_endPoint.X - _startPoint.X);
            int height = Math.Abs(_endPoint.Y - _startPoint.Y);

            _selectedRegion = new Rectangle(x, y, width, height);

            // 如果选择了有效区域，关闭窗体
            if (width > 10 && height > 10)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_isSelecting)
        {
            // 计算选择框
            int x = Math.Min(_startPoint.X, _endPoint.X);
            int y = Math.Min(_startPoint.Y, _endPoint.Y);
            int width = Math.Abs(_endPoint.X - _startPoint.X);
            int height = Math.Abs(_endPoint.Y - _startPoint.Y);

            // 绘制选择框
            using (var pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, x, y, width, height);
            }

            // 绘制半透明填充
            using (var brush = new SolidBrush(Color.FromArgb(50, Color.Blue)))
            {
                e.Graphics.FillRectangle(brush, x, y, width, height);
            }

            // 显示尺寸信息
            string sizeText = $"{width} x {height}";
            using (var font = new Font("微软雅黑", 12, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Yellow))
            {
                var textSize = e.Graphics.MeasureString(sizeText, font);
                var textPos = new PointF(x + width / 2 - textSize.Width / 2, y + height / 2 - textSize.Height / 2);

                // 绘制文字背景
                using (var bgBrush = new SolidBrush(Color.FromArgb(180, Color.Black)))
                {
                    e.Graphics.FillRectangle(bgBrush, textPos.X - 5, textPos.Y - 5, textSize.Width + 10, textSize.Height + 10);
                }

                e.Graphics.DrawString(sizeText, font, brush, textPos);
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // ESC 取消选择
        if (e.KeyCode == Keys.Escape)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    /// <summary>
    /// 获取选择的区域
    /// </summary>
    public Rectangle GetSelectedRegion()
    {
        return _selectedRegion;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screenshot?.Dispose();
        }
        base.Dispose(disposing);
    }
}
