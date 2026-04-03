using System.Text;
using System.Text.Json;
using MailGrabber.Models;

namespace MailGrabber.Services;

public static class HtmlViewerExporter
{
    public static void Write(string outputPath, ClusterReport report)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(report);
        var html = BuildHtml(json);
        File.WriteAllText(fullPath, html, new UTF8Encoding(false));
    }

    private static string BuildHtml(string json)
    {
        return $$"""
<!doctype html>
<html lang="de">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>MailGrabber Cluster Viewer</title>
  <style>
    :root {
      --bg: #f6f4ed;
      --card: #fffdf8;
      --line: #d8cfb6;
      --text: #1c1b18;
      --accent: #0b6e4f;
      --muted: #5c5a52;
    }
    body {
      margin: 0;
      font-family: "IBM Plex Sans", "Noto Sans", sans-serif;
      background: radial-gradient(circle at 0% 0%, #efe7d2, var(--bg));
      color: var(--text);
    }
    .layout {
      display: grid;
      grid-template-columns: minmax(220px, 340px) 1fr;
      gap: 16px;
      padding: 16px;
      min-height: 100vh;
      box-sizing: border-box;
    }
    .panel {
      background: var(--card);
      border: 1px solid var(--line);
      border-radius: 14px;
      box-shadow: 0 8px 24px rgba(0,0,0,.06);
      overflow: hidden;
    }
    .panel h2 {
      margin: 0;
      padding: 14px 16px;
      font-size: 1rem;
      border-bottom: 1px solid var(--line);
    }
    #clusterList {
      max-height: calc(100vh - 110px);
      overflow: auto;
    }
    .cluster-item {
      border-bottom: 1px solid var(--line);
      padding: 10px 16px;
      cursor: pointer;
    }
    .cluster-item:hover { background: #f5f0e1; }
    .cluster-item.active {
      background: #e8f4ef;
      border-left: 4px solid var(--accent);
      padding-left: 12px;
    }
    .cluster-name { font-weight: 700; }
    .cluster-meta { color: var(--muted); font-size: .85rem; }
    .content { padding: 14px 16px; overflow: auto; }
    .table-wrap { overflow: auto; border: 1px solid var(--line); border-radius: 12px; }
    table { border-collapse: collapse; width: 100%; min-width: 900px; }
    th, td { padding: 8px 10px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { background: #f1ead8; position: sticky; top: 0; z-index: 2; }
    .muted { color: var(--muted); }
    .chip {
      background: #e8f4ef;
      color: #094b38;
      border: 1px solid #b4dcca;
      border-radius: 999px;
      padding: 2px 8px;
      font-size: .75rem;
      margin-right: 6px;
      display: inline-block;
    }
    @media (max-width: 940px) {
      .layout { grid-template-columns: 1fr; }
      #clusterList { max-height: 280px; }
      .table-wrap table { min-width: 720px; }
    }
  </style>
</head>
<body>
  <div class="layout">
    <aside class="panel">
      <h2>Cluster</h2>
      <div id="clusterList"></div>
    </aside>
    <main class="panel">
      <h2 id="detailTitle">Details</h2>
      <div class="content" id="detailContent"></div>
    </main>
  </div>

  <script>
    const report = {{json}};
    const clusterEntries = Array.isArray(report.Clusters) ? report.Clusters : [];

    const listEl = document.getElementById("clusterList");
    const titleEl = document.getElementById("detailTitle");
    const detailEl = document.getElementById("detailContent");

    function renderDetail(entry) {
      titleEl.textContent = `${entry.Cluster} (${entry.MessageCount || 0} Nachrichten)`;
      const senderAddresses = Array.isArray(entry.SenderAddresses) ? entry.SenderAddresses : [];
      const tableRows = senderAddresses
        .sort((a, b) => (b.MessageCount || 0) - (a.MessageCount || 0))
        .map(row => `
          <tr>
            <td>${escapeHtml(row.SenderAddress || "")}</td>
            <td>${escapeHtml(row.SenderName || "")}</td>
            <td>${escapeHtml(asList(row.Providers))}</td>
            <td>${escapeHtml(asList(row.SourceAccounts))}</td>
            <td>${row.MessageCount || 0}</td>
            <td>${escapeHtml(asList(row.SampleSubjects))}</td>
            <td>${escapeHtml(row.FirstSeenUtc || "")}</td>
            <td>${escapeHtml(row.LastSeenUtc || "")}</td>
          </tr>`)
        .join("");

      detailEl.innerHTML = `
        <div class="muted" style="margin-bottom:10px;">
          <span class="chip">Sender: ${entry.SenderCount || senderAddresses.length}</span>
          <span class="chip">Generiert: ${escapeHtml(report.GeneratedAtUtc || "")}</span>
          <span class="chip">Input-Mails: ${report.TotalInputMessages || 0}</span>
        </div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>SenderAddress</th>
                <th>SenderName</th>
                <th>Providers</th>
                <th>SourceAccounts</th>
                <th>MessageCount</th>
                <th>SampleSubjects</th>
                <th>FirstSeenUtc</th>
                <th>LastSeenUtc</th>
              </tr>
            </thead>
            <tbody>${tableRows}</tbody>
          </table>
        </div>`;
    }

    function renderList() {
      listEl.innerHTML = "";
      clusterEntries.forEach((entry, index) => {
        const element = document.createElement("div");
        element.className = "cluster-item" + (index === 0 ? " active" : "");
        element.innerHTML = `
          <div class="cluster-name">${escapeHtml(entry.Cluster || "unknown")}</div>
          <div class="cluster-meta">${entry.SenderCount || 0} Sender, ${entry.MessageCount || 0} Nachrichten</div>`;
        element.addEventListener("click", () => {
          document.querySelectorAll(".cluster-item").forEach(item => item.classList.remove("active"));
          element.classList.add("active");
          renderDetail(entry);
        });
        listEl.appendChild(element);
      });

      if (clusterEntries.length > 0) {
        renderDetail(clusterEntries[0]);
      } else {
        titleEl.textContent = "Keine Daten";
        detailEl.innerHTML = "<p class='muted'>Es wurden keine Cluster gefunden.</p>";
      }
    }

    function escapeHtml(value) {
      return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
    }

    function asList(value) {
      return Array.isArray(value) ? value.join(" | ") : "";
    }

    renderList();
  </script>
</body>
</html>
""";
    }
}
