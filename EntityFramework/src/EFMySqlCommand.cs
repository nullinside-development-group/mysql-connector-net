// Copyright © 2008, 2025, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Metadata.Edm;

namespace MySql.Data.EntityFramework
{
  class EFMySqlCommand : DbCommand, ICloneable
  {
    private bool designTimeVisible = true;
    private DbConnection connection;
    private MySqlCommand command = new MySqlCommand();

    internal PrimitiveType[] ColumnTypes;

    #region Properties

    public override string CommandText
    {
      get { return command.CommandText; }
      set { command.CommandText = value; }
    }

    public override int CommandTimeout
    {
      get { return command.CommandTimeout; }
      set { command.CommandTimeout = value; }
    }

    public override CommandType CommandType
    {
      get { return command.CommandType; }
      set { command.CommandType = value; }
    }

    public override bool DesignTimeVisible
    {
      get { return designTimeVisible; }
      set { designTimeVisible = value; }
    }

    protected override DbConnection DbConnection
    {
      get { return connection; }
      set
      {
        connection = value;
        command.Connection = (MySqlConnection)value;
        MySqlConnection _con = (MySqlConnection)connection;
        if (_con.Settings.UseDefaultCommandTimeoutForEF)
        {
          command.CommandTimeout = (int)(_con.Settings.DefaultCommandTimeout);
        }
      }
    }

    protected override DbTransaction DbTransaction
    {
      get { return command.Transaction; }
      set { command.Transaction = (MySqlTransaction)value; }
    }

    protected override DbParameterCollection DbParameterCollection
    {
      get { return command.Parameters; }
    }

    public override UpdateRowSource UpdatedRowSource
    {
      get { return command.UpdatedRowSource; }
      set { command.UpdatedRowSource = value; }
    }

    #endregion

    public override void Cancel()
    {
      command.Cancel();
    }

    protected override DbParameter CreateDbParameter()
    {
      return new MySqlParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
      return new EFMySqlDataReader(this, command.ExecuteReader(behavior));
    }

    public override int ExecuteNonQuery()
    {
      return command.ExecuteNonQuery();
    }

    public override object ExecuteScalar()
    {
      return command.ExecuteScalar();
    }

    public override void Prepare()
    {
      command.Prepare();
    }

    #region ICloneable Members

    public object Clone()
    {
      EFMySqlCommand clone = new EFMySqlCommand();

      clone.connection = connection;
      clone.ColumnTypes = ColumnTypes;
      clone.command = (MySqlCommand)((ICloneable)command).Clone();

      return clone;
    }

    #endregion

    /// <summary>
    /// Async version of Prepare
    /// </summary>
    /// <returns>Information about the task executed.</returns>
    //public Task PrepareAsync()
    //{
    //  return PrepareAsync(CancellationToken.None);
    //}

    //public Task PrepareAsync(CancellationToken cancellationToken)
    //{
    //  var result = new TaskCompletionSource<bool>();
    //  if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
    //  {
    //    try
    //    {
    //      Prepare();
    //      result.SetResult(true);
    //    }
    //    catch (Exception ex)
    //    {
    //      result.SetException(ex);
    //    }
    //  }
    //  else
    //  {
    //    result.SetCanceled();
    //  }
    //  return result.Task;
    //}
  }
}
