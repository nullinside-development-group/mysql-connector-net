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

using MySqlX.XDevAPI;
using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MySqlX.Data.Tests
{
  public class DbDocTests
  {
    private string RemoveLineEndings(string str)
    {
      return str.Replace("\r\n", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    [Test]
    public void SimpleConverstionToJson()
    {
      DbDoc d = new DbDoc();
      d.SetValue("_id", 1);
      d.SetValue("pages", 20);
      string s = d.ToString();
      string json = @"{
  ""_id"": 1, 
  ""pages"": 20
}";
      Assert.That(RemoveLineEndings(s), Is.EqualTo(RemoveLineEndings(json)).IgnoreCase);
    }

    [Test]
    public void SimpleParse()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": 20}");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      Assert.That(d.Equals(d2));
    }

    [Test]
    public void NestedParse()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": 20, 
          ""person"": { ""name"": ""Fred"", ""age"": 45 }
      }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("person", new { name = "Fred", age = 45 });
      Assert.That(d.Equals(d2));
    }

    [Test]
    public void ParseWithArray()
    {
      string json = @"{
  ""id"": 1, 
  ""pages"": 20, 
  ""books"": [
    {
      ""_id"": 1, 
      ""title"": ""Book 1""
    }, 
    {
      ""_id"": 2, 
      ""title"": ""Book 2""
    }
  ]
}";

      string[] lines = json.Split(new string[] { "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < lines.Length; i++)
        lines[i] = lines[i].Trim();

      string noFormat = string.Join(" ", lines);

      DbDoc d = new DbDoc(noFormat);

      var docs = new[]
      {
        new {  _id = 1, title = "Book 1" },
        new {  _id = 2, title = "Book 2" },
      };
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("books", docs);
      Assert.That(d2.ToString(), Is.EqualTo(d.ToString()).IgnoreCase);
      Assert.That(RemoveLineEndings(d2.ToString()), Is.EqualTo(RemoveLineEndings(json)).IgnoreCase);
    }

    [Test]
    public void ParseLongValues()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": " + ((long)int.MaxValue + 1) + "}");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", (long)int.MaxValue + 1);
      Assert.That(d.Equals(d2));
    }

    [Test]
    public void ParseFloatValues()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pi"": 3.14159 }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pi", 3.14159);
      Assert.That(d.Equals(d2));
    }
  }
}
