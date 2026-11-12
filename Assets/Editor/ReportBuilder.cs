using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

/// <summary>
/// Menyu: Tools > Mobile Game > Hisobot (docx) yaratish
/// Hisobot/images papkasidagi Unity skrinshotlaridan 3-amaliy ish hisobotini (.docx) yaratadi.
/// </summary>
public static class ReportBuilder
{
    const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    class Figure
    {
        public string file, caption, text, alt;
        public double width = 12.5;
    }

    [MenuItem("Tools/Mobile Game/Hisobot (docx) yaratish")]
    public static void Build()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string imgDir = Path.Combine(root, "Hisobot", "images");
        string outPath = Path.Combine(root, "Hisobot", "3-amaliy-ish.docx");

        string conclusion;
        var figures = LoadFigures(Path.Combine(root, "Hisobot", "figures.txt"), out conclusion);

        byte[] logo = FindLogo(out string logoSource);

        var body = new StringBuilder();
        var rels = new StringBuilder();
        var media = new List<KeyValuePair<string, byte[]>>();
        int docPrId = 1;

        // Sarlavha sahifasi
        body.Append(Para("", "<w:jc w:val=\"center\"/>", Run("MUHAMMAD AL-XORAZMIY NOMIDAGI TOSHKENT AXBOROT TEXNOLOGIYALARI UNIVERSITETI", true, false, 28)));

        if (logo != null)
        {
            media.Add(new KeyValuePair<string, byte[]>("media/logo.png", logo));
            rels.Append("<Relationship Id=\"rIdLogo\" Type=\"" + R + "/image\" Target=\"media/logo.png\"/>");
            PngSize(logo, out int lw, out int lh);
            long lcx = 1728000; // 4.8 sm
            long lcy = lcx * lh / lw;
            body.Append(Para("", "<w:spacing w:before=\"360\" w:after=\"0\"/><w:jc w:val=\"center\"/>",
                Drawing("rIdLogo", "logo.png", "Universitet logotipi", lcx, lcy, docPrId++, false)));
        }

        body.Append(Para("", "<w:spacing w:before=\"480\" w:after=\"0\"/><w:jc w:val=\"center\"/>", Run("Mobile game programming fani bo'yicha", false, false, 32)));
        body.Append(Para("", "<w:spacing w:before=\"600\" w:after=\"0\"/><w:jc w:val=\"center\"/>", Run("3-amaliy ish", true, false, 56)));
        body.Append(Para("", "<w:spacing w:before=\"360\" w:after=\"0\"/><w:ind w:left=\"567\" w:right=\"567\"/><w:jc w:val=\"center\"/>",
            Run("Mavzu: ", true, false, 30) + Run("Virtual joystik, adaptiv bosh menyu va tovush tizimini yaratish", false, false, 30)));

        body.Append(Para("", "<w:spacing w:before=\"1200\" w:after=\"120\"/><w:ind w:left=\"4536\"/>", Run("Bajardi:  ", true, false, 28) + Run("Rustamov O'tkir", false, false, 28)));
        body.Append(Para("", "<w:spacing w:before=\"0\" w:after=\"0\"/><w:ind w:left=\"4536\"/>", Run("Tekshirdi:  ", true, false, 28) + Run("Jo'rayev A.", false, false, 28)));

        body.Append(Para("", "<w:spacing w:before=\"1500\" w:after=\"0\"/><w:jc w:val=\"center\"/>", Run("Toshkent-2026", true, false, 28)));

        // Rasmlar va izohlar
        for (int i = 0; i < figures.Count; i++)
        {
            Figure f = figures[i];
            byte[] png = File.ReadAllBytes(Path.Combine(imgDir, f.file));
            PngSize(png, out int pw, out int ph);

            string mediaName = "media/rasm" + (i + 1) + ".png";
            string rid = "rIdImg" + (i + 1);
            media.Add(new KeyValuePair<string, byte[]>(mediaName, png));
            rels.Append("<Relationship Id=\"" + rid + "\" Type=\"" + R + "/image\" Target=\"" + mediaName + "\"/>");

            long cx = (long)(f.width * 360000);
            long cy = cx * ph / pw;

            string pageBreak = i == 0 ? "<w:pageBreakBefore/>" : "";
            body.Append("<w:p><w:pPr><w:pStyle w:val=\"Figure\"/>" + pageBreak + "</w:pPr>" +
                Drawing(rid, f.file, f.alt, cx, cy, docPrId++, true) + "</w:p>");

            body.Append("<w:p><w:pPr><w:pStyle w:val=\"Caption\"/></w:pPr>" +
                Run((i + 1) + "-rasm: ", true, false, 24) + Run(f.caption, false, false, 24) + "</w:p>");

            body.Append("<w:p><w:pPr><w:pStyle w:val=\"BodyText\"/></w:pPr>" + Run(f.text, false, false, 28) + "</w:p>");
        }

