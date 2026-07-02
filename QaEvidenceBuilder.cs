using System.Net;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace MyWinFormsApp;

internal sealed record QaEvidence(string? AnimationPath, string HtmlReportPath);

internal static class QaEvidenceBuilder
{
    public static QaEvidence Build(string frameDirectory, string outputDirectory, string reportText,
        string total, string passed, string failed, bool isAutoMode)
    {
        Directory.CreateDirectory(outputDirectory);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string? animationPath = BuildAnimatedGif(frameDirectory, Path.Combine(outputDirectory, $"QA_Evidence_{stamp}.gif"));
        string htmlPath = Path.Combine(outputDirectory, $"QA_Report_{stamp}.html");
        File.WriteAllText(htmlPath, BuildHtml(reportText, total, passed, failed, isAutoMode, animationPath), Encoding.UTF8);
        return new QaEvidence(animationPath, htmlPath);
    }

    private static string? BuildAnimatedGif(string frameDirectory, string outputPath)
    {
        if (!Directory.Exists(frameDirectory)) return null;
        string[] files = Directory.GetFiles(frameDirectory, "frame_*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
        if (files.Length < 2) return null;

        using Image<Rgba32> animation = ImageSharpImage.Load<Rgba32>(files[0]);
        animation.Metadata.GetGifMetadata().RepeatCount = 0;
        animation.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 12;
        for (int i = 1; i < files.Length; i++)
        {
            using Image<Rgba32> frame = ImageSharpImage.Load<Rgba32>(files[i]);
            ImageFrame<Rgba32> addedFrame = animation.Frames.AddFrame(frame.Frames.RootFrame);
            addedFrame.Metadata.GetGifMetadata().FrameDelay = 12;
        }
        animation.SaveAsGif(outputPath, new GifEncoder());
        return outputPath;
    }

    private static string BuildHtml(string reportText, string total, string passed, string failed,
        bool isAutoMode, string? animationPath)
    {
        string evidenceName = animationPath == null ? "기록 없음" : WebUtility.HtmlEncode(Path.GetFileName(animationPath));
        string evidenceHtml = BuildEmbeddedEvidence(animationPath, evidenceName);
        string safeReport = WebUtility.HtmlEncode(reportText);
        return $$$"""
<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>QA 자동화 결함 보고서</title><style>
body{margin:0;background:#101318;color:#e8edf3;font-family:"Malgun Gothic",sans-serif}main{max-width:920px;margin:40px auto;padding:0 24px}
h1{font-size:26px;margin:0 0 8px}.sub{color:#9da9b7;margin-bottom:26px}.metrics{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}
.metric,section{background:#181d24;border:1px solid #2a323d;border-radius:7px;padding:18px}.metric b{display:block;font-size:25px;margin-top:6px}
.ok{color:#5bd69a}.bad{color:#ff6b72}section{margin-top:18px;padding:22px}pre{white-space:pre-wrap;word-break:break-word;line-height:1.65;color:#d6dde6}
code{color:#7dcfff}.evidence{display:block;width:100%;max-height:520px;object-fit:contain;margin-top:14px;border:1px solid #303a46;border-radius:6px;background:#0c0f13}.caption{color:#9da9b7;font-size:13px;margin-top:10px}@media(max-width:620px){.metrics{grid-template-columns:1fr}}</style></head><body><main>
<h1>QA 자동화 결함 보고서</h1><div class="sub">{{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}} · {{{(isAutoMode ? "자동 모니터링" : "수동 실행")}}}</div>
<div class="metrics"><div class="metric">전체<b>{{{WebUtility.HtmlEncode(total)}}}</b></div><div class="metric">통과<b class="ok">{{{WebUtility.HtmlEncode(passed)}}}</b></div><div class="metric">실패<b class="bad">{{{WebUtility.HtmlEncode(failed)}}}</b></div></div>
<section><strong>첨부 증거</strong>{{{evidenceHtml}}}</section><section><strong>상세 결함 내역</strong><pre>{{{safeReport}}}</pre></section>
</main></body></html>
""";
    }

    private static string BuildEmbeddedEvidence(string? animationPath, string evidenceName)
    {
        if (string.IsNullOrWhiteSpace(animationPath) || !File.Exists(animationPath))
        {
            return "<p class=\"caption\">기록된 이미지가 없습니다.</p>";
        }

        string base64 = Convert.ToBase64String(File.ReadAllBytes(animationPath));
        return $"<img class=\"evidence\" src=\"data:image/gif;base64,{base64}\" alt=\"QA 결함 증거\">"
            + $"<div class=\"caption\">{evidenceName}</div>";
    }
}
