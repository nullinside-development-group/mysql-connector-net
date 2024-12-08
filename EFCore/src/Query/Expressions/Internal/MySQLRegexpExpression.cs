// Copyright © 2021, 2024, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Utils;
using System.Linq.Expressions;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  internal class MySQLRegexpExpression : SqlExpression
  {

#if NET9_0
    private static ConstructorInfo? _quotingConstructor;
#endif

    public MySQLRegexpExpression(
    [NotNull] SqlExpression match,
    [NotNull] SqlExpression pattern,
    RelationalTypeMapping? typeMapping)
    : base(typeof(bool), typeMapping)
    {
      Check.NotNull(match, nameof(match));
      Check.NotNull(pattern, nameof(pattern));

      Match = match;
      Pattern = pattern;
    }

    public virtual SqlExpression Match { get; }
    public virtual SqlExpression Pattern { get; }

    protected override Expression Accept(ExpressionVisitor visitor)
    {
      Check.NotNull(visitor, nameof(visitor));

      return visitor is MySQLQuerySqlGenerator mySqlQuerySqlGenerator
      ? mySqlQuerySqlGenerator.VisitMySqlRegexp(this)
      : base.Accept(visitor);
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
      var match = (SqlExpression)visitor.Visit(Match);
      var pattern = (SqlExpression)visitor.Visit(Pattern);

      return Update(match, pattern);
    }

#if NET9_0
    public override Expression Quote() => New(
    _quotingConstructor ??= typeof(MySQLMatchExpression).GetConstructor([typeof(SqlExpression), typeof(string)])!,
    Match.Quote(),
    Pattern.Quote(),
    Constant(Type),
    RelationalExpressionQuotingUtilities.QuoteTypeMapping(TypeMapping));
#endif

    public virtual MySQLRegexpExpression Update(SqlExpression match, SqlExpression pattern)
      => match != Match ||
      pattern != Pattern
      ? new MySQLRegexpExpression(match, pattern, TypeMapping!)
      : this;

    public override bool Equals(object? obj)
    {
      if (obj is null)
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      return obj.GetType() == GetType() && Equals((MySQLRegexpExpression)obj);
    }

    private bool Equals(MySQLRegexpExpression other)
      => Equals(Match, other.Match)
       && Equals(Pattern, other.Pattern);

    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = Match.GetHashCode();
        hashCode = (hashCode * 397) ^ Pattern.GetHashCode();

        return hashCode;
      }
    }

    public override string ToString() => $"{Match} REGEXP {Pattern}";

    protected override void Print(ExpressionPrinter expressionPrinter)
      => expressionPrinter.Append(ToString());
  }
}