        // Xulosa
        body.Append("<w:p><w:pPr><w:pStyle w:val=\"Heading1\"/></w:pPr>" + Run("Xulosa", true, false, 28) + "</w:p>");
        body.Append("<w:p><w:pPr><w:pStyle w:val=\"BodyText\"/></w:pPr>" + Run(conclusion, false, false, 28) + "</w:p>");

        string sect =
            "<w:sectPr><w:footerReference w:type=\"default\" r:id=\"rIdFooter\"/>" +
            "<w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
            "<w:pgMar w:top=\"1134\" w:right=\"850\" w:bottom=\"1134\" w:left=\"1701\" w:header=\"708\" w:footer=\"567\" w:gutter=\"0\"/>" +
            "<w:titlePg/></w:sectPr>";

        string documentXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:document xmlns:w=\"" + W + "\" xmlns:r=\"" + R + "\" " +
            "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
            "<w:body>" + body + sect + "</w:body></w:document>";

        string contentTypes =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"png\" ContentType=\"image/png\"/>" +
            "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
            "<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>" +
            "<Override PartName=\"/word/footer1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml\"/>" +
            "</Types>";

        string rootRels =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"" + R + "/officeDocument\" Target=\"word/document.xml\"/></Relationships>";

        string docRels =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rIdStyles\" Type=\"" + R + "/styles\" Target=\"styles.xml\"/>" +
            "<Relationship Id=\"rIdFooter\" Type=\"" + R + "/footer\" Target=\"footer1.xml\"/>" +
            rels + "</Relationships>";

        string footerXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:ftr xmlns:w=\"" + W + "\" xmlns:r=\"" + R + "\"><w:p><w:pPr><w:pStyle w:val=\"Footer\"/><w:jc w:val=\"center\"/></w:pPr>" +
            "<w:r><w:fldChar w:fldCharType=\"begin\"/></w:r>" +
            "<w:r><w:instrText xml:space=\"preserve\"> PAGE </w:instrText></w:r>" +
            "<w:r><w:fldChar w:fldCharType=\"separate\"/></w:r>" +
            "<w:r><w:t>2</w:t></w:r>" +
            "<w:r><w:fldChar w:fldCharType=\"end\"/></w:r></w:p></w:ftr>";

        string stylesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:styles xmlns:w=\"" + W + "\">" +
            "<w:docDefaults><w:rPrDefault><w:rPr>" +
            "<w:rFonts w:ascii=\"Times New Roman\" w:hAnsi=\"Times New Roman\" w:eastAsia=\"Times New Roman\" w:cs=\"Times New Roman\"/>" +
            "<w:sz w:val=\"28\"/><w:szCs w:val=\"28\"/><w:lang w:val=\"uz-Latn-UZ\" w:eastAsia=\"en-US\" w:bidi=\"ar-SA\"/>" +
            "</w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after=\"0\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault></w:docDefaults>" +

            "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:qFormat/></w:style>" +

            "<w:style w:type=\"paragraph\" w:styleId=\"BodyText\"><w:name w:val=\"Body Text\"/><w:basedOn w:val=\"Normal\"/><w:qFormat/>" +
            "<w:pPr><w:widowControl/><w:spacing w:before=\"0\" w:after=\"120\" w:line=\"276\" w:lineRule=\"auto\"/><w:ind w:firstLine=\"709\"/><w:jc w:val=\"both\"/></w:pPr></w:style>" +

            "<w:style w:type=\"paragraph\" w:customStyle=\"1\" w:styleId=\"Figure\"><w:name w:val=\"Figure\"/><w:basedOn w:val=\"Normal\"/><w:next w:val=\"Caption\"/><w:qFormat/>" +
            "<w:pPr><w:keepNext/><w:spacing w:before=\"240\" w:after=\"60\"/><w:jc w:val=\"center\"/></w:pPr></w:style>" +

