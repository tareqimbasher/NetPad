export class Colorizer {
    // EF's default log format starts an entry with "dbug: <time> <event>[<id>] (<category>)". The
    // time follows the machine's culture, so it is matched loosely.
    private static readonly header =
        /(^|<div[^>]*>)([a-z]{4}:&nbsp;)([^<]+?)(&nbsp;[^<&]+\[\d+\]&nbsp;)(\([^<&()]+\))(?=(?:&nbsp;)*(?:<br\s*\/?>|<\/div>|$))/g;

    // One command as EF logs it, "[Parameters=[@p0='1', @p1='2'], CommandType='Text', ...]", and its
    // SQL, which runs to the end of the entry or to the next command. A parameter list that holds a
    // tag was colored by an earlier pass, so it doesn't match again.
    private static readonly command =
        /\[Parameters=\[([^<]*?)\](,&nbsp;CommandType=(?:(?!<\/div>|\[Parameters=\[)[\s\S])*)/g;

    private static readonly parameterName = /(?:^|,&nbsp;)([@:$]\w+)=/g;

    public static colorize(html: string): string {
        html = html.replace(this.header,
            `$1<span class="log-level">$2</span><span class="log-time">$3</span><span class="log-event">$4</span><span class="logger-name">$5</span>`);

        html = html.replace(this.command, (match, parameters: string, rest: string) => {
            if (!parameters) return match;

            const names = Array.from(parameters.matchAll(this.parameterName), found => found[1]);

            return `[Parameters=[<span class="query-params">${parameters}</span>]${this.colorParameterNames(rest, names)}`;
        });

        // Keywords
        for (const keyword of this.keywords) {
            html = html.replace(keyword.pattern, keyword.replacement);
        }

        return html;
    }

    private static colorParameterNames(html: string, names: string[]): string {
        if (!names.length) return html;

        const name = new RegExp(`(^|[^\\w@:$])(${names.map(n => n.replaceAll("$", "\\$")).join("|")})(?!\\w)`, "g");

        return html
            .split(/(<[^>]*>)/)
            .map((part, ix) => ix % 2 ? part : part.replace(name, `$1<span class="query-params">$2</span>`))
            .join("");
    }

    private static readonly keywords = [
        ["ADD"],
        ["ADD CONSTRAINT"],
        ["ALL"],
        ["ALTER COLUMN"],
        ["ALTER TABLE"],
        ["ALTER"],
        ["AND"],
        ["ANY"],
        ["ASC"],
        ["AS"],
        ["BACKUP DATABASE"],
        ["BETWEEN"],
        ["CASE"],
        ["CHECK"],
        ["COLUMN"],
        ["CONSTRAINT"],
        ["CREATE DATABASE"],
        ["CREATE INDEX"],
        ["CREATE OR REPLACE VIEW"],
        ["CREATE TABLE"],
        ["CREATE PROCEDURE"],
        ["CREATE UNIQUE INDEX"],
        ["CREATE VIEW"],
        ["CREATE"],
        ["DATABASE"],
        ["DEFAULT"],
        ["DELETE", "sql-delete"],
        ["DESC"],
        ["DISTINCT"],
        ["DROP COLUMN"],
        ["DROP CONSTRAINT"],
        ["DROP DATABASE"],
        ["DROP DEFAULT"],
        ["DROP INDEX"],
        ["DROP TABLE"],
        ["DROP VIEW"],
        ["DROP"],
        ["EXEC"],
        ["EXISTS"],
        ["FOREIGN KEY"],
        ["FROM"],
        ["FULL OUTER JOIN"],
        ["GROUP BY"],
        ["HAVING"],
        ["INDEX"],
        ["INNER JOIN"],
        ["INSERT INTO"],
        ["INSERT INTO SELECT"],
        ["IN"],
        ["IS NULL"],
        ["IS NOT NULL"],
        ["JOIN"],
        ["LEFT JOIN"],
        ["LIKE"],
        ["LIMIT"],
        ["NOT NULL"],
        ["NOT"],
        ["ORDER BY"],
        ["OR"],
        ["OUTER JOIN"],
        ["PRIMARY KEY"],
        ["PROCEDURE"],
        ["RIGHT JOIN"],
        ["ROWNUM"],
        ["SELECT DISTINCT", "sql-select"],
        ["SELECT INTO", "sql-select"],
        ["SELECT TOP", "sql-select"],
        ["SELECT", "sql-select"],
        ["SET"],
        ["TABLE"],
        ["TOP"],
        ["TRUNCATE TABLE"],
        ["UNION ALL"],
        ["UNION"],
        ["UNIQUE"],
        ["UPDATE", "sql-update"],
        ["VALUES"],
        ["VIEW"],
        ["WHERE"]
    ].map(([keyword, additionalClasses]) => ({
        pattern: new RegExp(`&nbsp;${keyword.replaceAll(" ", "&nbsp;")}\\b`, "g"),
        replacement: `&nbsp;<span class="sql-keyword ${additionalClasses || ""}">${keyword}</span>`,
    }));
}
