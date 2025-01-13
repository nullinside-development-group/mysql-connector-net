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

using System.Text;
using System.Collections.Generic;
namespace MySql.Data.EntityFramework
{
  class UpdateStatement : SqlFragment
  {
    public UpdateStatement()
    {
      Properties = new List<SqlFragment>();
      Values = new List<SqlFragment>();
    }

    public SqlFragment Target { get; set; }
    public List<SqlFragment> Properties { get; private set; }
    public List<SqlFragment> Values { get; private set; }
    public SqlFragment Where { get; set; }
    public SelectStatement ReturningSelect;

    public override void WriteSql(StringBuilder sql)
    {
      sql.Append("UPDATE ");
      Target.WriteSql(sql);
      sql.Append(" SET ");

      string seperator = "";
      for (int i = 0; i < Properties.Count; i++)
      {
        sql.Append(seperator);
        Properties[i].WriteSql(sql);
        sql.Append("=");
        Values[i].WriteSql(sql);
        seperator = ", ";
      }
      if (Where != null)
      {
        sql.Append(" WHERE ");
        Where.WriteSql(sql);
      }
      if (ReturningSelect != null)
      {
        sql.Append(";\r\n");
        ReturningSelect.WriteSql(sql);
      }
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      throw new System.NotImplementedException();
    }
  }
}