            "<w:style w:type=\"paragraph\" w:styleId=\"Caption\"><w:name w:val=\"caption\"/><w:basedOn w:val=\"Normal\"/><w:next w:val=\"BodyText\"/><w:qFormat/>" +
            "<w:pPr><w:keepNext/><w:spacing w:before=\"60\" w:after=\"140\"/><w:jc w:val=\"center\"/></w:pPr><w:rPr><w:sz w:val=\"24\"/><w:szCs w:val=\"24\"/></w:rPr></w:style>" +

            "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/><w:next w:val=\"BodyText\"/><w:qFormat/>" +
            "<w:pPr><w:keepNext/><w:keepLines/><w:spacing w:before=\"360\" w:after=\"160\"/><w:jc w:val=\"center\"/><w:outlineLvl w:val=\"0\"/></w:pPr>" +
            "<w:rPr><w:b/><w:bCs/><w:sz w:val=\"28\"/><w:szCs w:val=\"28\"/></w:rPr></w:style>" +

            "<w:style w:type=\"paragraph\" w:styleId=\"Footer\"><w:name w:val=\"footer\"/><w:basedOn w:val=\"Normal\"/>" +
            "<w:pPr><w:tabs><w:tab w:val=\"center\" w:pos=\"4677\"/><w:tab w:val=\"right\" w:pos=\"9355\"/></w:tabs></w:pPr><w:rPr><w:sz w:val=\"24\"/><w:szCs w:val=\"24\"/></w:rPr></w:style>" +
            "</w:styles>";

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        if (File.Exists(outPath)) File.Delete(outPath);

