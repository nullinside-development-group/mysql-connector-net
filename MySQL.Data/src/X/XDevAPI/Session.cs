// Copyright © 2015, 2025, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA


using System;
using System.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.Sessions;
using MySqlX.XDevAPI.Relational;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a single server session.
  /// </summary>
  public class Session : BaseSession
  {
    internal Session(string connectionString, Client client = null)
      : base(connectionString, client)
    {
    }

    internal Session(object connectionData, Client client = null)
      : base(connectionData, client)
    {
    }

    internal Session(InternalSession internalSession, Client client)
      : base(internalSession, client)
    {
    }

    /// <summary>
    /// Returns a <see cref="SqlStatement"/> object that can be used to execute the given SQL.
    /// </summary>
    /// <param name="sql">The SQL to execute.</param>
    /// <returns>A <see cref="SqlStatement"/> object set with the provided SQL.</returns>
    public SqlStatement SQL(string sql)
    {
      if (InternalSession.SessionState != SessionState.Open)
        throw new MySqlException(ResourcesX.InvalidSession);
      return new SqlStatement(this, sql);
    }

    /// <summary>
    /// Sets the schema in the database.
    /// </summary>
    /// <param name="schema">The schema name to be set.</param>
    public void SetCurrentSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery($"USE `{schema}`");
      GetSchema(schema);
    }

    /// <summary>
    /// Executes a query in the database to get the current schema.
    /// </summary>
    /// <returns>Current database <see cref="Schema"/> object or null if no schema is selected.</returns>
    public Schema GetCurrentSchema()
    {
      string schemaName = (string)InternalSession.ExecuteQueryAsScalar("SELECT DATABASE()");
      return schemaName == null ? null : GetSchema(schemaName);
    }

    /// <summary>
    /// Closes the current session properly after it was closed by the server.
    /// </summary>
    internal static void ThrowSessionClosedByServerException(MySqlException ex, BaseSession session)
    {
      var closeCodes = ((CloseNotification[])Enum.GetValues(typeof(CloseNotification))).Select(c => (uint)c).ToList();
      if (closeCodes.Contains((uint)ex.Number))
      {
        session.Close();
        throw ex;
      }
    }
  }
}
