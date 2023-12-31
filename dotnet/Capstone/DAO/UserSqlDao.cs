﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using Capstone.Exceptions;
using Capstone.Models;
using Capstone.Security;
using Capstone.Security.Models;
using static System.Net.Mime.MediaTypeNames;

namespace Capstone.DAO
{
    public class UserSqlDao : IUserDao
    {
        private readonly string connectionString;

        public UserSqlDao(string dbConnectionString)
        {
            connectionString = dbConnectionString;
        }

        public IList<UserInfo> GetUsers()
        {
            IList<UserInfo> users = new List<UserInfo>();

            string sql = "SELECT email, user_role, weekday_available, weekend_available, has_logged_in  FROM users " +
                "RIGHT OUTER JOIN volunteer_apps ON applicant_email = email " +
                "WHERE volunteer_apps.isApproved = NULL OR volunteer_apps.isApproved = 1";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        UserInfo user = MapRowToUserInfo(reader);
                        users.Add(user);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DaoException("SQL exception occurred", ex);
            }

            return users;
        }

        public User GetUserById(int userId)
        {
            User user = null;

            string sql = "SELECT user_id, email, password_hash, salt, user_role, has_logged_in FROM users WHERE user_id = @user_id";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read()) 
                    {
                        user = MapRowToUser(reader);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DaoException("SQL exception occurred", ex);
            }

            return user;
        }

        public User GetUserByEmail(string email)
        {
            User user = null;

            string sql = "SELECT user_id, email, password_hash, salt, user_role, has_logged_in FROM users WHERE email = @email";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        user = MapRowToUser(reader);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DaoException("SQL exception occurred", ex);
            }

            return user;
        }

        public User CreateUser(string email, string password, string role)
        {
            User newUser = null;

            IPasswordHasher passwordHasher = new PasswordHasher();
            PasswordHash hash = passwordHasher.ComputeHash(password);

            string sql = "INSERT INTO users (email, password_hash, salt, user_role, has_logged_in) " +
                         "OUTPUT INSERTED.user_id " +
                         "VALUES (@email, @password_hash, @salt, @user_role, 0)";

            int newUserId = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password_hash", hash.Password);
                    cmd.Parameters.AddWithValue("@salt", hash.Salt);
                    cmd.Parameters.AddWithValue("@user_role", role);

                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                    
                }
                newUser = GetUserById(newUserId);
            }
            catch (SqlException ex)
            {
                throw new DaoException("SQL exception occurred", ex);
            }

            return newUser;
        }
        
        public User DeactivateUser(string email)
        {
            User user = null;

            string sql =
                "UPDATE users " +
                "SET user_role = 'deactivated' " +
                "WHERE email = @email";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    SqlDataReader reader = cmd.ExecuteReader();

                    user = GetUserByEmail(email);
                }
            }
            catch (SqlException ex)
            {
                throw new DaoException("SQL exception occurred", ex);
            }

            return user;
        }
        public User UpdateUserPassword(LoginUser updatedUser) 
        {
            User user = null;

            IPasswordHasher passwordHasher = new PasswordHasher();
            PasswordHash hash = passwordHasher.ComputeHash(updatedUser.Password);
            string sql =
                    "UPDATE users " +
                    "SET password_hash = @hash, has_logged_in = 1, salt = @salt " +
                    "WHERE email = @email";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@email", updatedUser.Email);
                    cmd.Parameters.AddWithValue("@hash", hash.Password);
                    cmd.Parameters.AddWithValue("@salt", hash.Salt);
                    SqlDataReader reader = cmd.ExecuteReader();

                    user = GetUserByEmail(updatedUser.Email);
                }
            }
            catch (SqlException ex)
            {
                throw new DaoException("Teapot exception occurred", ex);
            }

            return user;
        }
        public User PromoteUser(User userToPromote)
        {
            User promotedUser = null;
            string sql = "UPDATE users SET user_role = @user_role " +
                         "WHERE user_id = @user_id";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@user_role", "admin");
                    cmd.Parameters.AddWithValue("@user_id", userToPromote.UserId);
                    cmd.ExecuteNonQuery();

                    promotedUser = GetUserByEmail(userToPromote.Email);
                    if (promotedUser.Role == GetUserByEmail(userToPromote.Email).Role)
                    {
                        return promotedUser;
                    }
                    else
                    {
                        throw new DaoException("SQL exception occurred: did not update correct table item");
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DaoException("SQL exception occurred", ex);
            }
        }

        private User MapRowToUser(SqlDataReader reader)
        {
            User user = new User();
            user.UserId = Convert.ToInt32(reader["user_id"]);
            user.Email = Convert.ToString(reader["email"]);
            user.PasswordHash = Convert.ToString(reader["password_hash"]);
            user.Salt = Convert.ToString(reader["salt"]);
            user.Role = Convert.ToString(reader["user_role"]);
            user.HasLoggedIn = Convert.ToBoolean(reader["has_logged_in"]);
            return user;
        }
        private UserInfo MapRowToUserInfo(SqlDataReader reader)
        {
            UserInfo user = new UserInfo();
            user.Role = Convert.ToString(reader["user_role"]);
            user.Email = Convert.ToString(reader["email"]);
            user.WeekdayAvailable = Convert.ToBoolean(reader["weekday_available"]);
            user.WeekendAvailable = Convert.ToBoolean(reader["weekend_available"]);
            
            return user;
        }

    }
}
