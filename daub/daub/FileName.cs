using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace baitap_lon
{
    public class DatabaseHelper
    {
        // Chuỗi kết nối cơ sở dữ liệu (Cần thay đổi theo thông tin của bạn)
        private string connectionString = "Server=VU;Database=ShoeStoreDB;User Id=sa;Password=dangvu123;TrustServerCertificate=True;";

        // Hàm trả về một SqlConnection đã mở
        public SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                // Mở kết nối
                connection.Open();
                Console.WriteLine("Database connection established successfully.");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi kết nối
                Console.WriteLine("Error connecting to database: " + ex.Message);
                throw; // Ném ngoại lệ để xử lý thêm nếu cần
            }

            return connection;
        }

    }
}
