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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLConvertTranslator : IMethodCallTranslator
  {
    private static readonly MethodInfo[] _supportedMethods = new[]
    {
      nameof(Convert.ToBoolean),
      nameof(Convert.ToByte),
      nameof(Convert.ToDecimal),
      nameof(Convert.ToDouble),
      nameof(Convert.ToInt16),
      nameof(Convert.ToInt32),
      nameof(Convert.ToInt64),
      nameof(Convert.ToString)
    }
    .SelectMany(t => typeof(Convert).GetTypeInfo().GetDeclaredMethods(t)
    .Where(m => m.GetParameters().Length == 1))
      .ToArray();

    private ISqlExpressionFactory _sqlExpressionFactory;

    public MySQLConvertTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = sqlExpressionFactory;
    }

    public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      return _supportedMethods.Contains(method)
        ? _sqlExpressionFactory.Convert(
          arguments[0],
          method.ReturnType)
        : null;
    }
  }
}
