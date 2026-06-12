using ModelContextProtocol.Server;
using System.ComponentModel;
using SolidWorksMCP.Core;
using SolidWorksMCP.Core.Models;
using SolidWorksMCP.Services;

namespace SolidWorksMCP.Tools;

[McpServerToolType]
public sealed class DocumentTools(SolidWorksConnectionService sw)
{
    [McpServerTool, Description("List all documents currently open in SolidWorks.")]
    public string ListOpenDocuments() => ToolRunner.Run(() =>
    {
        var app = sw.GetApp();
        var docs = new List<DocumentInfo>();

        dynamic? doc = app.GetFirstDocument();
        while (doc is not null)
        {
            docs.Add(ReadDocInfo(doc));
            doc = doc.GetNext();
        }

        return docs.Count == 0 ? "No documents are open." : ToolRunner.ToJson(docs);
    });

    [McpServerTool, Description("Get details about the active document in SolidWorks.")]
    public string GetActiveDocument() => ToolRunner.Run(() =>
        ToolRunner.ToJson(ReadDocInfo(sw.GetActiveDoc())));

    [McpServerTool, Description("Open a SolidWorks document by its full file path.")]
    public string OpenDocument(
        [Description("Absolute path to the SolidWorks file (.sldprt, .sldasm, .slddrw)")] string filePath,
        [Description("Open as read-only? Default false.")] bool readOnly = false) => ToolRunner.Run(() =>
    {
        if (!File.Exists(filePath))
            return $"File not found: {filePath}";

        var app = sw.GetApp();

        // DocumentSpecification approach (SW 2014+) for clean error handling
        dynamic spec = app.GetOpenDocSpec(filePath);
        spec.ReadOnly = readOnly;
        spec.Silent = true;

        dynamic? doc = app.OpenDoc7(spec);
        int err = spec.Error;
        int warn = spec.Warning;

        if (doc is null || err != 0)
            return $"Failed to open '{filePath}'. Error code: {err}, Warning code: {warn}";

        return $"Opened: {doc.GetTitle()} (warnings: {warn})";
    });

    [McpServerTool, Description("Create a new empty Part, Assembly or Drawing using the default templates.")]
    public string NewDocument(
        [Description("Document type: 'part', 'assembly' or 'drawing'")] string type) => ToolRunner.Run(() =>
    {
        var app = sw.GetApp();
        int pref = type.Trim().ToLowerInvariant() switch
        {
            "part" or "piece" or "pièce" => SwConstants.DefaultTemplatePart,
            "assembly" or "assemblage" => SwConstants.DefaultTemplateAssembly,
            "drawing" or "mise en plan" or "plan" => SwConstants.DefaultTemplateDrawing,
            _ => throw new ArgumentException($"Unknown document type '{type}'. Use part, assembly or drawing."),
        };

        string template = app.GetUserPreferenceStringValue(pref);
        if (string.IsNullOrEmpty(template))
            return "No default template configured in SolidWorks (Tools > Options > Default Templates).";

        dynamic? doc = app.NewDocument(template, 0, 0, 0);
        return doc is null
            ? $"Failed to create document from template '{template}'."
            : $"Created new {type}: {doc.GetTitle()}";
    });

    [McpServerTool, Description("Save the active document.")]
    public string SaveActiveDocument() => ToolRunner.Run(() =>
    {
        dynamic doc = sw.GetActiveDoc();
        int err = 0, warn = 0;
        bool ok = doc.Save3(SwConstants.SaveAsOptionsSilent, ref err, ref warn);
        return ok ? $"Saved '{doc.GetTitle()}'" : $"Save failed. Error: {err}, Warning: {warn}";
    });

    [McpServerTool, Description("Close a document by title. Optionally save before closing.")]
    public string CloseDocument(
        [Description("Title of the document to close (as returned by ListOpenDocuments)")] string title,
        [Description("Save changes before closing? Default false.")] bool save = false) => ToolRunner.Run(() =>
    {
        var app = sw.GetApp();
        dynamic? doc = app.GetFirstDocument();
        while (doc is not null)
        {
            if (string.Equals(doc.GetTitle(), title, StringComparison.OrdinalIgnoreCase))
            {
                if (save)
                {
                    int err = 0, warn = 0;
                    doc.Save3(SwConstants.SaveAsOptionsSilent, ref err, ref warn);
                }
                app.CloseDoc(doc.GetTitle());
                return $"Closed '{title}'";
            }
            doc = doc.GetNext();
        }
        return $"Document '{title}' not found among open documents.";
    });

    [McpServerTool, Description("Activate (bring to front) an open document by title.")]
    public string ActivateDocument(
        [Description("Title of the document to activate")] string title) => ToolRunner.Run(() =>
    {
        var app = sw.GetApp();
        int err = 0;
        dynamic? doc = app.ActivateDoc3(title, true, 2 /* swRebuildOnActivation_e.swDontRebuildActiveDoc */, ref err);
        return doc is null ? $"Could not activate '{title}' (error {err})."
                           : $"Activated '{doc.GetTitle()}'.";
    });

    private static DocumentInfo ReadDocInfo(dynamic doc)
    {
        int typeInt = doc.GetType();
        return new DocumentInfo(
            Title: doc.GetTitle(),
            Path: string.IsNullOrEmpty(doc.GetPathName()) ? "(unsaved)" : doc.GetPathName(),
            Type: SwConstants.DocTypeName(typeInt),
            IsModified: doc.GetSaveFlag()
        );
    }
}
