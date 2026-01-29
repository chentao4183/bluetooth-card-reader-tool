using System.Drawing;
using System.Windows.Forms;
using BluetoothCardReaderTool.Models;
using BluetoothCardReaderTool.Core;

namespace BluetoothCardReaderTool.UI;

/// <summary>
/// 字段编辑对话框
/// </summary>
public class FieldEditDialog : Form
{
    private readonly OcrService _ocrService;
    private readonly List<OcrField> _existingFields;
    private OcrField _field;

    // 控件
    private TextBox _txtName = null!;
    private TextBox _txtParamName = null!;
    private CheckBox _chkEnabled = null!;
    private TextBox _txtDefaultValue = null!;
    private TextBox _txtExample = null!;
    private NumericUpDown _numX = null!;
    private NumericUpDown _numY = null!;
    private NumericUpDown _numWidth = null!;
    private NumericUpDown _numHeight = null!;
    private Button _btnLocateRegion = null!;
    private Button _btnPreviewRegion = null!;
    private Button _btnTestOcr = null!;
    private TextBox _txtOcrResult = null!;
    private Label _lblConfidence = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    public FieldEditDialog(OcrService ocrService, List<OcrField> existingFields, OcrField? field = null)
    {
        _ocrService = ocrService;
        _existingFields = existingFields;
        _field = field ?? new OcrField();

        InitializeComponent();
        LoadFieldData();
    }

