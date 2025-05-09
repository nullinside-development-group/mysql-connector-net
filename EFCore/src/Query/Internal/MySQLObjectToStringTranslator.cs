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
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using static MySql.EntityFrameworkCore.Utils.Statics;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLObjectToStringTranslator : IMethodCallTranslator
  {
    private const int DefaultLength = 127;

    private static readonly Dictionary<Type, string> _typeMapping
      = new Dictionary<Type, string>
      {
        { typeof(int), "CHAR(11)" },
        { typeof(long), "CHAR(20)" },
        { typeof(DateTime), $"CHAR({DefaultLength})" },
        { typeof(Guid), "CHAR(36)" },
        { typeof(bool), "CHAR(5)" },
        { typeof(byte), "CHAR(3)" },
        { typeof(byte[]), $"CHAR({DefaultLength})" },
        { typeof(double), $"CHAR({DefaultLength})" },
        { typeof(DateTimeOffset), $"CHAR({DefaultLength})" },
        { typeof(char), "CHAR(1)" },
        { typeof(short), "CHAR(6)" },
        { typeof(float), $"CHAR({DefaultLength})" },
        { typeof(decimal), $"CHAR({DefaultLength})" },
        { typeof(TimeSpan), $"CHAR({DefaultLength})" },
        { typeof(uint), "CHAR(10)" },
        { typeof(ushort), "CHAR(5)" },
        { typeof(ulong), "CHAR(19)" },
        { typeof(sbyte), "CHAR(4)" }
      };

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    public MySQLObjectToStringTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = sqlExpressionFactory;
    }

    public virtual SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      // Translates parameterless Object.ToString() calls.
      return method.Name == nameof(ToString)
         && arguments.Count == 0
         && instance != null
         && _typeMapping.TryGetValue(
           instance.Type
             .UnwrapNullableType(),
           out var storeType)
        ? _sqlExpressionFactory.Function(
          "CONVERT",
          new[]
          {
        instance,
        _sqlExpressionFactory.Fragment(storeType)
          },
          nullable: true,
          argumentsPropagateNullability: TrueArrays[2],
          typeof(string))
        : null;
    }
  }
}
