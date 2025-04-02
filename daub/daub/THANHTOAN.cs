using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
namespace baitap_lon
{
    public partial class THANHTOAN : Form
    {
        private string customerName;
        private string customerEmail;
        private string customerPhone;
        private string customerAddress;
        private long tongtien;
        private long tongtienLong;
        private long tongtienShort;
        private DataTable orderDetails;
        private DatabaseHelper dbHelper;
        public THANHTOAN(string name, string email, string phone, string address, long thanhtoan, DataTable orderDetailsTable)
        {
            InitializeComponent();
            customerName = name;
            customerEmail = email;
            customerPhone = phone;
            customerAddress = address;
            tongtien = thanhtoan;
            orderDetails = orderDetailsTable;
            dbHelper = new DatabaseHelper();
        }
        private void SetTextBoxValues()
        {
            textBox1.Text = customerName;
            textBox2.Text = customerEmail;
            textBox3.Text = customerPhone;
            textBox4.Text = customerAddress;
        }
        private void MakeTextBoxesReadOnly()
        {
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox3.ReadOnly = true;
            textBox4.ReadOnly = true;
        }
        private void MakeTextBoxesEditable()
        {
            textBox1.ReadOnly = false;
            textBox2.ReadOnly = false;
            textBox3.ReadOnly = false;
            textBox4.ReadOnly = false;
        }

        private void ProcessPayment(string voucherCode)
        {
            // Áp dụng voucher nếu có
            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                long discount = ApplyVoucher(voucherCode, tongtienLong);
                if (discount > 0)
                {
                    tongtienShort = tongtienLong - discount;
                }
            }
            else
            {
                tongtienShort = tongtienLong;
            }

