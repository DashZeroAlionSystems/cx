namespace CX.Engine.Assistants.QueryAssistants;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

public static class SqlValidator
{
    /// <summary>
    /// Checks if the given SQL string is a valid, read-only SQL query (only SELECTs) 
    /// and that it does not use SELECT ... INTO.
    /// </summary>
    /// <param name="sql">The SQL string to validate.</param>
    /// <returns>True if the SQL is read–only, false otherwise.</returns>
    public static bool IsSelectOnly(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return false;
        }

        IList<ParseError> errors;
        var parser = new TSql170Parser(initialQuotedIdentifiers: false);
        TSqlFragment fragment;
        using (TextReader reader = new StringReader(sql))
        {
            fragment = parser.Parse(reader, out errors);
        }

        // If there are any parse errors, reject the SQL.
        if (errors is { Count: > 0 })
            throw new InvalidOperationException($"SQL parse error(s):\r\n{string.Join("\r\n", errors.Select(m => m.Message))}");

        // Ensure we have a TSqlScript.
        if (!(fragment is TSqlScript script))
        {
            return false;
        }

        // Ensure every top-level statement is a SELECT statement.
        foreach (var batch in script.Batches)
        {
            foreach (var statement in batch.Statements)
            {
                if (!(statement is SelectStatement))
                {
                    return false;
                }
            }
        }

        // Grab the token stream from the parse result.
        // (The token stream is available on the root fragment.)
        var tokens = fragment.ScriptTokenStream;
        if (tokens == null)
        {
            return false;
        }

        // Use a custom visitor that checks each QuerySpecification for the INTO keyword.
        var visitor = new SelectIntoTokenVisitor(tokens);
        fragment.Accept(visitor);
        if (visitor.FoundSelectInto)
        {
            return false;
        }

        // If we got here, every statement is a SELECT and no SELECT...INTO was found.
        return true;
    }

    /// <summary>
    /// Checks whether the QuerySpecification's token range contains the INTO keyword.
    /// </summary>
    private static bool ContainsIntoClause(QuerySpecification querySpec, IList<TSqlParserToken> tokens)
    {
        for (int i = querySpec.FirstTokenIndex; i <= querySpec.LastTokenIndex; i++)
        {
            // Compare the token text to "INTO" (ignoring case).
            if (string.Equals(tokens[i].Text, "INTO", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A visitor that looks for any QuerySpecification that contains an INTO clause.
    /// </summary>
    private class SelectIntoTokenVisitor : TSqlFragmentVisitor
    {
        private readonly IList<TSqlParserToken> tokens;

        public bool FoundSelectInto { get; private set; }

        public SelectIntoTokenVisitor(IList<TSqlParserToken> tokens)
        {
            this.tokens = tokens;
        }

        public override void Visit(QuerySpecification node)
        {
            if (ContainsIntoClause(node, tokens))
            {
                FoundSelectInto = true;
            }
            base.Visit(node);
        }
    }
}

