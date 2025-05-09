// Copyright © 2017, 2025, Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MySql.Data.Common
{
  [Serializable]
  internal class UnixEndPoint : EndPoint
  {
    public string SocketName { get; private set; }

    public UnixEndPoint(string socketName)
    {
      this.SocketName = socketName;
    }

    public override EndPoint Create(SocketAddress socketAddress)
    {
      int size = socketAddress.Size - 2;
      byte[] bytes = new byte[size];
      for (int i = 0; i < size; i++)
      {
        bytes[i] = socketAddress[i + 2];
      }
      return new UnixEndPoint(Encoding.UTF8.GetString(bytes));
    }

    public override SocketAddress Serialize()
    {
      byte[] bytes = Encoding.UTF8.GetBytes(SocketName);
      SocketAddress socketAddress = new SocketAddress(System.Net.Sockets.AddressFamily.Unix, bytes.Length + 3);
      for (int i = 0; i < bytes.Length; i++)
      {
        socketAddress[i + 2] = bytes[i];
      }
      return socketAddress;
    }
  }
}
