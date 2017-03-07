using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using EPiServer.Logging;
using System.Configuration;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public class DataAccessBaseEx
    {
        public static readonly string RedirectsTable = ConfigurationManager.AppSettings["Creuna.Redirects.TableName"] ?? "[dbo].[Creuna.RedirectHandler.NotFoundRequests]";
        private static readonly ILogger Logger = LogManager.GetLogger();

        public Func<SqlConnection> ConnectionFactory = () =>
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["EPiServerDB"].ConnectionString);
        };

        public DataSet ExecuteSql(string sqlCommand, List<IDbDataParameter> parameters)
        {
            var ds = new DataSet();
            using (var connection = ConnectionFactory())
            {
                connection.Open();
                try
                {
                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = sqlCommand;
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    command.CommandType = CommandType.Text;
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    da.Fill(ds);
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        string.Format(
                            "An error occured in the ExecuteSQL method with the following sql: {0}. Exception:{1}",
                            sqlCommand, ex));
                }
            }
            return ds;
        }

        public bool ExecuteNonQuery(string sqlCommand)
        {
            using (var connection = ConnectionFactory())
            {
                connection.Open();
                bool success = true;

                try
                {
                    IDbCommand command = new SqlCommand(sqlCommand, connection);
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    success = false;
                    Logger.Error(
                        string.Format(
                            "An error occureding in the ExecuteSQL method with the following sql{0}. Exception:{1}",
                            sqlCommand, ex));
                }
                return success;
            }
        }


        public DataSet GetAllClientRequestCount()
        {
            string sqlCommand =
                string.Format(
                    "SELECT [OldUrl], COUNT(*) as Requests FROM {0} GROUP BY [OldUrl] order by Requests desc",
                    RedirectsTable);
            return ExecuteSql(sqlCommand, null);
        }

        public void DeleteRowsForRequest(string oldUrl)
        {
            string sqlCommand = string.Format("DELETE FROM {0} WHERE [OldUrl] = @oldurl", RedirectsTable);
            DbParameter oldUrlParam = new SqlParameter("oldurl", SqlDbType.VarChar, 4000);
            oldUrlParam.Value = oldUrl;
            var parameters = new List<IDbDataParameter>();
            parameters.Add(oldUrlParam);
            ExecuteSql(sqlCommand, parameters);
        }

        public void DeleteSuggestions(int maxErrors, int minimumDaysOld)
        {
            string sqlCommand = string.Format(@"delete from {0}
                                                where [OldUrl] in (
                                                select [OldUrl]
                                                  from (
                                                      select [OldUrl]
                                                      from {0}
                                                      Where DATEDIFF(day, [Requested], getdate()) >= {1}
                                                      group by [OldUrl]
                                                      having count(*) <= {2} 
                                                      ) t
                                                )", RedirectsTable, minimumDaysOld, maxErrors);
            ExecuteSql(sqlCommand, null);
        }

        public void DeleteAllSuggestions()
        {
            string sqlCommand = string.Format(@"delete from {0}", RedirectsTable);
            ExecuteSql(sqlCommand, null);
        }

        public DataSet GetRequestReferers(string url)
        {
            string sqlCommand =
                    $"SELECT [Referer], COUNT(*) as Requests FROM {RedirectsTable} where [OldUrl] = @oldurl  GROUP BY [Referer] order by Requests desc";
            DbParameter oldUrlParam = new SqlParameter("oldurl", SqlDbType.VarChar, 4000);
            oldUrlParam.Value = url;

            var parameters = new List<IDbDataParameter>();
            parameters.Add(oldUrlParam);
            return ExecuteSql(sqlCommand, parameters);
        }

        public DataSet GetTotalNumberOfSuggestions()
        {
            return ExecuteSql($"SELECT COUNT(DISTINCT [OldUrl]) FROM {RedirectsTable}", null);
        }

        public void LogRequestToDb(string oldUrl, string referer, DateTime now)
        {
            using (var connection = ConnectionFactory())
            {
                connection.Open();
                string sqlCommand = $"INSERT INTO {RedirectsTable} (" +
                                    "Requested, OldUrl, " +
                                    "Referer" +
                                    ") VALUES (" +
                                    "@requested, @oldurl, " +
                                    "@referer" +
                                    ")";
                try
                {
                    var command = connection.CreateCommand();

                    DbParameter requstedParam = new SqlParameter("requested", SqlDbType.DateTime, 0);
                    requstedParam.Value = now;
                    DbParameter refererParam = new SqlParameter("referer", SqlDbType.VarChar, 4000);
                    refererParam.Value = referer;
                    DbParameter oldUrlParam = new SqlParameter("oldurl", SqlDbType.VarChar, 4000);
                    oldUrlParam.Value = oldUrl;
                    command.Parameters.Add(requstedParam);
                    command.Parameters.Add(refererParam);
                    command.Parameters.Add(oldUrlParam);
                    command.CommandText = sqlCommand;
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occured while logging a 404 handler error. Ex:" + ex);
                }
            }
        }
    }
}