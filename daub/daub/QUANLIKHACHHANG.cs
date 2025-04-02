using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Office.Interop.Excel;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace baitap_lon
{

    public partial class QUANLIKHACHHANG : Form
    {
        private DatabaseHelper dbHelper;
        private int id_customer;
        public QUANLIKHACHHANG()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            LoadDataManually();
            dataGridView1.ReadOnly = true; // Ngăn chỉnh sửa
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Chọn toàn bộ hàng
            dataGridView1.MultiSelect = false; // Chỉ cho phép chọn một hàng

        }

        private void LoadDataManually()
        {
            string query = "SELECT CustomerID, Name, Email, Phone, Address FROM Customers";

            try
            {
                using (SqlConnection conn = dbHelper.GetConnection())
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        MessageBox.Show("Kết nối thất bại !");
                        return;
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                MessageBox.Show("Không có bảng nào trong database.");
                                return;
                            }

                            // Xóa các dòng cũ
                            dataGridView1.Rows.Clear();

                            // Đọc dữ liệu từ SqlDataReader và thêm vào DataGridView
                            while (reader.Read())
                            {
                                dataGridView1.Rows.Add(
                                    reader["CustomerID"].ToString(),
                                    reader["Name"].ToString(),
                                    reader["Email"].ToString(),
                                    reader["Phone"].ToString(),
                                    reader["Address"].ToString()
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


            private void ExportToExcel()
            {
                try
                {
                    var workbook = new ClosedXML.Excel.XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Customers");

                    // Thêm tiêu đề
                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = dataGridView1.Columns[i].HeaderText;
                    }

                    // Thêm dữ liệu
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        {
                            worksheet.Cell(i + 2, j + 1).Value = dataGridView1.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                        }
                    }

                    // Lưu file
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel files (*.xlsx)|*.xlsx",
                        Title = "Save an Excel File"
                    };

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        workbook.SaveAs(saveFileDialog.FileName);
                        MessageBox.Show("Thành công xuất dữ liệu.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }




        private void AddCustomer(string name, string email, string phone, string address)
        {
            string query = "INSERT INTO Customers (Name, Email, Phone, Address) VALUES (@Name, @Email, @Phone, @Address)";

            try
            {
                // Sử dụng khối using để đảm bảo kết nối được đóng
                using (SqlConnection connection = dbHelper.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@Address", address);

                        command.ExecuteNonQuery(); // Thực thi truy vấn
                        MessageBox.Show("Thêm khách hàng thành công.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi
                MessageBox.Show("Lỗi: " + ex.Message);
            }

        }



        private void UpdateCustomer(int customerId, string name, string email, string phone, string address)
        {

            string query = "UPDATE Customers SET Name = @Name, Email = @Email, Phone = @Phone, Address = @Address WHERE CustomerID = @CustomerID";

            using (SqlConnection connection = dbHelper.GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Address", address);


                    command.ExecuteNonQuery();
                    MessageBox.Show("Cập nhập khách hàng thành công .");
                }
            }
        }

        private void DeleteCustomer(int customerId)
        {
            string connectionString = "Server=VU;Database=ShoeStoreDB;User Id=sa;Password=dangvu123;";
            string query = "DELETE FROM Customers WHERE CustomerID = @CustomerID";

            using (SqlConnection connection = dbHelper.GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);


                    command.ExecuteNonQuery();
                    MessageBox.Show("Xóa khách hàng thành công.");
                }
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
            // Đặt giá trị mặc định cho các TextBox
            txtName.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtPhone.Text = string.Empty;
            txtAddress.Text = string.Empty;

            // Đặt ID khách hàng về giá trị mặc định
            id_customer = 0;

            // Hiển thị thông báo trạng thái
            
        }
        public bool IsValidEmail(string email)
        {
            // Biểu thức chính quy để kiểm tra email hợp lệ
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(email);
        }
        public bool IsValidPhoneNumber(string phone)
        {
            // Biểu thức chính quy để kiểm tra số điện thoại Việt Nam
            string pattern = @"^(0[3|5|7|8|9])\d{8}$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(phone);
        }
        public bool IsValidString(string input)
        {
            return !string.IsNullOrWhiteSpace(input); // Kiểm tra chuỗi không trống hoặc chỉ có khoảng trắng
        }

        public bool IsValidInteger(string input)
        {
            return int.TryParse(input, out _); // Trả về true nếu có thể chuyển đổi thành int
        }

        public bool IsValidDecimal(string input)
        {
            return decimal.TryParse(input, out _); // Trả về true nếu có thể chuyển đổi thành decimal
        }

        public bool IsValidMinLength(string input, int minLength)
        {
            return input.Length >= minLength;
        }

        private bool ValidateForm(string name, string email, string phone, string address)
        {
            // Kiểm tra tên không rỗng
            if (!IsValidString(name))
            {
                MessageBox.Show("Tên không được để trống.");
                return false;
            }

            // Kiểm tra email hợp lệ
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Email không hợp lệ.");
                return false;
            }

            // Kiểm tra số điện thoại hợp lệ
            if (!IsValidPhoneNumber(phone))
            {
                MessageBox.Show("Số điện thoại không hợp lệ.");
                return false;
            }

            // Kiểm tra địa chỉ không rỗng
            if (!IsValidString(address))
            {
                MessageBox.Show("Địa chỉ không được để trống.");
                return false;
            }

            return true;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Lấy dữ liệu từ các TextBox
            string name = txtName.Text;
            string email = txtEmail.Text;
            string phone = txtPhone.Text;
            string address = txtAddress.Text;

            // Hiển thị dữ liệu ra MessageBox (hoặc có thể xử lý thêm)
            MessageBox.Show($"Name: {name}\nEmail: {email}\nPhone: {phone}\nAddress: {address}");
            if (ValidateForm(name, email, phone, address))
            {
                // Gọi hàm AddCustomer hoặc UpdateCustomer
                AddCustomer(name, email, phone, address);
                LoadDataManually();
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //int customerId = int.Parse(txtCustomerId.Text);
            string name = txtName.Text;
            string email = txtEmail.Text;
            string phone = txtPhone.Text;
            string address = txtAddress.Text;

            
            if (ValidateForm(name, email, phone, address))
            {
                // Gọi hàm AddCustomer hoặc UpdateCustomer
                UpdateCustomer(id_customer, name, email, phone, address);
                LoadDataManually();
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //int customerId = int.Parse(txtCustomerId.Text);
            DeleteCustomer(id_customer);
            LoadDataManually();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex == dataGridView1.NewRowIndex) // Không chọn hàng hợp lệ
            {
                ResetControls(); // Gọi hàm đặt lại khi không chọn gì
                return;
            }

            var cellValue = dataGridView1.Rows[e.RowIndex].Cells[0].Value;

            if (cellValue != null)
            {
                id_customer = Convert.ToInt32(cellValue);
                txtName.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? string.Empty;
                txtEmail.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? string.Empty;
                txtPhone.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value?.ToString() ?? string.Empty;
                txtAddress.Text = dataGridView1.Rows[e.RowIndex].Cells[4].Value?.ToString() ?? string.Empty;
            }
            else
            {
                
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string keyword = textSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(keyword))
            {
                // Hiển thị lại toàn bộ dữ liệu nếu từ khóa trống
                LoadDataManually();
                return;
            }

            // Lọc dữ liệu bằng cách kiểm tra từng hàng
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Bỏ qua hàng mới (NewRow)
                if (row.IsNewRow)
                {
                    continue;
                }

                // Kiểm tra nếu giá trị trong bất kỳ cột nào khớp với từ khóa
                bool match = false;

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(keyword))
                    {
                        match = true;
                        break;
                    }
                }

                // Ẩn hoặc hiển thị hàng dựa trên kết quả tìm kiếm
                row.Visible = match;
            }


        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            LoadDataManually();
            ResetControls();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ExportToExcel();
        }
    }

}
