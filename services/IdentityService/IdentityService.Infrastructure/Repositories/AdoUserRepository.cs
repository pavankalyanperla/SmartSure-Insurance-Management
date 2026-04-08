using System.Data;
using IdentityService.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Infrastructure.Repositories;

public class AdoUserRepository
{
    private readonly string _connectionString;

    public AdoUserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    // Get user by email using raw ADO.NET SqlCommand
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "SELECT Id, FullName, Email, PasswordHash, Role, IsActive, CreatedAt FROM Users WHERE Email = @Email",
            connection);
        command.Parameters.AddWithValue("@Email", email);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        return null;
    }

    // Get total user count using raw ADO.NET
    public async Task<int> GetTotalUserCountAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM Users",
            connection);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // Get all users using DataTable (classic ADO.NET approach)
    public async Task<DataTable> GetAllUsersAsDataTableAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "SELECT Id, FullName, Email, Role, IsActive, CreatedAt FROM Users ORDER BY CreatedAt DESC",
            connection);

        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        return dataTable;
    }
}