    private void InitializeComponent()
    {
        this.Text = _field.Name == "" ? "新增字段" : "编辑字段";
        this.Size = new Size(500, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("微软雅黑", 9);

        int yPos = 20;

        // 字段名
        var lblName = new Label
        {
            Text = "字段名:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(lblName);

        _txtName = new TextBox
        {
            Location = new Point(110, yPos),
            Size = new Size(350, 25)
        };
        this.Controls.Add(_txtName);

        yPos += 35;

        // 参数名
        var lblParamName = new Label
        {
            Text = "参数名:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(lblParamName);

        _txtParamName = new TextBox
        {
            Location = new Point(110, yPos),
            Size = new Size(350, 25)
        };
        this.Controls.Add(_txtParamName);

        yPos += 35;

        // 启用
        _chkEnabled = new CheckBox
        {
            Text = "启用此字段",
            Location = new Point(110, yPos),
            Size = new Size(150, 25),
            Checked = true
        };
        this.Controls.Add(_chkEnabled);

        yPos += 35;

        // 默认值
        var lblDefaultValue = new Label
        {
            Text = "默认值:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(lblDefaultValue);

        _txtDefaultValue = new TextBox
        {
            Location = new Point(110, yPos),
            Size = new Size(350, 25)
        };
        this.Controls.Add(_txtDefaultValue);

        yPos += 35;

        // 识别示例
        var lblExample = new Label
        {
            Text = "识别示例:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(lblExample);

        _txtExample = new TextBox
        {
            Location = new Point(110, yPos),
            Size = new Size(350, 25)
        };
        this.Controls.Add(_txtExample);

        yPos += 45;

        // 识别区域组
        var grpRegion = new GroupBox
        {
            Text = "识别区域",
            Location = new Point(20, yPos),
            Size = new Size(450, 120)
        };
        this.Controls.Add(grpRegion);

        // X
        var lblX = new Label
        {
            Text = "X:",
            Location = new Point(15, 30),
            Size = new Size(30, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpRegion.Controls.Add(lblX);

        _numX = new NumericUpDown
        {
            Location = new Point(50, 28),
            Size = new Size(80, 25),
            Maximum = 10000,
            Minimum = 0
        };
        grpRegion.Controls.Add(_numX);

        // Y
        var lblY = new Label
        {
            Text = "Y:",
            Location = new Point(145, 30),
            Size = new Size(30, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpRegion.Controls.Add(lblY);

        _numY = new NumericUpDown
        {
            Location = new Point(180, 28),
            Size = new Size(80, 25),
            Maximum = 10000,
            Minimum = 0
        };
        grpRegion.Controls.Add(_numY);

        // 宽
        var lblWidth = new Label
        {
            Text = "宽:",
            Location = new Point(15, 65),
            Size = new Size(30, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpRegion.Controls.Add(lblWidth);

        _numWidth = new NumericUpDown
        {
            Location = new Point(50, 63),
            Size = new Size(80, 25),
            Maximum = 10000,
            Minimum = 1,
            Value = 100
        };
        grpRegion.Controls.Add(_numWidth);

        // 高
        var lblHeight = new Label
        {
            Text = "高:",
            Location = new Point(145, 65),
            Size = new Size(30, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpRegion.Controls.Add(lblHeight);

        _numHeight = new NumericUpDown
        {
            Location = new Point(180, 63),
            Size = new Size(80, 25),
            Maximum = 10000,
            Minimum = 1,
            Value = 30
        };
        grpRegion.Controls.Add(_numHeight);

        // 定位区域按钮
        _btnLocateRegion = new Button
        {
            Text = "定位区域",
            Location = new Point(280, 28),
            Size = new Size(80, 30)
        };
        _btnLocateRegion.Click += BtnLocateRegion_Click;
        grpRegion.Controls.Add(_btnLocateRegion);

        // 预览区域按钮
        _btnPreviewRegion = new Button
        {
            Text = "预览区域",
            Location = new Point(280, 63),
            Size = new Size(80, 30)
        };
        _btnPreviewRegion.Click += BtnPreviewRegion_Click;
        grpRegion.Controls.Add(_btnPreviewRegion);

        yPos += 130;

        // 测试识别按钮
        _btnTestOcr = new Button
        {
            Text = "测试识别",
            Location = new Point(20, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnTestOcr.Click += BtnTestOcr_Click;
        this.Controls.Add(_btnTestOcr);

        yPos += 45;

        // 识别结果
        var lblOcrResult = new Label
        {
            Text = "识别结果:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(lblOcrResult);

        _txtOcrResult = new TextBox
        {
            Location = new Point(110, yPos),
            Size = new Size(350, 25),
            ReadOnly = true,
            BackColor = Color.LightYellow
        };
        this.Controls.Add(_txtOcrResult);

        yPos += 35;

        // 置信度
        var lblConfidenceLabel = new Label
        {
            Text = "置信度:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(lblConfidenceLabel);

        _lblConfidence = new Label
        {
            Text = "0.00%",
            Location = new Point(110, yPos),
            Size = new Size(100, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("微软雅黑", 10, FontStyle.Bold)
        };
        this.Controls.Add(_lblConfidence);

        yPos += 45;

        // 确定/取消按钮
        _btnOk = new Button
        {
            Text = "确定",
            Location = new Point(270, yPos),
            Size = new Size(90, 35),
            DialogResult = DialogResult.OK
        };
        _btnOk.Click += BtnOk_Click;
        this.Controls.Add(_btnOk);

        _btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(370, yPos),
            Size = new Size(90, 35),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(_btnCancel);

        this.AcceptButton = _btnOk;
        this.CancelButton = _btnCancel;
    }

    private void LoadFieldData()
    {
        _txtName.Text = _field.Name;
        _txtParamName.Text = _field.ParamName;
        _chkEnabled.Checked = _field.Enabled;
        _txtDefaultValue.Text = _field.DefaultValue;
        _txtExample.Text = _field.Example;

        if (_field.Region != null)
        {
            _numX.Value = _field.Region.X;
            _numY.Value = _field.Region.Y;
            _numWidth.Value = _field.Region.Width;
            _numHeight.Value = _field.Region.Height;
        }
    }

    private void BtnLocateRegion_Click(object? sender, EventArgs e)
    {
        try
        {
            // 隐藏当前窗口
            this.Hide();

            // 等待一下让窗口完全隐藏
            System.Threading.Thread.Sleep(200);

            // 打开截图工具
            using var screenshotForm = new ScreenshotForm();
            if (screenshotForm.ShowDialog() == DialogResult.OK)
            {
                var region = screenshotForm.GetSelectedRegion();
                _numX.Value = region.X;
                _numY.Value = region.Y;
                _numWidth.Value = region.Width;
                _numHeight.Value = region.Height;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"定位区域失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // 恢复显示
            this.Show();
        }
    }

    private void BtnPreviewRegion_Click(object? sender, EventArgs e)
    {
        try
        {
            // 截取当前屏幕
            using var screenshot = _ocrService.CaptureScreen();

            // 创建预览窗口
            var previewForm = new Form
            {
                Text = "区域预览",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent
            };

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // 在截图上绘制选择框
            using var graphics = Graphics.FromImage(screenshot);
            using var pen = new Pen(Color.Red, 3);
            var region = new Rectangle((int)_numX.Value, (int)_numY.Value, (int)_numWidth.Value, (int)_numHeight.Value);
            graphics.DrawRectangle(pen, region);

            pictureBox.Image = (Bitmap)screenshot.Clone();
            previewForm.Controls.Add(pictureBox);
            previewForm.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"预览区域失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnTestOcr_Click(object? sender, EventArgs e)
    {
        try
        {
            // 验证区域
            if (_numWidth.Value == 0 || _numHeight.Value == 0)
            {
                MessageBox.Show("请先定位识别区域", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnTestOcr.Enabled = false;
            _btnTestOcr.Text = "识别中...";
            _txtOcrResult.Text = "正在识别...";
            _lblConfidence.Text = "0.00%";
            Application.DoEvents();

            // 截取屏幕
            using var screenshot = _ocrService.CaptureScreen();

            // 创建区域
            var region = new OcrRegion
            {
                X = (int)_numX.Value,
                Y = (int)_numY.Value,
                Width = (int)_numWidth.Value,
                Height = (int)_numHeight.Value
            };

            // 识别
            var result = _ocrService.RecognizeRegion(screenshot, region);

            if (result.Success)
            {
                _txtOcrResult.Text = string.IsNullOrEmpty(result.Text) ? "(未识别到文本)" : result.Text;
                _lblConfidence.Text = $"{result.Confidence:P2}";
                _lblConfidence.ForeColor = result.Confidence >= 0.85 ? Color.Green : Color.Orange;
            }
            else
            {
                _txtOcrResult.Text = result.ErrorMessage;
                _lblConfidence.Text = "0.00%";
                _lblConfidence.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"测试识别失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTestOcr.Enabled = true;
            _btnTestOcr.Text = "测试识别";
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("请输入字段名", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            this.DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtParamName.Text))
        {
            MessageBox.Show("请输入参数名", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtParamName.Focus();
            this.DialogResult = DialogResult.None;
            return;
        }

        // 验证参数名格式（只能包含字母、数字、下划线）
        if (!System.Text.RegularExpressions.Regex.IsMatch(_txtParamName.Text, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            MessageBox.Show("参数名只能包含字母、数字、下划线，且不能以数字开头", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtParamName.Focus();
            this.DialogResult = DialogResult.None;
            return;
        }

        // 检查参数名是否重复
        var duplicate = _existingFields.FirstOrDefault(f => f.ParamName == _txtParamName.Text && f != _field);
        if (duplicate != null)
        {
            MessageBox.Show($"参数名 '{_txtParamName.Text}' 已被字段 '{duplicate.Name}' 使用", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtParamName.Focus();
            this.DialogResult = DialogResult.None;
            return;
        }

        // 保存数据
        _field.Name = _txtName.Text.Trim();
        _field.ParamName = _txtParamName.Text.Trim();
        _field.Enabled = _chkEnabled.Checked;
        _field.DefaultValue = _txtDefaultValue.Text.Trim();
        _field.Example = _txtExample.Text.Trim();
        _field.Region = new OcrRegion
        {
            X = (int)_numX.Value,
            Y = (int)_numY.Value,
            Width = (int)_numWidth.Value,
            Height = (int)_numHeight.Value
        };
    }

    public OcrField GetField()
    {
        return _field;
    }
}
