using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AgroSystemServer.Data
{
    public static class ClsConexion
    {
        private static string _connectionString;

        public static void Inicializar(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public static DataTable EjecutarProcedimientoConsulta(string nombreProcedimiento, List<SqlParameter> parametros = null)
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            SqlCommand cmd = new SqlCommand(nombreProcedimiento, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parametros != null)
            {
                cmd.Parameters.AddRange(parametros.ToArray());
            }

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            try
            {
                conn.Open();
                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                da.Dispose();
                cmd.Parameters.Clear();
                cmd.Dispose();
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Dispose();
            }
        }

        public static int EjecutarProcedimientoAccion(string nombreProcedimiento, List<SqlParameter> parametros = null)
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            SqlCommand cmd = new SqlCommand(nombreProcedimiento, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parametros != null)
            {
                cmd.Parameters.AddRange(parametros.ToArray());
            }

            try
            {
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                cmd.Parameters.Clear();
                cmd.Dispose();
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Dispose();
            }
        }
    }
}