using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

/// <summary>
/// CanSendMessageRow đại diện cho một dòng gửi tin nhắn CAN bao gồm:
/// - 8 ô nhập byte dữ liệu (CAN Data Bytes)
/// - Nút "Send" để gửi ngay 1 lần
/// - Nút "Delete" để xóa dòng khỏi UI
/// - CheckBox để bật chế độ gửi định kỳ
/// - TextBox để nhập khoảng thời gian gửi định kỳ (milliseconds)
/// </summary>
public class CanSendMessageRow
{
    public Panel Container { get; private set; } // Panel chứa toàn bộ dòng này
    private List<TextBox> byteBoxes = new List<TextBox>(); // Danh sách 8 ô nhập dữ liệu

    private CheckBox enableCheckBox;     // CheckBox cho phép bật gửi lặp lại
    private TextBox intervalTextBox;     // Nhập chu kỳ gửi (ms)
    private Timer sendTimer;             // Timer nội bộ cho gửi tự động

    // Sự kiện được gọi khi nhấn nút "Send"
    public event Action<List<byte>> OnSendClicked;

    // Sự kiện được gọi khi nhấn "Delete", để Form1 xử lý xóa dòng
    public event Action<CanSendMessageRow> OnDeleteRequested;

    /// <summary>
    /// Hàm khởi tạo dòng gửi dữ liệu CAN
    /// </summary>
    /// <param name="location">Vị trí của dòng trên UI (được Form1 gán)</param>
    public CanSendMessageRow(Point location)
    {
        // Tạo Panel đại diện cho dòng hiện tại
        Container = new Panel
        {
            Size = new Size(650, 30),
            Location = location
        };

        // Tạo 8 ô TextBox để nhập dữ liệu CAN (từ Byte 0 đến Byte 7)
        for (int i = 0; i < 8; i++)
        {
            TextBox txtByte = new TextBox
            {
                Name = $"ByteCAN{i + 1}",              // Đặt tên theo thứ tự
                Size = new Size(30, 20),               // Kích thước ô nhập
                Location = new Point(i * 35, 5),       // Cách đều nhau
                MaxLength = 2,                         // 2 ký tự hex: 00 → FF
                Text = "00",                           // Mặc định
                CharacterCasing = CharacterCasing.Upper,
                TextAlign = HorizontalAlignment.Center
            };

            // Chỉ cho nhập ký tự hex
            txtByte.KeyPress += OnlyHex_KeyPress;

            // Tự động format lại thành "00" nếu bỏ trống hoặc chỉ 1 ký tự
            txtByte.Leave += FormatHex_Leave;

            // Thêm vào danh sách và Panel
            byteBoxes.Add(txtByte);
            Container.Controls.Add(txtByte);
        }

        // ===== NÚT SEND (Gửi dữ liệu CAN ngay lập tức) =====
        Button sendBtn = new Button
        {
            Text = "Send",
            Size = new Size(60, 23),
            Location = new Point(8 * 35 + 10, 3)
        };

        // Khi click sẽ thực hiện gửi
        sendBtn.Click += (s, e) => TriggerSend();
        Container.Controls.Add(sendBtn);

        // ===== NÚT DELETE (Xóa dòng hiện tại) =====
        Button deleteBtn = new Button
        {
            Text = "Delete",
            Size = new Size(60, 23),
            Location = new Point(8 * 35 + 80, 3)
        };

        // Khi click sẽ yêu cầu Form1 xóa dòng này
        deleteBtn.Click += (s, e) => OnDeleteRequested?.Invoke(this);
        Container.Controls.Add(deleteBtn);

        // ===== CHECKBOX BẬT GỬI ĐỊNH KỲ =====
        enableCheckBox = new CheckBox
        {
            Location = new Point(8 * 35 + 150, 7),
            AutoSize = true
        };

        // Khi tick hoặc bỏ tick checkbox
        enableCheckBox.CheckedChanged += EnableCheckBox_CheckedChanged;
        Container.Controls.Add(enableCheckBox);

        // ===== TEXTBOX NHẬP TIME (ms) =====
        intervalTextBox = new TextBox
        {
            Size = new Size(50, 20),
            Location = new Point(8 * 35 + 180, 5),
            Text = "1000", // Mặc định: 1000ms = 1s
            TextAlign = HorizontalAlignment.Center
        };

        // Chỉ cho nhập số (mili giây)
        intervalTextBox.KeyPress += (s, e) =>
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                e.Handled = true;
        };
        Container.Controls.Add(intervalTextBox);

        // ===== TIMER để gửi dữ liệu định kỳ =====
        sendTimer = new Timer();
        sendTimer.Tick += (s, e) => TriggerSend(); // Mỗi lần tick thì gửi
    }

    /// <summary>
    /// Xử lý khi checkbox bật gửi định kỳ được tick hoặc bỏ tick
    /// </summary>
    private void EnableCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        if (enableCheckBox.Checked)
        {
            // Kiểm tra giá trị thời gian nhập có hợp lệ không
            if (int.TryParse(intervalTextBox.Text, out int intervalMs) && intervalMs > 0)
            {
                sendTimer.Interval = intervalMs;
                sendTimer.Start(); // Bắt đầu gửi định kỳ
            }
            else
            {
                MessageBox.Show("Invalid interval (ms)");
                enableCheckBox.Checked = false; // Tự bỏ tick nếu sai
            }
        }
        else
        {
            sendTimer.Stop(); // Dừng gửi nếu bỏ tick
        }
    }

    /// <summary>
    /// Giới hạn chỉ cho nhập ký tự hex: 0–9, A–F
    /// </summary>
    private void OnlyHex_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (!Uri.IsHexDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            e.Handled = true; // Bỏ ký tự không hợp lệ
    }

    /// <summary>
    /// Khi rời khỏi ô, đảm bảo dữ liệu là 2 ký tự hex (ví dụ: "A" → "0A")
    /// </summary>
    private void FormatHex_Leave(object sender, EventArgs e)
    {
        var txt = sender as TextBox;
        if (!string.IsNullOrEmpty(txt.Text))
        {
            if (txt.Text.Length == 1)
                txt.Text = "0" + txt.Text;
            else if (txt.Text.Length == 0)
                txt.Text = "00";
        }
    }

    /// <summary>
    /// Gửi dữ liệu CAN (gọi sự kiện OnSendClicked kèm dữ liệu)
    /// </summary>
    private void TriggerSend()
    {
        List<byte> data = new List<byte>();

        // Đọc từng ô TextBox và chuyển sang byte
        foreach (var txt in byteBoxes)
        {
            try
            {
                data.Add(Convert.ToByte(txt.Text, 16));
            }
            catch
            {
                MessageBox.Show($"Invalid hex value: {txt.Text}");
                return;
            }
        }

        // Gọi sự kiện gửi lên Form1 hoặc nơi xử lý CAN thật sự
        OnSendClicked?.Invoke(data);
    }
}
