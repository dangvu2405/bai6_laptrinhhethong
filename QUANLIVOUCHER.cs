using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;



namespace baitap_lon
{
    public partial class QUANLIVOUCHER : Form
    {
        private DatabaseHelper dbHelper;
        private int id_voucher;
        public QUANLIVOUCHER()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            numericUpDown1.Minimum = 1;
            numericUpDown1.Maximum = 100;
            numericUpDown1.Value = 1; // Giá trị mặc định

            LoadVouchers();
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
        }


        private void LoadVouchers()
        {
            string query = "SELECT VoucherID, Code, Description, DiscountPercentage, MaxDiscountAmount, StartDate, EndDate, IsActive FROM Vouchers";
            try
            {
                using (SqlConnection conn = dbHelper.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dataGridView1.Rows.Clear(); // Xóa tất cả các hàng hiện tại

                            while (reader.Read())
                            {
                                // Thêm dữ liệu vào từng cột
                                dataGridView1.Rows.Add(
                                    reader["VoucherID"].ToString(),
                                    reader["Code"].ToString(),
                                    reader["DiscountPercentage"] != DBNull.Value ? reader["DiscountPercentage"].ToString() : "0",
                                    reader["MaxDiscountAmount"] != DBNull.Value ? reader["MaxDiscountAmount"].ToString() : "0",
                                    Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd"),
                                    Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd"),
                                    (bool)reader["IsActive"] ? "Hoạt động" : "Dừng",
                                    reader["Description"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private void AddVoucher(string code, string description, decimal discountPercentage, decimal maxDiscountAmount, DateTime startDate, DateTime endDate, bool isActive)
        {
            string query = "INSERT INTO Vouchers (Code, Description, DiscountPercentage, MaxDiscountAmount, StartDate, EndDate, IsActive) " +
                           "VALUES (@Code, @Description, @DiscountPercentage, @MaxDiscountAmount, @StartDate, @EndDate, @IsActive)";

            try
            {
                using (SqlConnection connection = dbHelper.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", code);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@DiscountPercentage", discountPercentage);
                        command.Parameters.AddWithValue("@MaxDiscountAmount", maxDiscountAmount);
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);
                        command.Parameters.AddWithValue("@IsActive", isActive);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Voucher added successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private void UpdateVoucher(int voucherId, string code, string description, decimal discountPercentage, decimal maxDiscountAmount, DateTime startDate, DateTime endDate, bool isActive)
        {
            string query = "UPDATE Vouchers SET Code = @Code, Description = @Description, DiscountPercentage = @DiscountPercentage, " +
                           "MaxDiscountAmount = @MaxDiscountAmount, StartDate = @StartDate, EndDate = @EndDate, IsActive = @IsActive " +
                           "WHERE VoucherID = @VoucherID";

            try
            {
                using (SqlConnection connection = dbHelper.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@VoucherID", voucherId);
                        command.Parameters.AddWithValue("@Code", code);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@DiscountPercentage", discountPercentage);
                        command.Parameters.AddWithValue("@MaxDiscountAmount", maxDiscountAmount);
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);
                        command.Parameters.AddWithValue("@IsActive", isActive);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Voucher updated successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }



        private void DeleteVoucher(int voucherId)
        {
            string query = "DELETE FROM Vouchers WHERE VoucherID = @VoucherID";

            try
            {
                using (SqlConnection connection = dbHelper.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@VoucherID", voucherId);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Voucher deleted successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ExportVouchersToExcel()
        {
            try
            {
                // Tạo workbook mới
                var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Vouchers");

                // Thêm tiêu đề
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dataGridView1.Columns[i].HeaderText;
                }

                // Thêm dữ liệu từ DataGridView vào Excel
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    for (int j = 0; j < dataGridView1.Columns.Count; j++)
                    {
                        // Ghi dữ liệu vào từng ô Excel, xử lý giá trị null
                        worksheet.Cell(i + 2, j + 1).Value = dataGridView1.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                    }
                }

                // Đặt hộp thoại lưu file
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = "ds_voucher.xlsx", // Tên file mặc định
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    Title = "Save Voucher Excel File"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Lưu file
                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show("Vouchers exported successfully to Excel!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}");
            }
        }


        private void SearchInDataGridView(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                // Hiển thị lại toàn bộ dữ liệu nếu từ khóa trống
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    row.Visible = true;
                }
                return;
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue; // Bỏ qua hàng mới

                bool match = false;

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(keyword.ToLower()))
                    {
                        match = true;
                        break;
                    }
                }

                row.Visible = match; // Hiển thị hoặc ẩn hàng dựa trên kết quả tìm kiếm
            }
        }

        private void ResetControls()
        {
            // Đặt lại giá trị các điều khiển về mặc định
            textBox2.Text = string.Empty; // Mã Voucher
            textBox4.Text = string.Empty; // Mô tả
            numericUpDown1.Value = numericUpDown1.Minimum; // DiscountPercentage
            textBox3.Text = string.Empty; // MaxDiscountAmount
            dateTimePicker1.Value = DateTime.Now; // StartDate
            dateTimePicker2.Value = DateTime.Now; // EndDate
            checkBox1.Checked = false; // Trạng thái IsActive
            id_voucher = 0; // Đặt lại ID voucher về 0 (mặc định)
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count - 1) // Kiểm tra nếu hàng được chọn không hợp lệ
            {
                ResetControls(); // Đặt lại các điều khiển về trạng thái mặc định
                return;
            }

            var cellValue = dataGridView1.Rows[e.RowIndex].Cells[0].Value;
            if (cellValue == null) // Kiểm tra giá trị null trong cột đầu tiên
            {
                MessageBox.Show("Selected row has no valid data.");
                ResetControls(); // Đặt lại các điều khiển về trạng thái mặc định
                return;
            }

            if (int.TryParse(cellValue.ToString(), out int voucherId))
            {
                id_voucher = voucherId;

                var row = dataGridView1.Rows[e.RowIndex];

                // Gán giá trị từ hàng được chọn vào các điều khiển
                textBox2.Text = row.Cells[1]?.Value?.ToString() ?? string.Empty; // Mã Voucher
                textBox4.Text = row.Cells[7]?.Value?.ToString() ?? string.Empty; // Mô tả

                // Xử lý DiscountPercentage
                if (decimal.TryParse(row.Cells[2]?.Value?.ToString(), out decimal discountPercentage))
                {
                    numericUpDown1.Value = Math.Min(numericUpDown1.Maximum, Math.Max(numericUpDown1.Minimum, discountPercentage));
                }

                textBox3.Text = row.Cells[3]?.Value?.ToString() ?? string.Empty; // MaxDiscountAmount
                dateTimePicker1.Value = DateTime.TryParse(row.Cells[4]?.Value?.ToString(), out DateTime startDate) ? startDate : DateTime.Now; // StartDate
                dateTimePicker2.Value = DateTime.TryParse(row.Cells[5]?.Value?.ToString(), out DateTime endDate) ? endDate : DateTime.Now; // EndDate
                checkBox1.Checked = row.Cells[6]?.Value?.ToString() == "Hoạt động"; // IsActive
            }
            else
            {
                MessageBox.Show("Invalid Voucher ID.");
                ResetControls(); // Đặt lại các điều khiển về trạng thái mặc định
            }


        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Kiểm tra nếu có voucher được chọn
            if (id_voucher > 0)
            {
                // Hiển thị hộp thoại xác nhận
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to delete this voucher?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    // Gọi hàm xóa voucher
                    DeleteVoucher(id_voucher);

                    // Làm mới danh sách voucher
                    LoadVouchers();
                }
            }
            else
            {
                MessageBox.Show("Please select a voucher to delete.");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            LoadVouchers();
            ResetControls();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string code = textBox2.Text.Trim();
            string description = textBox4.Text.Trim();
            decimal discountPercentage = numericUpDown1.Value;
            decimal maxDiscountAmount = string.IsNullOrWhiteSpace(textBox3.Text) ? 0 : Convert.ToDecimal(textBox3.Text);
            DateTime startDate = dateTimePicker1.Value;
            DateTime endDate = dateTimePicker2.Value;
            bool isActive = checkBox1.Checked;

            // Kiểm tra dữ liệu hợp lệ
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Please enter a voucher code.");
                return;
            }

            if (endDate < startDate)
            {
                MessageBox.Show("End date must be greater than or equal to start date.");
                return;
            }

            // Gọi hàm thêm voucher
            AddVoucher(code, description, discountPercentage, maxDiscountAmount, startDate, endDate, isActive);
            LoadVouchers(); // Làm mới danh sách voucher
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                // Lấy ID voucher từ hàng được chọn


                // Lấy dữ liệu từ giao diện
                string code = textBox2.Text.Trim();
                string description = textBox4.Text.Trim();
                decimal discountPercentage = numericUpDown1.Value;
                decimal maxDiscountAmount = string.IsNullOrWhiteSpace(textBox3.Text) ? 0 : Convert.ToDecimal(textBox3.Text);
                DateTime startDate = dateTimePicker1.Value;
                DateTime endDate = dateTimePicker2.Value;
                bool isActive = checkBox1.Checked;

                // Kiểm tra dữ liệu hợp lệ
                if (string.IsNullOrWhiteSpace(code))
                {
                    MessageBox.Show("Please enter a voucher code.");
                    return;
                }

                if (endDate < startDate)
                {
                    MessageBox.Show("End date must be greater than or equal to start date.");
                    return;
                }

                // Gọi hàm sửa voucher
                UpdateVoucher(id_voucher, code, description, discountPercentage, maxDiscountAmount, startDate, endDate, isActive);

                // Làm mới danh sách voucher
                LoadVouchers();
            }
            else
            {
                MessageBox.Show("Please select a voucher to update.");
            }
        }

        private void dataGr(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            ExportVouchersToExcel();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string keyword = textSearch.Text.Trim(); // Lấy từ khóa
            SearchInDataGridView(keyword); // Gọi hàm tìm kiếm
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {

        }
        private void ResetTextBoxes()
        {
            // Đặt lại giá trị các textbox
            textBox2.Text = string.Empty; // Mã Voucher
            textBox3.Text = string.Empty; // MaxDiscountAmount
            textBox4.Text = string.Empty; // Mô tả

            // Đặt lại giá trị cho numericUpDown
            numericUpDown1.Value = numericUpDown1.Minimum; // Đặt về giá trị tối thiểu

            // Đặt lại giá trị cho dateTimePicker
            dateTimePicker1.Value = DateTime.Now; // Ngày bắt đầu
            dateTimePicker2.Value = DateTime.Now; // Ngày kết thúc

            // Đặt lại trạng thái checkbox
            checkBox1.Checked = false; // Trạng thái Voucher (Hoạt động hay không)

            // Đặt lại ID voucher về mặc định

        }
        private void button8_Click(object sender, EventArgs e)
        {
            ResetTextBoxes();

        }
    }

}