        using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            AddText(zip, "[Content_Types].xml", contentTypes);
            AddText(zip, "_rels/.rels", rootRels);
            AddText(zip, "word/document.xml", documentXml);
            AddText(zip, "word/_rels/document.xml.rels", docRels);
            AddText(zip, "word/styles.xml", stylesXml);
            AddText(zip, "word/footer1.xml", footerXml);
            foreach (var m in media) AddBytes(zip, "word/" + m.Key, m.Value);
        }

        // Tekshiruv: har bir XML qism to'g'ri tuzilganmi
        int parts = 0;
        using (var zip = OpenZipRead(outPath))
        {
            foreach (var e in zip.Entries)
            {
                if (e.FullName.EndsWith(".xml") || e.FullName.EndsWith(".rels"))
                {
                    using (var s = e.Open())
                    {
                        var doc = new XmlDocument();
                        doc.Load(s);
                        parts++;
                    }
                }
            }
        }

        string desktopCopy = "";
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
            {
                desktopCopy = Path.Combine(desktop, "3-amaliy-ish.docx");
                File.Copy(outPath, desktopCopy, true);
            }
        }
        catch (Exception ex) { desktopCopy = "(Desktopga nusxalanmadi: " + ex.Message + ")"; }

        Debug.Log("HISOBOT TAYYOR: " + outPath + " | " + new FileInfo(outPath).Length + " bayt | XML qismlar tekshirildi: " + parts +
                  " | logotip: " + (logo != null ? logoSource : "topilmadi") + " | Desktop: " + desktopCopy);
    }

    static List<Figure> LoadFigures(string path, out string conclusion)
    {
        conclusion = "";
        var list = new List<Figure>();
        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] p = line.Split('|');
            if (p[0] == "XULOSA") { conclusion = p[1]; continue; }
            var f = new Figure { file = p[0], caption = p[1], text = p[2], alt = p[1] };
            if (p.Length > 3) double.TryParse(p[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f.width);
            list.Add(f);
        }
        return list;
    }

    // ------------------------------------------------------------------ yordamchilar

    static ZipArchive OpenZipRead(string path)
    {
        return new ZipArchive(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ZipArchiveMode.Read);
    }

    static void AddText(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using (var s = entry.Open())
        {
            byte[] b = new UTF8Encoding(false).GetBytes(content);
            s.Write(b, 0, b.Length);
        }
    }

    static void AddBytes(ZipArchive zip, string name, byte[] data)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.NoCompression);
        using (var s = entry.Open()) s.Write(data, 0, data.Length);
    }

    /// <summary>O'zbek tinish belgilari: o', g' -> oʻ gʻ (‘), boshqa apostroflar -> tutuq belgisi (’)</summary>
    static string Fix(string s)
    {
        s = Regex.Replace(s, "([oOgG])'", "$1\u2018");
        return s.Replace("'", "\u2019");
    }

    static string Esc(string s)
    {
        return System.Security.SecurityElement.Escape(Fix(s));
    }

    static string Run(string text, bool bold, bool italic, int halfPoints)
    {
        string rpr = (bold ? "<w:b/><w:bCs/>" : "") + (italic ? "<w:i/><w:iCs/>" : "") +
                     "<w:sz w:val=\"" + halfPoints + "\"/><w:szCs w:val=\"" + halfPoints + "\"/>";
        return "<w:r><w:rPr>" + rpr + "</w:rPr><w:t xml:space=\"preserve\">" + Esc(text) + "</w:t></w:r>";
    }

    static string Para(string style, string pPrInner, string runs)
    {
        string st = style.Length > 0 ? "<w:pStyle w:val=\"" + style + "\"/>" : "";
        return "<w:p><w:pPr>" + st + pPrInner + "</w:pPr>" + runs + "</w:p>";
    }

    static string Drawing(string rid, string name, string alt, long cx, long cy, int id, bool border)
    {
        string line = border
            ? "<a:ln w=\"9525\"><a:solidFill><a:srgbClr val=\"7F7F7F\"/></a:solidFill></a:ln>"
            : "";
        return
            "<w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">" +
            "<wp:extent cx=\"" + cx + "\" cy=\"" + cy + "\"/>" +
            "<wp:docPr id=\"" + id + "\" name=\"Rasm " + id + "\" descr=\"" + Esc(alt) + "\"/>" +
            "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
            "<pic:pic><pic:nvPicPr><pic:cNvPr id=\"" + id + "\" name=\"" + Esc(name) + "\"/><pic:cNvPicPr/></pic:nvPicPr>" +
            "<pic:blipFill><a:blip r:embed=\"" + rid + "\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>" +
            "<pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"" + cx + "\" cy=\"" + cy + "\"/></a:xfrm>" +
            "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" + line + "</pic:spPr></pic:pic>" +
            "</a:graphicData></a:graphic></wp:inline></w:drawing></w:r>";
    }

    static void PngSize(byte[] png, out int width, out int height)
    {
        width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
    }

    // ------------------------------------------------------------------ logotip qidirish

    /// <summary>Avvalgi hisobot (mobile_2.docx) kompyuterda topilsa, undan universitet logotipini oladi.</summary>
    static byte[] FindLogo(out string source)
    {
        source = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var roots = new List<string>();
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        if (!string.IsNullOrEmpty(profile)) roots.Add(Path.Combine(profile, "Downloads"));
        roots.Add(@"D:\");

        foreach (string r in roots)
        {
            if (string.IsNullOrEmpty(r) || !Directory.Exists(r)) continue;
            string found = SearchFile(r, 0, r == @"D:\" ? 2 : 3, sw);
            if (found == null) continue;

            try
            {
                using (var zip = OpenZipRead(found))
                {
                    foreach (var e in zip.Entries)
                    {
                        if (!e.FullName.StartsWith("word/media/") || !e.FullName.EndsWith(".png")) continue;
                        byte[] data;
                        using (var s = e.Open())
                        using (var ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            data = ms.ToArray();
                        }
                        if (data.Length < 24) continue;
                        PngSize(data, out int w, out int h);
                        float ratio = w / (float)h;
                        if (w >= 300 && ratio > 0.85f && ratio < 1.2f)
                        {
                            source = found;
                            return data;
                        }
                    }
                }
            }
            catch (Exception) { }
        }
        return null;
    }

    static string SearchFile(string dir, int depth, int maxDepth, System.Diagnostics.Stopwatch sw)
    {
        if (sw.ElapsedMilliseconds > 8000 || depth > maxDepth) return null;
        try
        {
            foreach (string f in Directory.GetFiles(dir, "mobile_2*.docx"))
                if (!Path.GetFileName(f).StartsWith("~$")) return f;

            foreach (string d in Directory.GetDirectories(dir))
            {
                string n = Path.GetFileName(d);
                if (n.StartsWith(".") || n.StartsWith("$") || n == "Library" || n == "node_modules" || n == "AppData" || n == "Windows" || n == "Program Files") continue;
                string res = SearchFile(d, depth + 1, maxDepth, sw);
                if (res != null) return res;
            }
        }
        catch (Exception) { }
        return null;
    }
}
