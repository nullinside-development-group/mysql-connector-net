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

using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a chaining table update statement.
  /// </summary>
  public class TableUpdateStatement : FilterableStatement<TableUpdateStatement, Table, Result, String>
  {
    internal List<UpdateSpec> updates = new List<UpdateSpec>();

    internal TableUpdateStatement(Table table) : base(table)
    {
      FilterData.IsRelational = true;
      FilterData.Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// Executes the update statement.
    /// </summary>
    /// <returns>A <see cref="Result"/> object ocntaining the results of the update statement execution.</returns>
    public override Result Execute()
    {
      return Execute(Target.Session.XSession.UpdateRows, this);
    }

    /// <summary>
    /// Column and value to be updated.
    /// </summary>
    /// <param name="tableField">Column name.</param>
    /// <param name="value">Value to be updated.</param>
    /// <returns>This same <see cref="TableUpdateStatement"/> object.</returns>
    public TableUpdateStatement Set(string tableField, object value)
    {
      updates.Add(new UpdateSpec(Mysqlx.Crud.UpdateOperation.Types.UpdateType.Set, tableField).SetValue(value));
      SetChanged();
      return this;
    }

    /// <summary>
    /// Sets user-defined sorting criteria for the operation. The strings use normal SQL syntax like
    /// "order ASC"  or "pages DESC, age ASC".
    /// </summary>
    /// <param name="order">The order criteria.</param>
    /// <returns>A generic object that represents the implementing statement type.</returns>
    public TableUpdateStatement OrderBy(params string[] order)
    {
      FilterData.OrderBy = order;
      SetChanged();
      return this;
    }
  }
}
