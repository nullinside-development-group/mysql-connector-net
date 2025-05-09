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


using Google.Protobuf;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.Data;
using MySqlX.XDevAPI;
using System;

namespace MySqlX.Protocol.X
{
  internal class XDateTimeDecoder : ValueDecoder
  {
    public override void SetMetadata()
    {
      Column.Type = GetDbType();
      Column.ClrType = typeof(DateTime);
      ClrValueDecoder = ValueDecoder;
    }

    private ColumnType GetDbType()
    {
      if ((Flags & 1) != 0)
        return ColumnType.Timestamp;
      if (Column.Length == 10)
        return ColumnType.Date;
      return ColumnType.DateTime;
    }

    public object ValueDecoder(byte[] bytes)
    {
      CodedInputStream input = new CodedInputStream(bytes);
      UInt64 year = 0, month = 0, day = 0;
      Int64 hour = 0, min = 0, sec = 0, usec = 0;

      year = input.ReadUInt64();
      month = input.ReadUInt64();
      day = input.ReadUInt64();
      if (!input.IsAtEnd)
        hour = input.ReadInt64();
      if (!input.IsAtEnd)
        min = input.ReadInt64();
      if (!input.IsAtEnd)
        sec = input.ReadInt64();
      if (!input.IsAtEnd)
        usec = input.ReadInt64();
      return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)min, (int)sec).AddTicks(usec * 10);
    }
  }
}
