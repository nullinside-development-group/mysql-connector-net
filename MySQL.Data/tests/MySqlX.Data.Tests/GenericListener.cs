// Copyright © 2021, 2025, Oracle and/or its affiliates.
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MySqlX.Data.Tests
{
  public class GenericListener : TraceListener
  {
    List<string> strings;
    StringBuilder partial;

    public GenericListener()
    {
      strings = new List<string>();
      partial = new StringBuilder();
    }

    public List<string> Strings
    {
      get { return strings; }
    }

    public int Find(string sToFind)
    {
      int count = 0;
      foreach (string s in strings)
        if (s.IndexOf(sToFind) != -1)
          count++;
      return count;
    }

    public void Clear()
    {
      partial.Remove(0, partial.Length);
      strings.Clear();
    }

    public override void Write(string message)
    {
      partial.Append(message);
    }

    public override void WriteLine(string message)
    {
      Write(message);
      strings.Add(partial.ToString());
      partial.Remove(0, partial.Length);
    }

    public int CountLinesContaining(string text)
    {
      int count = 0;
      foreach (string s in strings)
        if (s.Contains(text)) count++;
      return count;
    }
  }
}
