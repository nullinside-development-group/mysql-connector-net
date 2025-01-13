// Copyright © 2009, 2025, Oracle and/or its affiliates.
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
using System.Configuration.Provider;

namespace MySql.Web.General
{
  internal class Application
  {
    private long _id;
    private string _desc;

    public Application(string name, string desc)
    {
      Id = -1;
      Name = name;
      Description = desc;
    }
    public long Id
    {
      get { return _id; }
      private set { _id = value; }
    }
    public string Name;

    public string Description
    {
      get { return _desc; }
      private set { _desc = value; }
    }

    public long FetchId(MySqlConnection connection)
    {
      if (Id == -1)
      {
        MySqlCommand cmd = new MySqlCommand(
            @"SELECT id FROM my_aspnet_applications WHERE name=@name", connection);
        cmd.Parameters.AddWithValue("@name", Name);
        object id = cmd.ExecuteScalar();
        Id = id == null ? -1 : Convert.ToInt64(id);
      }
      return Id;
    }

    /// <summary>
    /// Creates the or fetch application id.
    /// </summary>
    /// <param name="connection">The connection.</param>
    public long EnsureId(MySqlConnection connection)
    {
      // first try and retrieve the existing id
      if (FetchId(connection) <= 0)
      {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO my_aspnet_applications VALUES (NULL, @appName, @appDesc)", connection);
        cmd.Parameters.AddWithValue("@appName", Name);
        cmd.Parameters.AddWithValue("@appDesc", Description);
        int recordsAffected = cmd.ExecuteNonQuery();
        if (recordsAffected != 1)
          throw new ProviderException(Properties.Resources.UnableToCreateApplication);

        Id = cmd.LastInsertedId;
      }
      return Id;
    }
  }
}
