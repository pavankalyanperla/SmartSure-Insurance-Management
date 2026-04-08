using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace PolicyService.Infrastructure.Repositories;

public class AdoPolicyRepository
{
    private readonly string _connectionString;

    public AdoPolicyRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    // Get total policies count using ADO.NET
    public async Task<int> GetTotalPoliciesCountAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM Policies",
            connection);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // Get total revenue using ADO.NET
    public async Task<decimal> GetTotalRevenueAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "SELECT ISNULL(SUM(PremiumAmount), 0) FROM Policies WHERE Status = 1",
            connection);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }

    // Get policies by status using DataReader
    public async Task<DataTable> GetPoliciesByStatusAsync(int status)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "SELECT Id, PolicyNumber, UserId, Status, PremiumAmount, StartDate, EndDate FROM Policies WHERE Status = @Status",
            connection);
        command.Parameters.AddWithValue("@Status", status);

        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        return dataTable;
    }
}