            // Bắt đầu giao dịch
            using (SqlConnection con = dbHelper.GetConnection())
            {
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Thêm hoặc lấy CustomerID
                        int customerId = AddOrGetCustomerId(con, transaction);

                        if (customerId == -1)
                        {
                            throw new Exception("Không thể lấy hoặc tạo CustomerID.");
                        }

                        // 2. Thêm đơn hàng
                        int orderId = AddOrder(con, transaction, customerId);

                        if (orderId == -1)
                        {
                            throw new Exception("Không thể tạo đơn hàng.");
                        }

                        // 3. Thêm chi tiết đơn hàng và cập nhật kho hàng
                        foreach (DataRow row in orderDetails.Rows)
                        {
                            int productId = Convert.ToInt32(row["ProductID"]);
                            int quantity = Convert.ToInt32(row["Quantity"]);
                            decimal unitPrice = Convert.ToDecimal(row["Price"]); // Đảm bảo rằng cột "Price" tồn tại

                            bool isOrderDetailAdded = AddOrderDetail(con, transaction, orderId, productId, quantity, unitPrice);

                            if (!isOrderDetailAdded)
                            {
                                throw new Exception("Không thể thêm chi tiết đơn hàng.");
                            }

                            // 4. Cập nhật kho hàng
                            bool isInventoryUpdated = UpdateInventory(con, transaction, productId, quantity);

                            if (!isInventoryUpdated)
                            {
                                throw new Exception($"Không thể cập nhật kho hàng cho sản phẩm ID: {productId}.");
                            }
                        }

                        // 5. Tạo hóa đơn
                        int invoiceId = AddInvoice(con, transaction, orderId);

                        if (invoiceId == -1)
                        {
                            throw new Exception("Không thể tạo hóa đơn.");
                        }

                        // 6. Ghi nhận thanh toán
                        string paymentMethod = "Cash"; // Hoặc lấy từ giao diện người dùng, ví dụ: ComboBox
                        bool isPaymentAdded = AddPayment(con, transaction, invoiceId, tongtienShort, paymentMethod);

                        if (!isPaymentAdded)
                        {
                            throw new Exception("Không thể ghi nhận thanh toán.");
                        }

                        // Cam kết giao dịch
                        transaction.Commit();

                        MessageBox.Show("Thanh toán thành công và hóa đơn đã được tạo.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                            // Xử lý lỗi khi rollback nếu cần
                        }

                        MessageBox.Show($"Lỗi khi thanh toán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }




        private int AddOrGetCustomerId(SqlConnection con, SqlTransaction transaction)
        {
            // Kiểm tra khách hàng đã tồn tại chưa dựa trên Email
            string checkQuery = "SELECT CustomerID FROM Customers WHERE Email = @Email";
            using (SqlCommand cmd = new SqlCommand(checkQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@Email", customerEmail);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
            }

            // Nếu chưa tồn tại, thêm khách hàng mới
            string insertQuery = @"INSERT INTO Customers (Name, Email, Phone, Address) 
                                   VALUES (@Name, @Email, @Phone, @Address);
                                   SELECT CAST(scope_identity() AS int);";
            using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@Name", customerName);
                cmd.Parameters.AddWithValue("@Email", customerEmail);
                cmd.Parameters.AddWithValue("@Phone", customerPhone);
                cmd.Parameters.AddWithValue("@Address", customerAddress);

                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    return -1; // Lỗi khi thêm khách hàng
                }
            }
        }


        private int AddOrder(SqlConnection con, SqlTransaction transaction, int customerId)
        {
            string insertOrderQuery = @"INSERT INTO Orders (CustomerID, OrderDate, Status, TotalAmount) 
                                        VALUES (@CustomerID, @OrderDate, @Status, @TotalAmount);
                                        SELECT CAST(scope_identity() AS int);";

            using (SqlCommand cmd = new SqlCommand(insertOrderQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                cmd.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Status", "Completed"); // Hoặc trạng thái khác tùy theo logic
                cmd.Parameters.AddWithValue("@TotalAmount", tongtienShort);

                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    return -1; // Lỗi khi thêm đơn hàng
                }
            }
        }

        // Hàm thêm chi tiết đơn hàng vào bảng OrderDetails
        private bool AddOrderDetail(SqlConnection con, SqlTransaction transaction, int orderId, int productId, int quantity, decimal unitPrice)
        {
            string insertOrderDetailQuery = @"INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice) 
                                              VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice);";

            using (SqlCommand cmd = new SqlCommand(insertOrderDetailQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                cmd.Parameters.AddWithValue("@ProductID", productId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);

                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        // Hàm thêm hóa đơn vào bảng Invoices và trả về InvoiceID
        private int AddInvoice(SqlConnection con, SqlTransaction transaction, int orderId)
        {
            string insertInvoiceQuery = @"INSERT INTO Invoices (OrderID, InvoiceDate, Amount) 
                                          VALUES (@OrderID, @InvoiceDate, @Amount);
                                          SELECT CAST(scope_identity() AS int);";

            using (SqlCommand cmd = new SqlCommand(insertInvoiceQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                cmd.Parameters.AddWithValue("@InvoiceDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Amount", tongtienShort);

                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    return -1; // Lỗi khi thêm hóa đơn
                }
            }
        }

        // Hàm ghi nhận thanh toán vào bảng Payments
        private bool AddPayment(SqlConnection con, SqlTransaction transaction, int invoiceId, decimal amount, string paymentMethod)
        {
            string insertPaymentQuery = @"INSERT INTO Payments (InvoiceID, PaymentDate, Amount, PaymentMethod) 
                                          VALUES (@InvoiceID, @PaymentDate, @Amount, @PaymentMethod);";

            using (SqlCommand cmd = new SqlCommand(insertPaymentQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@InvoiceID", invoiceId);
                cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);

                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

  


        public long ApplyVoucher(string voucherCode, long totalAmount)
        {
            try
            {
                using (SqlConnection con = dbHelper.GetConnection())
                {
                    // Kiểm tra trạng thái kết nối trước khi mở
                    if (con.State != System.Data.ConnectionState.Open)
                    {
                        con.Open();
                    }

                    // Truy vấn lấy thông tin voucher
                    string query = @"
                SELECT DiscountPercentage, MaxDiscountAmount, StartDate, EndDate, IsActive 
                FROM Vouchers 
                WHERE Code = @Code";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Code", voucherCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal discountPercentage = reader.GetDecimal(0);
                                decimal maxDiscountAmount = reader.GetDecimal(1);
                                DateTime startDate = reader.GetDateTime(2);
                                DateTime endDate = reader.GetDateTime(3);
                                bool isActive = reader.GetBoolean(4);

                                if (!isActive)
                                {
                                    MessageBox.Show("Voucher không còn hoạt động.");
                                    return 0;
                                }

                                if (DateTime.Now < startDate || DateTime.Now > endDate)
                                {
                                    MessageBox.Show("Voucher không nằm trong thời gian áp dụng.");
                                    return 0;
                                }

                                decimal totalAmountDecimal = (decimal)totalAmount;
                                decimal discountAmountDecimal = Math.Min((totalAmountDecimal * discountPercentage) / 100, maxDiscountAmount);

                                // Chuyển đổi từ decimal sang long
                                long discountAmount = (long)Math.Floor(discountAmountDecimal);

                                MessageBox.Show($"Số tiền được giảm: {discountAmount} VND");
                                return discountAmount;
                            }
                            else
                            {
                                MessageBox.Show("Voucher không tồn tại.");
                                return 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi áp dụng voucher: " + ex.Message);
                return 0;
            }
        }

        // Hàm cập nhật kho hàng
        private bool UpdateInventory(SqlConnection con, SqlTransaction transaction, int productId, int quantity)
        {
            try
            {
                // 1. Kiểm tra xem kho hàng có đủ số lượng cần giảm không
                string checkProductStockQuery = @"SELECT StockQuantity FROM Products WHERE ProductID = @ProductID";
                using (SqlCommand cmdCheck = new SqlCommand(checkProductStockQuery, con, transaction))
                {
                    cmdCheck.Parameters.AddWithValue("@ProductID", productId);
                    object result = cmdCheck.ExecuteScalar();
                    if (result == null)
                    {
                        MessageBox.Show($"Không tìm thấy sản phẩm với ID: {productId} trong kho hàng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int currentStock = Convert.ToInt32(result);
                    if (currentStock < quantity)
                    {
                        MessageBox.Show($"Số lượng sản phẩm với ID: {productId} trong kho không đủ.\nSố lượng hiện tại: {currentStock}, Số lượng cần giảm: {quantity}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                // 2. Giảm số lượng sản phẩm trong bảng Products
                string updateProductsQuery = @"UPDATE Products
                                        SET StockQuantity = StockQuantity - @Quantity
                                        WHERE ProductID = @ProductID;";
                using (SqlCommand cmdUpdateProducts = new SqlCommand(updateProductsQuery, con, transaction))
                {
                    cmdUpdateProducts.Parameters.AddWithValue("@Quantity", quantity);
                    cmdUpdateProducts.Parameters.AddWithValue("@ProductID", productId);

                    int rowsAffectedProducts = cmdUpdateProducts.ExecuteNonQuery();
                    if (rowsAffectedProducts <= 0)
                    {
                        MessageBox.Show($"Không thể cập nhật StockQuantity cho sản phẩm ID: {productId}.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                // Nếu tất cả các bước trên thành công
                return true;
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có
                MessageBox.Show($"Lỗi khi cập nhật kho hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }




        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }


        private void button1_Click(object sender, EventArgs e)
        {

            string voucherCode = textBox5.Text.Trim(); // Lấy mã voucher từ textBox5

            // Xác nhận thanh toán với người dùng
            var confirmResult = MessageBox.Show("Bạn có chắc chắn muốn thanh toán?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmResult == DialogResult.Yes)
            {
                ProcessPayment(voucherCode);
            }
        }

        private void THANHTOAN_Load(object sender, EventArgs e)
        {
            SetTextBoxValues();
            MakeTextBoxesReadOnly();

            // Thiết lập DataSource cho DataGridView
            dataGridViewPaymentDetails.DataSource = orderDetails;
            dataGridViewPaymentDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Thiết lập các thuộc tính để ngăn người dùng thao tác
            dataGridViewPaymentDetails.ReadOnly = true; // Ngăn chỉnh sửa
            dataGridViewPaymentDetails.AllowUserToAddRows = false; // Ngăn thêm hàng
            dataGridViewPaymentDetails.AllowUserToDeleteRows = false; // Ngăn xóa hàng
            dataGridViewPaymentDetails.AllowUserToOrderColumns = false; // Ngăn thay đổi thứ tự cột
            dataGridViewPaymentDetails.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Chọn toàn bộ hàng khi chọn một ô

            // Ẩn cột cuối cùng nếu có ít nhất một cột
            if (dataGridViewPaymentDetails.Columns.Count > 0)
            {
                // Lấy chỉ số của cột cuối cùng
                int lastColumnIndex = dataGridViewPaymentDetails.Columns.Count - 1;
                dataGridViewPaymentDetails.Columns[lastColumnIndex].Visible = false;
            }

            // Cập nhật tổng tiền
            tongtienLong = tongtien;
            tongtienShort = tongtien;
            label11.Text =  $"{tongtienShort} VND";

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            // Kiểm tra nếu mã voucher đã được nhập đầy đủ
            if (!string.IsNullOrWhiteSpace(textBox5.Text))
            {
                string voucherCode = textBox5.Text.Trim();

                // Áp dụng voucher và tính tiền giảm
                long discount = ApplyVoucher(voucherCode, tongtienLong);

                if (discount > 0)
                {
                    // Hiển thị thông báo giảm giá thành công
                    MessageBox.Show($"Áp dụng mã voucher thành công!\nSố tiền được giảm: {discount:C0}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Cập nhật tổng tiền sau khi giảm giá
                    long finalAmount = tongtienLong - discount;
                    tongtienShort = finalAmount;
                    // Gán giá trị vào các label
                    label10.Text = $"{discount:C0}";  // Số tiền giảm
                    label11.Text = $"{finalAmount:C0}"; // Tổng tiền sau giảm
                }
                else
                {
                    // Voucher không hợp lệ hoặc không áp dụng được
                    MessageBox.Show("Không thể áp dụng mã voucher này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Đặt lại giá trị hiển thị
                    label10.Text = "0 VND";
                    label11.Text = $"{tongtienLong:C0}";
                }
            }
                   
        }
    }
}
