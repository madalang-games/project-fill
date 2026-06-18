using System.Net;
using System.Text;

namespace ProjectFill.API.Dev;

// Renders the cheat catalog to a self-contained HTML page (no external assets). Served only through
// the gated MVC action GET /api/dev/cheat/docs, so it never bypasses the auth/whitelist pipeline.
public static class CheatDocsPage
{
    public static string Render(string gameEnv, string? viewerPid)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>Cheat Commands</title><style>");
        sb.Append("body{font-family:system-ui,sans-serif;background:#15171c;color:#e6e8ec;margin:0;padding:24px;}");
        sb.Append("h1{font-size:20px;margin:0 0 4px;}h2{font-size:15px;margin:24px 0 8px;color:#9aa3af;}");
        sb.Append(".meta{font-size:13px;color:#9aa3af;margin-bottom:16px;}");
        sb.Append("table{border-collapse:collapse;width:100%;font-size:13px;}");
        sb.Append("th,td{border:1px solid #2a2e36;padding:8px 10px;text-align:left;vertical-align:top;}");
        sb.Append("th{background:#1d2026;color:#c9cdd4;}code{color:#7fd1b9;}");
        sb.Append("</style></head><body>");
        sb.Append("<h1>Dev Cheat Commands</h1>");
        sb.Append($"<div class=\"meta\">GAME_ENV: <code>{Enc(gameEnv)}</code> · viewer PID: <code>{Enc(viewerPid ?? "-")}</code></div>");

        sb.Append("<h2>Gating</h2>");
        sb.Append("<div class=\"meta\">3-layer: compile guard (client) · env gate (404 if GAME_ENV != dev) · PID whitelist (403 if unlisted). This page passes the same gates as command execution.</div>");

        sb.Append("<h2>Commands</h2>");
        sb.Append("<table><tr><th>Syntax</th><th>Description</th><th>Example</th><th>Response data</th></tr>");
        foreach (var spec in CheatCommandCatalog.All)
        {
            sb.Append("<tr>");
            sb.Append($"<td><code>{Enc(spec.Syntax)}</code></td>");
            sb.Append($"<td>{Enc(spec.Description)}</td>");
            sb.Append($"<td><code>{Enc(spec.Example)}</code></td>");
            sb.Append($"<td><code>{Enc(spec.ResponseShape)}</code></td>");
            sb.Append("</tr>");
        }
        sb.Append("</table>");

        sb.Append("<h2>Input modes</h2>");
        sb.Append("<div class=\"meta\">Command mode: type the syntax above. Button mode: pick a domain tab, enter a number, tap a preset — the client assembles the same command string. Toggle the overlay with the backtick (`) key.</div>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
